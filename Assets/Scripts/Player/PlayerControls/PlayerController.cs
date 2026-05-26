using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private PlayerControls controls;

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
    }

    private void HandleStandardMovement()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        bool isSprinting = controls.Player.Sprint.IsPressed();
        currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

        moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (moveInput != Vector2.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, playerRotationSpeed * Time.deltaTime);
        }
    }

    private void Jump()
    {
        if (isGrounded && !isCrouching && !isInCover)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, coverCheckDistance, coverLayer))
        {
            if (!isInCover)
            {
                isInCover = true;
                coverNormal = hit.normal;
                transform.rotation = Quaternion.LookRotation(-coverNormal);
                return;
            }
        }

        if (isInCover)
        {
            isInCover = false;
            return;
        }

        if (!isDashing && moveInput != Vector2.zero)
            StartCoroutine(PerformDash());
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

    private void HandleCoverMovement()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        Vector3 coverRight = Vector3.Cross(Vector3.up, coverNormal).normalized;
        Vector3 coverMoveDir = coverRight * moveInput.x;

        controller.Move(coverMoveDir * (walkSpeed * 0.7f) * Time.deltaTime);

        if (coverMoveDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(-coverNormal);
    }
}