using UnityEngine;
using UnityEngine.AI; // Vital para la IA que esquiva paredes

[RequireComponent(typeof(NavMeshAgent))] // Esto le pone el "cerebro" de navegación automáticamente
public class EnemigoLanzador : MonoBehaviour
{
    [Header("Configuración de Tiro")]
    public Transform player;
    public GameObject proyectilHarinaPrefab;
    public Transform puntoDisparo;
    public float distanciaAtaque = 15f;
    public float tiempoEntreDisparos = 4f;
    public float fuerzaLanzamiento = 12f;

    [Header("Patrullaje Inteligente")]
    public float radioPatrullaje = 10f; // Qué tan lejos puede vagar desde donde lo pusiste
    public float tiempoEsperaPunto = 2f; // Cuánto tiempo se queda viendo a la nada antes de seguir

    [Header("Sistema de Chisme (Interacción)")]
    public float radioDeteccionAmigos = 3f; // A qué distancia se saludan
    public float tiempoPlatica = 4f; // Cuánto dura el chisme

    private NavMeshAgent agente;
    private Vector3 centroPatrullaje;
    private float tiempoSiguienteDisparo = 0f;
    private float temporizadorEspera = 0f;
    private bool esperandoEnPunto = false;

    // Estados
    public bool estaPlaticando = false;
    private float temporizadorPlatica = 0f;
    private Transform amigoPlatica;

    private void Start()
    {
        agente = GetComponent<NavMeshAgent>();

        // --- EL IMÁN: Lo forzamos a aterrizar en el NavMesh ---
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            agente.Warp(hit.position); // Lo teletransporta exactamente a la malla azul
        }

        centroPatrullaje = transform.position;
        BuscarNuevoPuntoPatrulla();
    }

    private void Update()
    {
        if (player == null) return;

        float distanciaAlJugador = Vector3.Distance(transform.position, player.position);

        // 1. PRIORIDAD ABSOLUTA: Si te ve, deja el chisme y te ataca
        if (distanciaAlJugador <= distanciaAtaque)
        {
            RomperPlatica();
            AtacarJugador();
            return;
        }

        // 2. Si no te ve y está platicando, se queda platicando
        if (estaPlaticando)
        {
            Platicar();
            return;
        }

        // 3. Buscar compadres para platicar mientras camina
        if (BuscarAmigoParaPlaticar()) return;

        // 4. Si no hay jugador ni amigos, sigue su patrullaje aleatorio
        Patrullar();
    }

    private void AtacarJugador()
    {
        agente.isStopped = true; // Clava los frenos para disparar

        Vector3 direccionMirada = player.position - transform.position;
        direccionMirada.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccionMirada), Time.deltaTime * 5f);

        if (Time.time >= tiempoSiguienteDisparo && TieneLineaDeVision())
        {
            Disparar();
            tiempoSiguienteDisparo = Time.time + tiempoEntreDisparos;
        }
    }

    private void Patrullar()
    {
        agente.isStopped = false; // Le quita el freno

        // Si llegó a un punto, espera un ratito antes de moverse al siguiente
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

        // Si ya llegó a su destino actual (NavMesh sabe cuando ya llegó)
        if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance)
        {
            esperandoEnPunto = true;
        }
    }

    private void BuscarNuevoPuntoPatrulla()
    {
        // Elige un punto al azar dentro de su círculo de patrullaje
        Vector3 direccionAleatoria = Random.insideUnitSphere * radioPatrullaje;
        direccionAleatoria += centroPatrullaje;

        NavMeshHit hit;
        // SamplePosition verifica que ese punto aleatorio no esté adentro de una casa o fuera del mapa
        if (NavMesh.SamplePosition(direccionAleatoria, out hit, radioPatrullaje, NavMesh.AllAreas))
        {
            agente.SetDestination(hit.position);
        }
    }

    private bool BuscarAmigoParaPlaticar()
    {
        // Tira un radar a su alrededor buscando a otros enemigos
        Collider[] amigosCercanos = Physics.OverlapSphere(transform.position, radioDeteccionAmigos);

        foreach (Collider col in amigosCercanos)
        {
            if (col.gameObject == this.gameObject) continue; // No puede platicar consigo mismo

            EnemigoLanzador otroEnemigo = col.GetComponent<EnemigoLanzador>();

            // Si encontró a otro enemigo y ESE enemigo no está platicando ya con alguien más
            if (otroEnemigo != null && !otroEnemigo.estaPlaticando)
            {
                // Ambos inician el chisme
                IniciarPlaticaCon(otroEnemigo);
                otroEnemigo.IniciarPlaticaCon(this);
                return true;
            }
        }
        return false;
    }

    public void IniciarPlaticaCon(EnemigoLanzador amigo)
    {
        estaPlaticando = true;
        temporizadorPlatica = 0f;
        amigoPlatica = amigo.transform;

        agente.isStopped = true; // Se detienen para hablar

        // --- AQUÍ PONDRÁS TU ANIMACIÓN DESPUÉS ---
        // if (animator != null) animator.SetBool("IsChatting", true);
    }

    private void Platicar()
    {
        // Se giran suavemente para verse a los ojos
        if (amigoPlatica != null)
        {
            Vector3 direccionMirada = amigoPlatica.position - transform.position;
            direccionMirada.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccionMirada), Time.deltaTime * 5f);
        }

        temporizadorPlatica += Time.deltaTime;

        // Cuando se acaba el tiempo, se despiden
        if (temporizadorPlatica >= tiempoPlatica)
        {
            RomperPlatica();
        }
    }

    public void RomperPlatica()
    {
        estaPlaticando = false;
        amigoPlatica = null;
        if (agente != null && agente.isOnNavMesh) agente.isStopped = false; // Vuelven a caminar

        // --- AQUÍ QUITARÁS TU ANIMACIÓN DESPUÉS ---
        // if (animator != null) animator.SetBool("IsChatting", false);
    }

    private bool TieneLineaDeVision()
    {
        Vector3 direccion = (player.position + Vector3.up * 1f) - puntoDisparo.position;
        if (Physics.Raycast(puntoDisparo.position, direccion, out RaycastHit hit, distanciaAtaque))
        {
            if (hit.collider.CompareTag("Player")) return true;
        }
        return false;
    }

    private void Disparar()
    {
        // 1. Empujamos el panecillo hacia adelante para que no choque con su propia panza
        Vector3 origenSeguro = puntoDisparo.position + transform.forward * 1.5f;

        GameObject proyectil = Instantiate(proyectilHarinaPrefab, origenSeguro, transform.rotation);
        Rigidbody rb = proyectil.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // 2. Apuntamos desde el nuevo origen seguro hacia ti
            Vector3 direccionTiro = (player.position + Vector3.up * 1f - origenSeguro).normalized;
            rb.velocity = (direccionTiro + Vector3.up * 0.2f) * fuerzaLanzamiento;
        }
    }
}