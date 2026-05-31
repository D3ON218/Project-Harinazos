using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemigoTecho : MonoBehaviour
{
    [Header("Configuración de Caída")]
    public float radioDeteccionSuelo = 4f;
    public LayerMask capaJugador;

    [Header("Explosión de Harina")]
    public GameObject nubeExplosionPrefab;
    public float radioExplosion = 5f;

    [Header("Patrullaje")]
    public Transform[] puntosPatrulla;
    public float velocidadPatrulla = 2f;
    public float tiempoEspera = 2f;

    private Rigidbody rb;
    private bool yaCayo = false;
    private float tiempoInicioCaida = 0f;

    // Variables de patrulla
    private int indiceDestino = 0;
    private float temporizadorEspera = 0f;
    private bool esperando = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private void Update()
    {
        if (yaCayo) return;

        // 1. Buscar al jugador hacia abajo
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, radioDeteccionSuelo, Vector3.down, out hit, 20f, capaJugador))
        {
            Desplomar();
            return; // Cortamos aquí para que deje de patrullar
        }

        // 2. Si no ve al jugador, Patrullar
        Patrullar();
    }

    private void Patrullar()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;

        if (esperando)
        {
            temporizadorEspera += Time.deltaTime;
            if (temporizadorEspera >= tiempoEspera)
            {
                esperando = false;
                temporizadorEspera = 0f;
                // Mirar al nuevo punto
                transform.LookAt(puntosPatrulla[indiceDestino]);
            }
            return;
        }

        Transform destino = puntosPatrulla[indiceDestino];
        transform.position = Vector3.MoveTowards(transform.position, destino.position, velocidadPatrulla * Time.deltaTime);

        if (Vector3.Distance(transform.position, destino.position) < 0.1f)
        {
            esperando = true;
            indiceDestino = (indiceDestino + 1) % puntosPatrulla.Length;
        }
    }

    private void Desplomar()
    {
        yaCayo = true;
        rb.isKinematic = false;
        tiempoInicioCaida = Time.time; // Guardamos la hora exacta de la caída
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!yaCayo) return;

        // EL ARREGLO: Le damos 0.5 segundos de invulnerabilidad para que se despegue del techo
        if (Time.time < tiempoInicioCaida + 0.5f) return;

        if (nubeExplosionPrefab != null)
        {
            Instantiate(nubeExplosionPrefab, transform.position, Quaternion.identity);
        }

        Collider[] afectados = Physics.OverlapSphere(transform.position, radioExplosion, capaJugador);
        foreach (Collider col in afectados)
        {
            Debug.Log("¡El jugador fue alcanzado por la explosión!");
        }

        Destroy(gameObject);
    }
}