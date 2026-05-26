using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Seguimiento Estilo Gears")]
    public Transform targetToFollow;
    public float distance = 2.5f;        // Qué tan atrás está
    public float height = 1.2f;          // Altura base 
    public float shoulderOffset = 0.8f;  // Desplazamiento lateral (X) para que quede sobre el hombro
    public float sensitivity = 150f;

    private PlayerControls controls;
    private float camXRotation = 0f;
    private float camYRotation = 0f;

    private void Awake()
    {
        controls = new PlayerControls();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void LateUpdate()
    {
        if (targetToFollow == null) return;

        Vector2 lookInput = controls.Player.Look.ReadValue<Vector2>();

        camYRotation += lookInput.x * sensitivity * Time.deltaTime;
        camXRotation -= lookInput.y * sensitivity * Time.deltaTime;
        camXRotation = Mathf.Clamp(camXRotation, -30f, 60f);

        Quaternion rotation = Quaternion.Euler(camXRotation, camYRotation, 0f);

        // ShoulderOffset para que la cámara quede sobre el hombro derecho del personaje
        Vector3 positionOffset = rotation * new Vector3(shoulderOffset, 0f, -distance);
        Vector3 targetHeadPosition = targetToFollow.position + (Vector3.up * height);

        transform.rotation = rotation;
        transform.position = targetHeadPosition + positionOffset;
    }
}