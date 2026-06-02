using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyDummy : MonoBehaviour
{
    [Header("Estadísticas de Vida")]
    public int saludHarina = 3;
    public bool isCoughing = false;
    public float tiempoTos = 4f;

    [Header("Ajustes de Animación Terco (Tos)")]
    public Vector3 rotacionMagicaTos = new Vector3(-90f, 0f, 0f);
    public Vector3 offsetAlturaTos = new Vector3(0f, 1f, 0f);

    [Header("Sistema Social: Chisme Dinámico")]
    public float radioDeteccionAmigos = 4f;
    public bool estaPlaticando = false;
    private float temporizadorPlatica = 0f;
    private float tiempoPlaticaAleatorio = 0f;
    private Transform amigoPlatica;
    private float tiempoEnfriamientoChisme = 0f;

    [Header("Sistema Social: Baile Automático")]
    public bool estaDancing = false;
    private float temporizadorDecisiones = 0f;

    [Header("Sistema de Alerta")]
    public bool estaAlerta = false;
    private float temporizadorAlerta = 0f;

    [Header("Seguridad de Combate")]
    [Tooltip("Cualquier script de ataque puede activar esto para apagar el chisme/baile temporalmente")]
    public bool bloqueadoPorCombate = false; // <-- NUEVA ANCLA DE SEGURIDAD

    private Animator animator;
    private NavMeshAgent agente;

    private Vector3 posInicialModelo;
    private Quaternion rotInicialModelo;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agente = GetComponent<NavMeshAgent>();

        if (animator != null)
        {
            posInicialModelo = animator.transform.localPosition;
            rotInicialModelo = animator.transform.localRotation;
        }
    }

    private void Update()
    {
        if (tiempoEnfriamientoChisme > 0) tiempoEnfriamientoChisme -= Time.deltaTime;
        if (temporizadorAlerta > 0)
        {
            temporizadorAlerta -= Time.deltaTime;
            if (temporizadorAlerta <= 0) estaAlerta = false;
        }

        // SI ESTÁ MUERTO, TOSIENDO O ATACANDO, SE APAGA LA VIDA SOCIAL COMPLETAMENTE
        if (saludHarina <= 0 || isCoughing || bloqueadoPorCombate) return;

        // 1. Sincronizar Animaciones
        if (animator != null)
        {
            if (agente != null && agente.enabled)
            {
                animator.SetFloat("Speed", agente.velocity.magnitude);
            }
            animator.SetBool("IsTalking", estaPlaticating());
            animator.SetBool("IsDancing", estaDancing);
        }

        if (estaAlerta)
        {
            RomperPlatica();
            estaDancing = false;
            return;
        }

        TomarDecisionDeBaile();

        if (estaDancing)
        {
            if (agente != null && agente.isOnNavMesh)
            {
                agente.isStopped = true;
                agente.velocity = Vector3.zero;
            }
            return;
        }

        if (estaPlaticando)
        {
            Platicar();
            return;
        }

        if (tiempoEnfriamientoChisme <= 0 && agente.velocity.magnitude < 0.5f)
        {
            BuscarAmigoParaPlaticar();
        }
    }

    private bool estaPlaticating() { return estaPlaticando; }

    private void TomarDecisionDeBaile()
    {
        temporizadorDecisiones += Time.deltaTime;
        if (temporizadorDecisiones >= 3f && agente.velocity.magnitude < 0.2f)
        {
            temporizadorDecisiones = 0f;

            int probabilidad = estaPlaticando ? 40 : 10;
            int dado = Random.Range(0, 100);

            if (dado <= probabilidad && !estaDancing)
            {
                StartCoroutine(RutinaBaile());
            }
        }
    }

    private IEnumerator RutinaBaile()
    {
        estaDancing = true;

        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
            agente.ResetPath();
        }

        yield return new WaitForSeconds(Random.Range(3f, 6f));

        estaDancing = false;

        if (!estaPlaticando && agente != null && agente.isOnNavMesh && !bloqueadoPorCombate)
        {
            agente.isStopped = false;
        }
    }

    private void BuscarAmigoParaPlaticar()
    {
        Collider[] amigosCercanos = Physics.OverlapSphere(transform.position, radioDeteccionAmigos);

        foreach (Collider col in amigosCercanos)
        {
            if (col.gameObject == this.gameObject) continue;

            EnemyDummy otroDummy = col.GetComponent<EnemyDummy>();

            if (otroDummy != null && !otroDummy.estaPlaticando && otroDummy.tiempoEnfriamientoChisme <= 0 && !otroDummy.estaAlerta && !otroDummy.isCoughing && otroDummy.saludHarina > 0 && !otroDummy.bloqueadoPorCombate)
            {
                float tiempoAcordado = Random.Range(5f, 12f);
                IniciarPlaticaCon(otroDummy, tiempoAcordado);
                otroDummy.IniciarPlaticaCon(this, tiempoAcordado);
                return;
            }
        }
    }

    public void IniciarPlaticaCon(EnemyDummy amigo, float tiempo)
    {
        estaPlaticando = true;
        temporizadorPlatica = 0f;
        tiempoPlaticaAleatorio = tiempo;
        amigoPlatica = amigo.transform;

        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
            agente.ResetPath();
        }
    }

    private void Platicar()
    {
        if (amigoPlatica != null)
        {
            Vector3 direccionMirada = amigoPlatica.position - transform.position;
            direccionMirada.y = 0;
            if (direccionMirada != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccionMirada), Time.deltaTime * 5f);
            }
        }

        temporizadorPlatica += Time.deltaTime;
        if (temporizadorPlatica >= tiempoPlaticaAleatorio)
        {
            RomperPlatica();
            tiempoEnfriamientoChisme = 10f;
        }
    }

    public void RomperPlatica()
    {
        estaPlaticando = false;
        amigoPlatica = null;
        if (agente != null && agente.isOnNavMesh && !bloqueadoPorCombate) agente.isStopped = false;
    }

    public void RecibirHarinazo()
    {
        saludHarina--;
        AvisarACompaneros(10f);

        if (saludHarina <= 0)
        {
            CaerNoqueado();
        }
        else if (!isCoughing)
        {
            StartCoroutine(EstadoVulnerable());
        }
    }

    private void AvisarACompaneros(float radioAlerta)
    {
        Collider[] cercanos = Physics.OverlapSphere(transform.position, radioAlerta);
        foreach (Collider col in cercanos)
        {
            if (col.gameObject == this.gameObject) continue;

            EnemyDummy compa = col.GetComponent<EnemyDummy>();
            if (compa != null && compa.saludHarina > 0 && !compa.isCoughing)
            {
                compa.ActivarAlerta();
            }
        }
    }

    public void ActivarAlerta()
    {
        estaAlerta = true;
        temporizadorAlerta = 8f;
        RomperPlatica();
        estaDancing = false;
    }

    public void RecibirPatada()
    {
        CaerNoqueado();
    }

    private IEnumerator EstadoVulnerable()
    {
        isCoughing = true;
        RomperPlatica();
        estaDancing = false;

        if (animator != null)
        {
            animator.transform.localRotation = Quaternion.Euler(rotacionMagicaTos);
            animator.transform.localPosition = posInicialModelo + offsetAlturaTos;
            animator.SetBool("IsCoughing", true);
        }

        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
        }

        yield return new WaitForSeconds(tiempoTos);

        isCoughing = false;

        if (animator != null)
        {
            animator.SetBool("IsCoughing", false);
            animator.transform.localRotation = rotInicialModelo;
            animator.transform.localPosition = posInicialModelo;
        }

        if (agente != null && agente.isOnNavMesh) agente.isStopped = false;
    }

    private void CaerNoqueado()
    {
        StopAllCoroutines();
        isCoughing = false;
        RomperPlatica();

        if (animator != null)
        {
            animator.SetBool("IsCoughing", false);
            animator.transform.localRotation = rotInicialModelo;
            animator.transform.localPosition = posInicialModelo;
            animator.Play("Knocked");
        }

        if (agente != null) agente.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        this.enabled = false;
    }
}