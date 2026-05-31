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

    [Header("Colisión con Paredes")]
    public LayerMask capasParedes;
    public float suavidadAcercamiento = 25f;
    [Tooltip("Grosor de la cámara para que no atraviese las esquinas")]
    public float radioCamara = 0.3f;

    private PlayerControls controls;
    private float camXRotation = 0f;
    private float camYRotation = 0f;

    // EL SECRETO: Guardar solo la distancia (un número escalar), no las coordenadas globales X,Y,Z
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
        // Iniciamos la cámara en su distancia máxima
        distanciaActual = distance;
    }

    private void LateUpdate()
    {
        if (targetToFollow == null) return;

        // 1. Controles de rotación con Mouse/Joystick
        Vector2 lookInput = controls.Player.Look.ReadValue<Vector2>();

        camYRotation += lookInput.x * sensitivity * Time.deltaTime;
        camXRotation -= lookInput.y * sensitivity * Time.deltaTime;
        camXRotation = Mathf.Clamp(camXRotation, -30f, 60f);

        Quaternion rotation = Quaternion.Euler(camXRotation, camYRotation, 0f);

        // 2. Establecemos el punto ancla en tiempo real (El hombro derecho)
        Vector3 targetHeadPosition = targetToFollow.position + (Vector3.up * height);
        Vector3 shoulderPivot = targetHeadPosition + (rotation * new Vector3(shoulderOffset, 0f, 0f));

        // 3. Calculamos la dirección estricta hacia atrás desde la lente
        Vector3 direccionHaciaAtras = rotation * new Vector3(0f, 0f, -1f);

        // 4. Asumimos primero que la distancia será la máxima
        float distanciaObjetivo = distance;

        // Lanzamos el detector (SphereCast). Si choca con el muro, recortamos la distanciaObjetivo
        if (Physics.SphereCast(shoulderPivot, radioCamara, direccionHaciaAtras, out RaycastHit hit, distance, capasParedes))
        {
            distanciaObjetivo = hit.distance;
        }

        // 5. Suavizamos SOLO el tamaño del palo retráctil (adiós temblores al estar emparentada)
        distanciaActual = Mathf.Lerp(distanciaActual, distanciaObjetivo, Time.deltaTime * suavidadAcercamiento);

        // 6. Aplicamos la posición final empujando la cámara hacia atrás desde el hombro
        transform.position = shoulderPivot + (direccionHaciaAtras * distanciaActual);
        transform.rotation = rotation;
    }
}