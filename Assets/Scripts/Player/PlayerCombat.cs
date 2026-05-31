using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    private PlayerControls controls;
    private Animator animator;
    private Transform camTransform;

    [Header("Inventario")]
    public int municionHarina = 0;

    [Header("Estado")]
    public bool isAiming = false;
    public bool isPerformingAction = false;

    [Header("Ataque a Distancia")]
    public GameObject proyectilPrefab;
    public Transform puntoDisparo;
    public float fuerzaLanzamiento = 15f;
    public float arcoLanzamiento = 0.5f;

    [Header("Corte y Marcador de Trayectoria")]
    public LayerMask capaColisionTrayectoria;
    public GameObject marcadorImpacto;
    private LineRenderer trayectoriaLine;

    [Header("Ataque Melee")]
    public float radioPatada = 2.5f;
    public LayerMask capaEnemigo;

    private void Awake()
    {
        controls = new PlayerControls();
        animator = GetComponentInChildren<Animator>();
        trayectoriaLine = GetComponent<LineRenderer>();

        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        controls.Player.Throw.performed += ctx => LanzarHarina();
        controls.Player.Melee.performed += ctx => EjecutarPatada();

        controls.Player.Aim.started += ctx => IniciarApuntado();
        controls.Player.Aim.canceled += ctx => CancelarApuntado();

        controls.Player.Interact.performed += ctx => IntentarInteractuar();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void IniciarApuntado()
    {
        if (isPerformingAction) return; // No apuntar mientras haces otra cosa
        isAiming = true;
    }

    private void CancelarApuntado()
    {
        isAiming = false;
        if (trayectoriaLine != null) trayectoriaLine.enabled = false;
        if (marcadorImpacto != null) marcadorImpacto.SetActive(false);
    }

    private void Update()
    {
        if (municionHarina > 0 && isAiming && !isPerformingAction)
        {
            trayectoriaLine.enabled = true;
            DibujarTrayectoria();
        }
        else
        {
            if (trayectoriaLine != null) trayectoriaLine.enabled = false;
            if (marcadorImpacto != null) marcadorImpacto.SetActive(false);
        }
    }

    private void DibujarTrayectoria()
    {
        int numPuntos = 30;
        trayectoriaLine.positionCount = numPuntos;

        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position + transform.forward + Vector3.up * 1.2f;
        Vector3 direccionLanzamiento = (camTransform.forward + Vector3.up * arcoLanzamiento).normalized;
        Vector3 velocidadInicial = direccionLanzamiento * fuerzaLanzamiento;

        Vector3 puntoAnterior = origen;
        trayectoriaLine.SetPosition(0, origen);

        bool golpeoAlgo = false;

        for (int i = 1; i < numPuntos; i++)
        {
            float tiempo = i * 0.1f;
            Vector3 puntoCalculado = origen + (velocidadInicial * tiempo) + (Physics.gravity * 0.5f * tiempo * tiempo);

            Vector3 direccion = puntoCalculado - puntoAnterior;
            float distancia = direccion.magnitude;

            RaycastHit hit;
            if (Physics.Raycast(puntoAnterior, direccion.normalized, out hit, distancia, capaColisionTrayectoria))
            {
                trayectoriaLine.positionCount = i + 1;
                trayectoriaLine.SetPosition(i, hit.point);

                if (marcadorImpacto != null)
                {
                    marcadorImpacto.SetActive(true);
                    marcadorImpacto.transform.position = hit.point + hit.normal * 0.05f;
                    marcadorImpacto.transform.rotation = Quaternion.LookRotation(-hit.normal);
                }

                golpeoAlgo = true;
                break;
            }
            else
            {
                trayectoriaLine.SetPosition(i, puntoCalculado);
                puntoAnterior = puntoCalculado;
            }
        }

        if (!golpeoAlgo && marcadorImpacto != null)
        {
            marcadorImpacto.SetActive(false);
        }
    }

    private void LanzarHarina()
    {
        if (municionHarina <= 0 || !isAiming || isPerformingAction) return;
        StartCoroutine(RutinaLanzar());
    }

    private System.Collections.IEnumerator RutinaLanzar()
    {
        isPerformingAction = true;
        municionHarina--;

        if (animator != null) animator.SetTrigger("Throw");

        yield return new WaitForSeconds(0.3f);

        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position + transform.forward + Vector3.up * 1.2f;
        GameObject proyectil = Instantiate(proyectilPrefab, origen, transform.rotation);

        Rigidbody rbProyectil = proyectil.GetComponent<Rigidbody>();
        if (rbProyectil != null)
        {
            Vector3 direccionLanzamiento = (camTransform.forward + Vector3.up * arcoLanzamiento).normalized;
            rbProyectil.velocity = direccionLanzamiento * fuerzaLanzamiento;
        }

        yield return new WaitForSeconds(0.7f);

        isPerformingAction = false;
    }

    private void EjecutarPatada()
    {
        if (isPerformingAction) return;
        StartCoroutine(RutinaPatada());
    }

    private System.Collections.IEnumerator RutinaPatada()
    {
        isPerformingAction = true;

        if (animator != null) animator.SetTrigger("Kick");

        yield return new WaitForSeconds(0.4f);

        Vector3 centroEsfera = transform.position + transform.forward * 1.2f + Vector3.up * 1f;
        Collider[] enemigosGolpeados = Physics.OverlapSphere(centroEsfera, radioPatada, capaEnemigo);

        foreach (Collider col in enemigosGolpeados)
        {
            EnemyDummy enemigo = col.GetComponent<EnemyDummy>();
            if (enemigo != null)
            {
                float coincidenciaMirada = Vector3.Dot(transform.forward, enemigo.transform.forward);

                if (coincidenciaMirada > 0.5f)
                {
                    Debug.Log("¡ATAQUE SIGILOSO DESDE ATRÁS! Noqueado de una patada.");
                    enemigo.RecibirPatada();
                    break;
                }

                if (enemigo.isCoughing)
                {
                    Debug.Log("¡PATADA GIRATORIA DE REMATE! Noqueado por combo.");
                    enemigo.RecibirPatada();
                    break;
                }
                else
                {
                    Debug.Log("Le diste una patada de frente, pero como no está tosiendo, se la peló.");
                }
            }
        }

        yield return new WaitForSeconds(0.6f);

        isPerformingAction = false;
    }

    private void IntentarInteractuar()
    {
        if (isPerformingAction) return;

        Collider[] objetosCerca = Physics.OverlapSphere(transform.position, 1.5f);

        foreach (Collider col in objetosCerca)
        {
            SacoHarina saco = col.GetComponent<SacoHarina>();
            if (saco != null)
            {
                StartCoroutine(RutinaRecoger(saco));
                return;
            }
        }
    }

    private System.Collections.IEnumerator RutinaRecoger(SacoHarina saco)
    {
        isPerformingAction = true;

        if (animator != null) animator.SetTrigger("Pickup");

        yield return new WaitForSeconds(0.5f);

        if (saco != null) saco.Recoger(this);

        yield return new WaitForSeconds(0.5f);

        isPerformingAction = false;
    }

    public void AgregarMunicion(int cantidad)
    {
        municionHarina += cantidad;
    }
}