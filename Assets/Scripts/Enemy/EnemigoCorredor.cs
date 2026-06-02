using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(EnemyDummy))]
public class EnemigoCorredor : MonoBehaviour
{
    [Header("Ataque de Área (Grito y Explosión)")]
    public Transform player;
    public GameObject nubeHarinaPrefab;
    public float multiplicadorTamanoExplosion = 2f;

    [Tooltip("Distancia a la que te ve normalmente (Ojos del enemigo)")]
    public float distanciaDeteccionNormal = 8f;
    [Tooltip("Distancia a la que te busca si le pegaron a un amigo cercano")]
    public float distanciaDeteccionAlerta = 20f;

    public float distanciaAtaqueArea = 2.5f;
    public float tiempoGritoPreparacion = 1f;
    public float tiempoCansancio = 3f;

    [Header("Velocidades de Movimiento")]
    public float velocidadPatrullaje = 2f;
    public float velocidadPersecucion = 6f;

    [Header("Patrullaje Inteligente")]
    public float radioPatrullaje = 8f;
    public float tiempoEsperaPunto = 2f;

    private NavMeshAgent agente;
    private Animator animator;
    private EnemyDummy dummy;

    private Vector3 centroPatrullaje;
    private float temporizadorEspera = 0f;
    private bool esperandoEnPunto = false;

    public bool estaAtacando = false;

    private void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        dummy = GetComponent<EnemyDummy>();

        Animator[] animators = GetComponentsInChildren<Animator>();
        foreach (Animator anim in animators)
        {
            if (anim.gameObject != this.gameObject)
            {
                animator = anim;
                break;
            }
        }

        centroPatrullaje = transform.position;
        BuscarNuevoPuntoPatrulla();
    }

    private void Update()
    {
        if (agente == null || !agente.enabled || dummy.saludHarina <= 0 || dummy.isCoughing) return;

        if (estaAtacando) return;

        float radioVisionActual = dummy.estaAlerta ? distanciaDeteccionAlerta : distanciaDeteccionNormal;

        if (player != null)
        {
            float distanciaAlJugador = Vector3.Distance(transform.position, player.position);
            bool tieneOjosEnElJugador = TieneLineaDeVision(radioVisionActual);

            // TE DETECTA SÓLO SI: Está en rango Y lo ve directo a los ojos, O si ya está en Mente Colmena (Alerta)
            if (distanciaAlJugador <= radioVisionActual && (tieneOjosEnElJugador || dummy.estaAlerta))
            {
                // Apagamos la vida social de inmediato
                dummy.bloqueadoPorCombate = true;
                dummy.RomperPlatica();
                dummy.estaDancing = false;
                dummy.estaAlerta = false;

                if (animator != null)
                {
                    animator.SetBool("IsTalking", false);
                    animator.SetBool("IsDancing", false);
                }

                PerseguirYAtacar(distanciaAlJugador);
                return;
            }
            else
            {
                // Si el jugador se escapó o se escondió detrás de un muro, liberamos el bloqueo de combate
                if (!estaAtacando) dummy.bloqueadoPorCombate = false;

                // Si se rompió el camino y traía al jugador de destino, reseteamos para que patrulle normal
                if (agente.hasPath && agente.destination == player.position)
                {
                    agente.ResetPath();
                    BuscarNuevoPuntoPatrulla();
                }
            }
        }

        if (dummy.estaAlerta)
        {
            agente.speed = velocidadPatrullaje;
            if (agente.isOnNavMesh) agente.isStopped = true;

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

        if (dummy.estaDancing || dummy.estaPlaticando) return;

        Patrullar();
    }

    private void PerseguirYAtacar(float distanciaAlJugador)
    {
        if (!agente.isOnNavMesh) return;

        agente.speed = velocidadPersecucion;
        agente.isStopped = false;
        agente.SetDestination(player.position);

        if (distanciaAlJugador <= distanciaAtaqueArea && !estaAtacando)
        {
            StartCoroutine(RutinaAtaqueExplosivo());
        }
    }

    private IEnumerator RutinaAtaqueExplosivo()
    {
        estaAtacando = true;
        dummy.bloqueadoPorCombate = true; // Aseguramos bloqueo absoluto

        if (agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
            agente.ResetPath();
        }

        // BAJA TOTAL: Forzamos la animación de Idle de la raíz del proyecto usando su ruta absoluta
        if (animator != null)
        {
            animator.SetBool("IsDancing", false);
            animator.SetBool("IsTalking", false);
            animator.Play("Base Layer.Idle", 0, 0f);
        }

        if (player != null)
        {
            Vector3 direccionMirada = player.position - transform.position;
            direccionMirada.y = 0;
            if (direccionMirada != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direccionMirada);
            }
        }

        yield return new LogIfGreeting(); // Pequeño espacio de respiro técnico

        if (animator != null && !dummy.isCoughing && dummy.saludHarina > 0)
            animator.SetTrigger("BattleCry");

        float t = 0;
        while (t < tiempoGritoPreparacion)
        {
            if (dummy.isCoughing || dummy.saludHarina <= 0)
            {
                FinalizarAtaque();
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        if (nubeHarinaPrefab != null && !dummy.isCoughing && dummy.saludHarina > 0)
        {
            GameObject nube = Instantiate(nubeHarinaPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            nube.transform.localScale = nube.transform.localScale * multiplicadorTamanoExplosion;
        }

        t = 0;
        while (t < tiempoCansancio)
        {
            if (dummy.isCoughing || dummy.saludHarina <= 0)
            {
                FinalizarAtaque();
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        FinalizarAtaque();
    }

    private void FinalizarAtaque()
    {
        estaAtacando = false;
        dummy.bloqueadoPorCombate = false;
        if (agente.isOnNavMesh && dummy.saludHarina > 0 && !dummy.isCoughing)
        {
            agente.isStopped = false;
        }
    }

    private bool TieneLineaDeVision(float radioVisionActual)
    {
        if (player == null) return false;

        // Lanzamos el rayo desde la altura del pecho del enemigo hacia el jugador
        Vector3 origen = transform.position + Vector3.up * 1f;
        Vector3 direccion = (player.position + Vector3.up * 1f) - origen;

        if (Physics.Raycast(origen, direccion.normalized, out RaycastHit hit, radioVisionActual))
        {
            if (hit.collider.CompareTag("Player")) return true;
        }
        return false;
    }

    private void Patrullar()
    {
        if (!agente.isOnNavMesh) return;

        agente.speed = velocidadPatrullaje;
        agente.isStopped = false;

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
}

public class LogIfGreeting : CustomYieldInstruction
{
    public override bool keepWaiting { get { return false; } }
}