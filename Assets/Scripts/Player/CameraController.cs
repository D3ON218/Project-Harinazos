using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Seguimiento Estilo Gears")]
    public Transform targetToFollow;
    public float distance = 2.5f;
    public float height = 1.2f;
    public float shoulderOffset = 0.8f;
    public float sensitivity = 150f;

    [Header("Colisi�n con Paredes")]
    public LayerMask capasParedes;
    public float suavidadAcercamiento = 25f;
    [Tooltip("Grosor de la c�mara para que no atraviese las esquinas(No lo pude hacer jalar)")]
    public float radioCamara = 0.3f;

    private PlayerControls controls;
    private float camXRotation = 0f;
    private float camYRotation = 0f;

    private float distanciaActual;

    private void Awake()
    {
        controls = new PlayerControls();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Start()
    {
        distanciaActual = distance;
    }

    private void LateUpdate()
    {
        if (targetToFollow == null) return;

        Vector2 lookInput = controls.Player.Look.ReadValue<Vector2>();

        camYRotation += lookInput.x * sensitivity * Time.deltaTime;
        camXRotation -= lookInput.y * sensitivity * Time.deltaTime;
        camXRotation = Mathf.Clamp(camXRotation, -30f, 60f);

        Quaternion rotation = Quaternion.Euler(camXRotation, camYRotation, 0f);

        Vector3 targetHeadPosition = targetToFollow.position + (Vector3.up * height);
        Vector3 shoulderPivot = targetHeadPosition + (rotation * new Vector3(shoulderOffset, 0f, 0f));

        Vector3 direccionHaciaAtras = rotation * new Vector3(0f, 0f, -1f);

        float distanciaObjetivo = distance;

        if (Physics.SphereCast(shoulderPivot, radioCamara, direccionHaciaAtras, out RaycastHit hit, distance, capasParedes))
        {
            distanciaObjetivo = hit.distance;
        }

        distanciaActual = Mathf.Lerp(distanciaActual, distanciaObjetivo, Time.deltaTime * suavidadAcercamiento);

        transform.position = shoulderPivot + (direccionHaciaAtras * distanciaActual);
        transform.rotation = rotation;
    }
}