using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private PlayerControls controls;
    private Transform camTransform; // Referencia automática a la cámara

    // --- AQUÍ ESTÁ EL CEREBRO ---
    private Animator animator;

    [Header("Movimiento")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float playerRotationSpeed = 10f;
    private float currentSpeed;
    private Vector2 moveInput;
    private Vector3 moveDirection;

    [Header("Físicas / Salto")]
    public float gravity = -15f;
    public float jumpHeight = 1.5f;
    private Vector3 velocity;
    private bool isGrounded;

    [Header("Agachado")]
    public float normalHeight = 2f;
    public float crouchHeight = 1f;
    private bool isCrouching = false;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashTime = 0.2f;
    private bool isDashing = false;

    [Header("Sistema de Cobertura")]
    public float coverCheckDistance = 1.5f;
    public LayerMask coverLayer;
    private bool isInCover = false;
    private Vector3 coverNormal;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();

        // Buscar al hijo (Suit) que tiene el Animator
        animator = GetComponentInChildren<Animator>();

        // Buscar la cámara principal automáticamente al iniciar
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        controls.Player.Jump.performed += ctx => Jump();
        controls.Player.Crouch.performed += ctx => ToggleCrouch();
        controls.Player.CoverDash.performed += ctx => HandleCoverOrDash();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (!isDashing)
        {
            if (isInCover)
                HandleCoverMovement();
            else
                HandleStandardMovement();
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // --- ACTUALIZAR EL BLEND TREE Y ESTADOS ---
        UpdateAnimator();
    }

    private void HandleStandardMovement()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        bool isSprinting = controls.Player.Sprint.IsPressed();

        // --- NUEVO: Romper la postura de agachado para salir corriendo directamente ---
        if (isSprinting && isCrouching && moveInput.magnitude > 0.1f)
        {
            isCrouching = false;
            controller.height = normalHeight;
            controller.center = new Vector3(0, controller.height / 2f, 0);
        }

        // Calculamos la velocidad actual según el estado
        currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

        // --- NUEVO: Reducir la velocidad si caminamos hacia atrás por realismo ---
        if (moveInput.y < -0.1f && !isCrouching)
        {
            currentSpeed = walkSpeed * 0.6f;
        }

        // 1. Tomamos la dirección hacia donde mira la cámara
        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;

        // 2. Forzamos a que el eje Y sea cero
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        // 3. El movimiento ahora es RELATIVO a la cámara
        moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        // Aplicar movimiento si hay input
        if (moveInput.magnitude > 0.1f)
        {
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);

            // Rotar al personaje suavemente
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, playerRotationSpeed * Time.deltaTime);
        }
    }

    private void HandleCoverMovement()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        Vector3 coverRight = Vector3.Cross(Vector3.up, coverNormal).normalized;
        Vector3 coverMoveDir = coverRight * moveInput.x;

        if (moveInput.magnitude > 0.1f)
        {
            controller.Move(coverMoveDir * (walkSpeed * 0.7f) * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(-coverNormal);
        }
    }

    private void Jump()
    {
        if (isGrounded && !isCrouching && !isInCover)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // --- NUEVO: ACTIVAR LA ANIMACIÓN DE SALTO ---
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }
    }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        controller.height = isCrouching ? crouchHeight : normalHeight;
        controller.center = new Vector3(0, controller.height / 2f, 0);
    }

    private void HandleCoverOrDash()
    {
        RaycastHit hit;
        // Lanzamos el rayo para buscar cobertura
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, coverCheckDistance, coverLayer))
        {
            if (!isInCover)
            {
                isInCover = true;
                coverNormal = hit.normal;
                transform.rotation = Quaternion.LookRotation(-coverNormal);
                return; // Entra en cobertura y corta la función
            }
        }

        // Si ya está en cobertura, salir de ella
        if (isInCover)
        {
            isInCover = false;
            return;
        }

        // Si no encontró cobertura y no está haciendo dash, hace el Dash
        if (!isDashing && moveInput != Vector2.zero)
        {
            StartCoroutine(PerformDash());
        }
    }

    private System.Collections.IEnumerator PerformDash()
    {
        isDashing = true;
        float startTime = Time.time;

        while (Time.time < startTime + dashTime)
        {
            controller.Move(moveDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }
        isDashing = false;
    }

    // --- FUNCIÓN DEDICADA PARA EL CEREBRO (ANIMATOR) ---
    private void UpdateAnimator()
    {
        if (animator == null) return;

        float targetAnimSpeed = 0f;

        // Si el jugador está tocando las teclas de movimiento
        if (moveInput.magnitude > 0.1f && !isDashing)
        {
            if (isInCover)
            {
                targetAnimSpeed = walkSpeed * 0.7f; // Velocidad de cobertura
            }
            else
            {
                targetAnimSpeed = currentSpeed; // Velocidad dinámica (Caminar, Correr, Agachado, Reversa)
            }
        }

        // Le mandamos el valor de la velocidad al Blend Tree
        animator.SetFloat("Speed", targetAnimSpeed, 0.1f, Time.deltaTime);

        // --- NUEVO: Mapeo de parámetros de estados ---
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetFloat("Vertical", moveInput.y); // Ayuda al Animator a saber si vamos en reversa

        // --- NUEVO: Le decimos al Animator si estamos tocando el suelo ---
        animator.SetBool("IsGrounded", isGrounded);
    }
}