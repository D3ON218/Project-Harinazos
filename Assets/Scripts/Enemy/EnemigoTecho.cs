using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyDummy))]
public class EnemigoTecho : MonoBehaviour
{
    public static bool eventoCinematicoActivo = false;

    [Header("Ataque Cinemático (El Apachurrón)")]
    public Transform player;
    public GameObject nubeHarinaPrefab;
    public float multiplicadorTamanoExplosion = 4f;

    public float radioDeteccion = 3.5f;
    public float tiempoAdvertencia = 0.5f;
    public float tiempoVentanaQTE = 1.5f;

    [Header("Físicas del Salto (Arco)")]
    public float alturaArcoSalto = 2.0f;

    [Header("Efectos de Cámara")]
    public float zoomFOV = 40f;
    private float fovOriginal;
    private Camera camaraPrincipal;

    private Quaternion rotacionOriginalCamara;
    private Vector3 offsetOriginalCamara;
    private MonoBehaviour scriptCamaraController;

    private EnemyDummy dummy;
    private Animator animator;
    private PlayerController playerController;

    private enum EstadoTecho { Fiesta, Advertencia, QTE, Cayendo, Inactivo }
    private EstadoTecho estadoActual = EstadoTecho.Fiesta;

    private int ciclosBaile = 0;
    private bool forzandoBaile = false;

    private void Start()
    {
        dummy = GetComponent<EnemyDummy>();
        animator = GetComponentInChildren<Animator>();
        camaraPrincipal = Camera.main;

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }

        if (camaraPrincipal != null) fovOriginal = camaraPrincipal.fieldOfView;

        UnityEngine.AI.NavMeshAgent agente = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agente != null) agente.enabled = false;

        StartCoroutine(RutinaFiestaTecho());
    }

    private void Update()
    {
        if (dummy == null || dummy.saludHarina <= 0 || dummy.isCoughing || player == null) return;

        if (estadoActual == EstadoTecho.Fiesta)
        {
            Vector3 direccionMirada = player.position - transform.position;
            direccionMirada.y = 0;
            if (direccionMirada != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccionMirada), Time.deltaTime * 3f);
            }

            Vector2 posEnemigo2D = new Vector2(transform.position.x, transform.position.z);
            Vector2 posJugador2D = new Vector2(player.position.x, player.position.z);
            float distanciaHorizontal = Vector2.Distance(posEnemigo2D, posJugador2D);

            if (distanciaHorizontal <= radioDeteccion && transform.position.y > player.position.y + 1f)
            {
                StartCoroutine(RutinaAdvertencia());
            }
        }
    }

    private IEnumerator RutinaFiestaTecho()
    {
        while (dummy.saludHarina > 0)
        {
            if (estadoActual != EstadoTecho.Fiesta || dummy.estaPlaticando)
            {
                if (forzandoBaile) { forzandoBaile = false; dummy.estaDancing = false; }
                yield return null;
                continue;
            }

            forzandoBaile = true;
            dummy.estaDancing = true;

            yield return new WaitForSeconds(Random.Range(4f, 6f));

            ciclosBaile++;

            if (ciclosBaile >= Random.Range(3, 6))
            {
                forzandoBaile = false;
                dummy.estaDancing = false;
                yield return new WaitForSeconds(3f);
                ciclosBaile = 0;
            }
        }
    }

    private IEnumerator RutinaAdvertencia()
    {
        estadoActual = EstadoTecho.Advertencia;

        dummy.bloqueadoPorCombate = true;
        forzandoBaile = false;
        dummy.estaDancing = false;
        dummy.RomperPlatica();

        if (animator != null)
        {
            animator.SetBool("IsDancing", false);
            animator.SetBool("IsTalking", false);
            animator.Play("Base Layer.Idle", 0, 0f);
        }

        float timer = 0;
        while (timer < tiempoAdvertencia)
        {
            Vector2 posEnemigo2D = new Vector2(transform.position.x, transform.position.z);
            Vector2 posJugador2D = new Vector2(player.position.x, player.position.z);

            if (Vector2.Distance(posEnemigo2D, posJugador2D) > radioDeteccion)
            {
                estadoActual = EstadoTecho.Fiesta;
                dummy.bloqueadoPorCombate = false;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(RutinaQTECinematico());
    }

    private IEnumerator RutinaQTECinematico()
    {
        estadoActual = EstadoTecho.QTE;
        eventoCinematicoActivo = true;

        if (animator != null) animator.SetTrigger("Jump");

        if (camaraPrincipal != null)
        {
            scriptCamaraController = camaraPrincipal.GetComponent("CameraController") as MonoBehaviour;
            if (scriptCamaraController != null) scriptCamaraController.enabled = false;

            rotacionOriginalCamara = camaraPrincipal.transform.rotation;
            offsetOriginalCamara = camaraPrincipal.transform.position - player.position;
        }

        Time.timeScale = 0.2f;
        float timerReal = 0f;
        bool jugadorHizoDash = false;

        while (timerReal < tiempoVentanaQTE)
        {
            if (camaraPrincipal != null)
            {
                camaraPrincipal.fieldOfView = Mathf.Lerp(camaraPrincipal.fieldOfView, zoomFOV, Time.unscaledDeltaTime * 5f);

                Vector3 posicionOjosJugador = player.position + Vector3.up * 1.5f + player.forward * 0.3f;
                camaraPrincipal.transform.position = Vector3.Lerp(camaraPrincipal.transform.position, posicionOjosJugador, Time.unscaledDeltaTime * 10f);

                Vector3 direccionEnfoque = (transform.position + Vector3.up * 0.5f) - camaraPrincipal.transform.position;
                Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionEnfoque);
                camaraPrincipal.transform.rotation = Quaternion.Slerp(camaraPrincipal.transform.rotation, rotacionObjetivo, Time.unscaledDeltaTime * 12f);
            }

            if (ChecarInputDashJugador())
            {
                jugadorHizoDash = true;
                break;
            }

            timerReal += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        estadoActual = EstadoTecho.Cayendo;

        Vector3 posicionDesdeTecho = transform.position;
        Vector3 posicionPisoObjetivo = player.position;

        float tiempoCaidaTotal = 0.5f;
        float t = 0;

        while (t < tiempoCaidaTotal)
        {
            t += Time.deltaTime;
            float porcentaje = t / tiempoCaidaTotal;

            if (!jugadorHizoDash)
            {
                posicionPisoObjetivo = player.position;
            }

            Vector3 posicionLinealActual = Vector3.Lerp(posicionDesdeTecho, posicionPisoObjetivo, porcentaje);
            float valorCurvaArc = Mathf.Sin(porcentaje * Mathf.PI);
            float alturaExtraActual = valorCurvaArc * alturaArcoSalto;

            Vector3 posicionFinalFinal = new Vector3(posicionLinealActual.x, posicionLinealActual.y + alturaExtraActual, posicionLinealActual.z);
            transform.position = posicionFinalFinal;

            yield return null;
        }

        transform.position = posicionPisoObjetivo;

        if (nubeHarinaPrefab != null)
        {
            GameObject nube = Instantiate(nubeHarinaPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            nube.transform.localScale *= multiplicadorTamanoExplosion;
        }

        if (!jugadorHizoDash)
        {
            Collider[] afectados = Physics.OverlapSphere(transform.position, 4f);
            foreach (Collider col in afectados)
            {
                // CONEXIÓN AL NUEVO SISTEMA DE SALUD
                if (col.CompareTag("Player"))
                {
                    PlayerHealth playerHealth = col.GetComponent<PlayerHealth>();
                    if (playerHealth != null) playerHealth.MancharTraje(40f);
                }
            }
        }

        estadoActual = EstadoTecho.Inactivo;
        if (dummy != null)
        {
            dummy.saludHarina = 0;
            dummy.RecibirHarinazo();
        }

        StartCoroutine(RestaurarCamara());
    }

    private bool ChecarInputDashJugador()
    {
        if (playerController != null) return playerController.isDashing;
        return Input.GetKeyDown(KeyCode.Space);
    }

    private IEnumerator RestaurarCamara()
    {
        if (camaraPrincipal == null)
        {
            eventoCinematicoActivo = false;
            yield break;
        }

        float t = 0;
        while (t < 1f)
        {
            Vector3 posicionObjetivoCamara = player.position + offsetOriginalCamara;

            camaraPrincipal.fieldOfView = Mathf.Lerp(camaraPrincipal.fieldOfView, fovOriginal, Time.deltaTime * 8f);
            camaraPrincipal.transform.rotation = Quaternion.Slerp(camaraPrincipal.transform.rotation, rotacionOriginalCamara, Time.deltaTime * 8f);
            camaraPrincipal.transform.position = Vector3.Lerp(camaraPrincipal.transform.position, posicionObjetivoCamara, Time.deltaTime * 8f);

            t += Time.deltaTime;
            yield return null;
        }

        camaraPrincipal.fieldOfView = fovOriginal;
        if (scriptCamaraController != null) scriptCamaraController.enabled = true;

        eventoCinematicoActivo = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.7f);

        DrawWireDisc(transform.position, radioDeteccion);
        Vector3 posicionSuelo = new Vector3(transform.position.x, 0, transform.position.z);
        if (player != null) posicionSuelo.y = player.position.y;
        DrawWireDisc(posicionSuelo, radioDeteccion);
        Gizmos.DrawLine(transform.position + Vector3.right * radioDeteccion, posicionSuelo + Vector3.right * radioDeteccion);
        Gizmos.DrawLine(transform.position - Vector3.right * radioDeteccion, posicionSuelo - Vector3.right * radioDeteccion);
        Gizmos.DrawLine(transform.position + Vector3.forward * radioDeteccion, posicionSuelo + Vector3.forward * radioDeteccion);
        Gizmos.DrawLine(transform.position - Vector3.forward * radioDeteccion, posicionSuelo - Vector3.forward * radioDeteccion);
    }

    private void DrawWireDisc(Vector3 center, float radius)
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);

        float step = 0.2f;
        Vector3 lastPoint = Vector3.zero;
        Vector3 firstPoint = Vector3.zero;

        for (float theta = 0; theta < 2 * Mathf.PI + step; theta += step)
        {
            float x = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);
            Vector3 nextPoint = new Vector3(x, 0, z);

            if (theta > 0)
            {
                Gizmos.DrawLine(lastPoint, nextPoint);
            }
            else
            {
                firstPoint = nextPoint;
            }
            lastPoint = nextPoint;
        }
        Gizmos.DrawLine(lastPoint, firstPoint);
        Gizmos.matrix = oldMatrix;
    }
}