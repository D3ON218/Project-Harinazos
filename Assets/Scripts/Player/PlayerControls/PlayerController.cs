using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private PlayerControls controls;
    private Transform camTransform;
    private Animator animator;
    private PlayerCombat combatScript;

    [Header("Movimiento")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float playerRotationSpeed = 10f;
    public float aimRotationSpeed = 25f;

    private float currentSpeed;
    private Vector2 moveInput;
    private Vector3 moveDirection;

    [Header("Físicas")]
    public float gravity = -15f;
    private Vector3 velocity;
    private bool isGrounded;

    [Header("Agachado")]
    public float normalHeight = 2f;
    public float crouchHeight = 1f;
    private bool isCrouching = false;

    [Header("Dash (Rodar)")]
    public float dashSpeed = 10f;
    public float dashTime = 0.8f;
    public bool isDashing = false;

    [Header("Sistema de Cobertura")]
    public float coverCheckDistance = 1.5f;
    public LayerMask coverLayer;
    public bool isInCover = false;
    private Vector3 coverNormal;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();
        animator = GetComponentInChildren<Animator>();
        combatScript = GetComponent<PlayerCombat>();

        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

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

        if (combatScript != null && combatScript.isPerformingAction)
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            if (animator != null) animator.SetFloat("Speed", 0f);

            return; 
        }

        bool isAiming = combatScript != null && combatScript.isAiming;

        if (!isDashing)
        {
            if (isInCover)
            {
                HandleCoverMovement();
            }
            else
            {
                HandleStandardMovement(isAiming);
            }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        UpdateAnimator();
    }

    private void HandleStandardMovement(bool isAiming)
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        bool isSprinting = controls.Player.Sprint.IsPressed() && !isAiming;

        if (isSprinting && isCrouching && moveInput.magnitude > 0.1f)
        {
            isCrouching = false;
            controller.height = normalHeight;
            controller.center = new Vector3(0, controller.height / 2f, 0);
        }

        currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

        if (isAiming && moveInput.y < -0.1f)
        {
            currentSpeed = crouchSpeed * 0.8f;
        }

        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        if (moveInput.magnitude > 0.1f)
        {
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);

            if (isAiming)
            {
                Quaternion aimRotation = Quaternion.LookRotation(forward);
                transform.rotation = Quaternion.Slerp(transform.rotation, aimRotation, aimRotationSpeed * Time.deltaTime);
            }
            else
            {
                Quaternion walkRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, walkRotation, playerRotationSpeed * Time.deltaTime);
            }
        }
        else if (isAiming)
        {
            Quaternion aimRotation = Quaternion.LookRotation(forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, aimRotation, aimRotationSpeed * Time.deltaTime);
        }
    }

    private void HandleCoverMovement()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 coverRight = Vector3.Cross(coverNormal, Vector3.up).normalized;
            float moveDirectionSign = Mathf.Sign(moveInput.x);

            Vector3 lookAheadOrigin = transform.position + Vector3.up * 0.5f + (coverNormal * 0.5f) + (coverRight * moveDirectionSign * 0.3f);
            Vector3 rayDir = (-coverNormal + coverRight * moveDirectionSign * 0.5f).normalized;

            RaycastHit hit;
            if (Physics.Raycast(lookAheadOrigin, rayDir, out hit, coverCheckDistance + 0.5f, coverLayer))
            {
                coverNormal = hit.normal;
                Vector3 coverMoveDir = (coverRight * moveInput.x) + (-coverNormal * 0.5f);
                controller.Move(coverMoveDir * (walkSpeed * 0.7f) * Time.deltaTime);

                transform.rotation = Quaternion.LookRotation(coverNormal);
            }
        }
    }

    private void ToggleCrouch()
    {
        if (combatScript != null && combatScript.isPerformingAction) return; // Bloqueo

        isCrouching = !isCrouching;
        controller.height = isCrouching ? crouchHeight : normalHeight;
        controller.center = new Vector3(0, controller.height / 2f, 0);
    }

    private void HandleCoverOrDash()
    {
        if (combatScript != null && combatScript.isPerformingAction) return; // Bloqueo

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, coverCheckDistance, coverLayer))
        {
            if (!isInCover)
            {
                isInCover = true;
                coverNormal = hit.normal;

                transform.rotation = Quaternion.LookRotation(coverNormal);

                isCrouching = true;
                controller.height = crouchHeight;
                controller.center = new Vector3(0, controller.height / 2f, 0);

                return;
            }
        }

        if (isInCover)
        {
            isInCover = false;
            isCrouching = false;
            controller.height = normalHeight;
            controller.center = new Vector3(0, controller.height / 2f, 0);
            return;
        }

        if (!isDashing && moveInput != Vector2.zero && !(combatScript != null && combatScript.isAiming))
        {
            StartCoroutine(PerformDash());
        }
    }

    private System.Collections.IEnumerator PerformDash()
    {
        isDashing = true;
        float startTime = Time.time;

        if (animator != null) animator.SetTrigger("Dash");

        while (Time.time < startTime + dashTime)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, 0.8f, coverLayer))
            {
                isInCover = true;
                coverNormal = hit.normal;
                transform.rotation = Quaternion.LookRotation(coverNormal);

                isCrouching = true;
                controller.height = crouchHeight;
                controller.center = new Vector3(0, controller.height / 2f, 0);

                isDashing = false;
                yield break;
            }

            controller.Move(moveDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }
        isDashing = false;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float targetAnimSpeed = 0f;
        if (moveInput.magnitude > 0.1f && !isDashing)
        {
            if (isInCover) targetAnimSpeed = walkSpeed * 0.7f;
            else targetAnimSpeed = currentSpeed;
        }

        animator.SetFloat("Speed", targetAnimSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetFloat("Vertical", moveInput.y);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsInCover", isInCover);
    }
}