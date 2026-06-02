using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyDummy))]
public class EnemigoLanzador : MonoBehaviour
{
    [Header("Configuración de Tiro")]
    public Transform player;
    public GameObject proyectilHarinaPrefab;
    public Transform puntoDisparo;

    [Tooltip("Distancia a la que te ve normalmente (Corta)")]
    public float distanciaAtaqueNormal = 6f;
    [Tooltip("Distancia a la que te ve cuando se asusta (Larga)")]
    public float distanciaAtaqueAlerta = 20f;

    public float tiempoEntreDisparos = 4f;
    public float fuerzaLanzamiento = 12f;

    [Header("Patrullaje Inteligente")]
    public float radioPatrullaje = 5f;
    public float tiempoEsperaPunto = 2f;

    private NavMeshAgent agente;
    private Animator animator;
    private EnemyDummy dummy;

    private Vector3 centroPatrullaje;
    private float tiempoSiguienteDisparo = 0f;
    private float temporizadorEspera = 0f;
    private bool esperandoEnPunto = false;

    private void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        dummy = GetComponent<EnemyDummy>();

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agente.Warp(hit.position);
        }

        centroPatrullaje = transform.position;
        esperandoEnPunto = false;
        BuscarNuevoPuntoPatrulla();
    }

    private void Update()
    {
        if (agente == null || !agente.enabled || dummy.saludHarina <= 0 || dummy.isCoughing) return;

        // 1. Calcular el rango de visión dinámico
        float radioVisionActual = dummy.estaAlerta ? distanciaAtaqueAlerta : distanciaAtaqueNormal;

        // 2. Sistema de Agresividad Directa
        if (player != null)
        {
            float distanciaAlJugador = Vector3.Distance(transform.position, player.position);

            // Revisa si entraste en su rango actual (corto o largo)
            if (distanciaAlJugador <= radioVisionActual)
            {
                dummy.RomperPlatica();
                dummy.estaDancing = false;
                dummy.estaAlerta = false; // Ya te vio, ataca directo
                AtacarJugador(radioVisionActual);
                return;
            }
        }

        // 3. ¿Están en estado de Alerta porque le pegaron a un amigo?
        if (dummy.estaAlerta)
        {
            if (agente.isOnNavMesh) agente.isStopped = true;

            // Voltean hacia el jugador a lo lejos buscando venganza
            if (player != null)
            {
                Vector3 direccionMirada = player.position - transform.position;
                direccionMirada.y = 0;
                if (direccionMirada != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccionMirada), Time.deltaTime * 3f);
                }
            }
            return;
        }

        // 4. Respetar vida social
        if (dummy.estaDancing || dummy.estaPlaticando) return;

        // 5. Patrullaje de rutina
        Patrullar();
    }

    private void AtacarJugador(float radioVisionActual)
    {
        if (agente.isOnNavMesh) agente.isStopped = true;

        Vector3 direccionMirada = player.position - transform.position;
        direccionMirada.y = 0;
        if (direccionMirada != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccionMirada), Time.deltaTime * 5f);
        }

        // Le pasamos el radio actual a la línea de visión
        if (Time.time >= tiempoSiguienteDisparo && TieneLineaDeVision(radioVisionActual))
        {
            Disparar();
            tiempoSiguienteDisparo = Time.time + tiempoEntreDisparos;
        }
    }

    private void Patrullar()
    {
        if (agente.isOnNavMesh) agente.isStopped = false;

        if (esperandoEnPunto)
        {
            temporizadorEspera += Time.deltaTime;
            if (temporizadorEspera >= tiempoEsperaPunto)
            {
                esperandoEnPunto = false;
                temporizadorEspera = 0f;
                BuscarNuevoPuntoPatrulla();
            }
            return;
        }

        if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance)
        {
            esperandoEnPunto = true;
            temporizadorEspera = 0f;
        }
    }

    private void BuscarNuevoPuntoPatrulla()
    {
        if (radioPatrullaje <= 0) radioPatrullaje = 10f;

        for (int i = 0; i < 10; i++)
        {
            Vector3 direccionAleatoria = Random.insideUnitSphere * radioPatrullaje;
            direccionAleatoria += centroPatrullaje;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(direccionAleatoria, out hit, radioPatrullaje, NavMesh.AllAreas))
            {
                if (agente.isOnNavMesh)
                {
                    agente.SetDestination(hit.position);
                    return;
                }
            }
        }
    }

    private bool TieneLineaDeVision(float radioVisionActual)
    {
        if (puntoDisparo == null) return false;
        Vector3 direccion = (player.position + Vector3.up * 1f) - puntoDisparo.position;

        // El raycast ahora llega hasta su visión máxima actual
        if (Physics.Raycast(puntoDisparo.position, direccion.normalized, out RaycastHit hit, radioVisionActual))
        {
            if (hit.collider.CompareTag("Player")) return true;
        }
        return false;
    }

    private void Disparar()
    {
        if (puntoDisparo == null) return;

        if (animator != null) animator.SetTrigger("Throw");

        Vector3 origenSeguro = puntoDisparo.position + transform.forward * 1.2f;
        GameObject proyectil = Instantiate(proyectilHarinaPrefab, origenSeguro, transform.rotation);
        Rigidbody rb = proyectil.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 direccionTiro = (player.position + Vector3.up * 1f - origenSeguro).normalized;
            rb.velocity = (direccionTiro + Vector3.up * 0.2f) * fuerzaLanzamiento;
        }
    }
}