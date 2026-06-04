using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(EnemyDummy))]
public class EnemigoCorredor : MonoBehaviour
{
    [Header("Ataque de Área (Grito y Explosión)")]
    public Transform player;
    public GameObject nubeHarinaPrefab;
    [Tooltip("¿Qué tan grande será la nube comparada con la del Lanzador? (ej. 2 = el doble)")]
    public float multiplicadorTamanoExplosion = 2f;

    [Tooltip("Distancia a la que te ve normalmente (Ojos del enemigo)")]
    public float distanciaDeteccionNormal = 8f;
    [Tooltip("Distancia a la que te busca si le pegaron a un amigo cercano")]
    public float distanciaDeteccionAlerta = 20f;

    public float distanciaAtaqueArea = 2.5f;
    public float tiempoGritoPreparacion = 1.2f;
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
    private float radioAmigosOriginal;

    // --- NUESTRA VARIABLE DEL FRENO DE MANO ---
    private bool estabaTosiendo = false;

    private void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        dummy = GetComponent<EnemyDummy>();
        animator = GetComponentInChildren<Animator>();

        centroPatrullaje = transform.position;

        if (dummy != null)
        {
            radioAmigosOriginal = dummy.radioDeteccionAmigos;
        }

        BuscarNuevoPuntoPatrulla();
    }

    private void Update()
    {
        // 1. QUITAMOS dummy.isCoughing DE ESTA LÍNEA
        if (agente == null || !agente.enabled || dummy.saludHarina <= 0) return;
        if (EnemigoTecho.eventoCinematicoActivo) return;

        // 2. AGREGAMOS EL BLOQUE DEL FRENO DE MANO
        if (dummy.isCoughing)
        {
            if (!estabaTosiendo)
            {
                // En el instante del golpe, detenemos el NavMeshAgent en seco
                if (agente.isOnNavMesh)
                {
                    agente.isStopped = true;
                    agente.velocity = Vector3.zero;
                }
                estabaTosiendo = true;
            }
            return; // Abortamos el resto del Update mientras tose
        }
        else if (estabaTosiendo)
        {
            // Ya se le pasó la tos, le quitamos el freno de mano
            estabaTosiendo = false;
            if (agente.isOnNavMesh) agente.isStopped = false;
        }

        // --- DE AQUÍ PARA ABAJO ES TU CÓDIGO NORMAL ---

        if (estaAtacando)
        {
            if (dummy.estaPlaticando) dummy.RomperPlatica();
            dummy.estaDancing = false;

            if (animator != null)
            {
                animator.SetBool("IsDancing", false);
                animator.SetBool("IsTalking", false);
                animator.SetFloat("Speed", 0f);
            }
            return;
        }

        float radioVisionActual = dummy.estaAlerta ? distanciaDeteccionAlerta : distanciaDeteccionNormal;

        if (player != null)
        {
            float distanciaAlJugador = Vector3.Distance(transform.position, player.position);
            bool tieneOjosEnElJugador = TieneLineaDeVision(radioVisionActual);

            if (distanciaAlJugador <= radioVisionActual && (tieneOjosEnElJugador || dummy.estaAlerta))
            {
                if (dummy.estaPlaticando) dummy.RomperPlatica();
                if (dummy.estaDancing) dummy.estaDancing = false;

                PerseguirYAtacar(distanciaAlJugador);
                return;
            }
            else
            {
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
            if (agente.isOnNavMesh) { agente.isStopped = true; MirarAlJugador(); }
            return;
        }

        if (dummy.estaDancing || dummy.estaPlaticating()) return;

        Patrullar();
    }

    private void MirarAlJugador()
    {
        if (player == null) return;
        Vector3 direccionMirada = player.position - transform.position;
        direccionMirada.y = 0;
        if (direccionMirada != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccionMirada), Time.deltaTime * 5f);
        }
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

        dummy.radioDeteccionAmigos = 0f;
        if (dummy.estaPlaticando) dummy.RomperPlatica();
        dummy.estaDancing = false;

        if (agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
            agente.ResetPath();
        }

        if (animator != null)
        {
            animator.SetBool("IsDancing", false);
            animator.SetBool("IsTalking", false);
            animator.SetFloat("Speed", 0f);
        }
        yield return null;

        if (animator != null && !dummy.isCoughing && dummy.saludHarina > 0)
        {
            animator.SetTrigger("BattleCry");
        }

        float t = 0;
        while (t < tiempoGritoPreparacion)
        {
            if (dummy.isCoughing || dummy.saludHarina <= 0)
            {
                FinalizarAtaque();
                yield break;
            }
            MirarAlJugador();
            t += Time.deltaTime;
            yield return null;
        }

        if (nubeHarinaPrefab != null && !dummy.isCoughing && dummy.saludHarina > 0)
        {
            GameObject nube = Instantiate(nubeHarinaPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            nube.transform.localScale *= multiplicadorTamanoExplosion;

            float radioExplosion = 3f;
            Collider[] afectados = Physics.OverlapSphere(transform.position, radioExplosion);

            foreach (Collider col in afectados)
            {
                if (col.CompareTag("Player"))
                {
                    PlayerHealth playerHealth = col.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.MancharTraje(15f);
                    }
                }
            }
        }

        yield return new WaitForSeconds(tiempoCansancio);
        FinalizarAtaque();
    }

    private void FinalizarAtaque()
    {
        estaAtacando = false;
        dummy.radioDeteccionAmigos = radioAmigosOriginal;

        if (agente != null && agente.isOnNavMesh && dummy.saludHarina > 0 && !dummy.isCoughing)
        {
            agente.isStopped = false;
        }
    }

    private bool TieneLineaDeVision(float radio)
    {
        if (player == null) return false;
        Vector3 origen = transform.position + Vector3.up * 1f;
        Vector3 direccion = (player.position + Vector3.up * 1f) - origen;

        if (Physics.Raycast(origen, direccion.normalized, out RaycastHit hit, radio))
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
        Vector3 direccionAleatoria = Random.insideUnitSphere * radioPatrullaje + centroPatrullaje;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(direccionAleatoria, out hit, radioPatrullaje, NavMesh.AllAreas))
        {
            if (agente.isOnNavMesh) agente.SetDestination(hit.position);
        }
    }
}

public static class DummyExtensions
{
    public static bool estaPlaticating(this EnemyDummy d) { return d.estaPlaticando; }
}