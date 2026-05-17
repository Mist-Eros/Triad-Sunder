using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float slideSpeed = 10f;      // speed while sliding
    [SerializeField] private float jumpForce = 5f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchScaleY = 0.5f;   // Y scale when crouching

    [Header("Stamina (Phases)")]
    [SerializeField] private int maxStamina = 3;                  // total stamina points (phases)
    [SerializeField] private float staminaRegenTime = 5f;         // seconds to recover 1 point
    [SerializeField] private float jumpStaminaCost = 1f;          // cost per jump (should be 1)
    // Sliding also costs 1 point (hardcoded as 1, see entry cost logic)

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckMargin = 0.05f;
    [SerializeField] private LayerMask groundMask = ~0;

    // Component references
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Transform cameraTransform;

    // State
    private float verticalRotation = 0f;
    private bool isGrounded;
    [HideInInspector] public float currentStamina;
    private Vector3 originalScale;
    private int jumpsRemaining;            // double jump counter (2 when grounded)
    private bool wasSliding;               // to detect slide entry
    private bool wallJumpUsed;              // wall jump used this airtime

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        cameraTransform = GetComponentInChildren<Camera>()?.transform;

        if (rb == null || capsuleCollider == null || cameraTransform == null)
        {
            Debug.LogError("Movement script requires Rigidbody, CapsuleCollider, and a child Camera.");
            enabled = false;
            return;
        }

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        originalScale = transform.localScale;
        currentStamina = maxStamina;
        jumpsRemaining = 1;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleStaminaRegen();

        // Jump (double jump allowed)
        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0 && currentStamina >= jumpStaminaCost)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            currentStamina -= jumpStaminaCost;
            jumpsRemaining--;
        }

        UpdateCrouchScale();
    }

    void FixedUpdate()
    {
        CheckGrounded();
        HandleMovement();
    }

    // ------------------------------------------------------------
    // Mouse Look
    // ------------------------------------------------------------
    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    // ------------------------------------------------------------
    // Ground Check (resets double jump)
    // ------------------------------------------------------------
    private void CheckGrounded()
    {
        float halfHeight = (capsuleCollider.height * 0.5f) * transform.localScale.y;
        float rayLength = halfHeight + groundCheckMargin;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayLength, groundMask);
        Debug.DrawRay(transform.position, Vector3.down * rayLength, isGrounded ? Color.green : Color.red);

        if (isGrounded)
        {
            jumpsRemaining = 1;
            wallJumpUsed = false;
        }
        else if (jumpsRemaining == 0 && IsNearWall() && !wallJumpUsed)
        {
            jumpsRemaining = 1;
            wallJumpUsed = true;
        }
    }

    // ------------------------------------------------------------
    // Wall Detection (for wall jumps)
    // ------------------------------------------------------------
    private bool IsNearWall()
    {
        float checkDistance = 1.5f;
        Vector3 center = transform.TransformPoint(capsuleCollider.center);
        Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

        foreach (Vector3 dir in dirs)
        {
            if (Physics.Raycast(center, transform.TransformDirection(dir), checkDistance, groundMask))
                return true;
        }
        return false;
    }

    // ------------------------------------------------------------
    // Movement & States (crouch/slide allowed anywhere)
    // ------------------------------------------------------------
    private void HandleMovement()
    {
        float hor = Input.GetAxis("Horizontal");
        float ver = Input.GetAxis("Vertical");

        // Determine desired states
        bool wantSprint = Input.GetKey(KeyCode.LeftShift);
        bool wantCrouch = Input.GetKey(KeyCode.LeftControl);

        // Slide toggle: requires both keys to start, but only crouch to keep sliding
        bool canSlide = (currentStamina >= 1f) || wasSliding;
        bool isSliding = wasSliding ? (wantCrouch && canSlide) : (wantSprint && wantCrouch && canSlide);
        bool isSprinting = wantSprint && !wantCrouch;
        // (crouching alone is just wantCrouch, no extra speed)

        // Detect slide entry for one-time stamina cost
        if (isSliding && !wasSliding)
        {
            currentStamina -= 1f;   // slide costs 1 phase
        }

        // Camera-relative input direction (projected onto XZ plane)
        Vector3 inputDir = (transform.right * hor + transform.forward * ver).normalized;

        // Compute desired horizontal velocity
        Vector3 newHorizontalVel = Vector3.zero;
        bool applyNewVel = false;

        if (isSliding)
        {
            // Slide always overrides – constant speed in camera forward direction
            Vector3 slideDir = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            newHorizontalVel = slideDir * slideSpeed;
            applyNewVel = true;
        }
        else if (inputDir.sqrMagnitude > 0.001f)
        {
            // Player is giving input – walk, sprint, or crouch-walk
            float speed = walkSpeed;
            if (isSprinting) speed = sprintSpeed;
            // (crouching alone just uses walk speed)
            newHorizontalVel = inputDir * speed;
            applyNewVel = true;
        }
        else
        {
            // No input
            if (isGrounded)
            {
                // Stop instantly when on the ground
                newHorizontalVel = Vector3.zero;
                applyNewVel = true;
            }
            // else airborne → do nothing (preserve existing horizontal momentum)
        }

        if (applyNewVel)
        {
            Vector3 targetVelocity = new Vector3(newHorizontalVel.x, rb.velocity.y, newHorizontalVel.z);

            Vector3 horizontalMove = new Vector3(newHorizontalVel.x, 0, newHorizontalVel.z);
            if (horizontalMove.sqrMagnitude > 0.001f)
            {
                Vector3 center = transform.TransformPoint(capsuleCollider.center);
                float radius = capsuleCollider.radius * transform.localScale.y;
                float height = capsuleCollider.height * transform.localScale.y;
                Vector3 top = center + Vector3.up * (height * 0.5f - radius);
                Vector3 bottom = center - Vector3.up * (height * 0.5f - radius);
                float castDist = horizontalMove.magnitude * Time.fixedDeltaTime + 0.1f;

                if (Physics.CapsuleCast(top, bottom, radius, horizontalMove.normalized,
                        out RaycastHit hit, castDist, groundMask))
                {
                    float intoWall = Vector3.Dot(horizontalMove, hit.normal);
                    if (intoWall < 0f)
                    {
                        targetVelocity -= new Vector3(
                            hit.normal.x * intoWall,
                            0f,
                            hit.normal.z * intoWall
                        );
                    }
                }
            }

            rb.velocity = targetVelocity;
        }

        wasSliding = isSliding;   // remember for next frame
    }

    // ------------------------------------------------------------
    // Crouch Scaling (always when holding Ctrl)
    // ------------------------------------------------------------
    private void UpdateCrouchScale()
    {
        bool scaleDown = Input.GetKey(KeyCode.LeftControl);
        Vector3 targetScale = scaleDown
            ? new Vector3(originalScale.x, crouchScaleY, originalScale.z)
            : originalScale;

        transform.localScale = targetScale;
    }

    // ------------------------------------------------------------
    // Stamina Regeneration (only continuous refill, no drain)
    // ------------------------------------------------------------
    private void HandleStaminaRegen()
    {
        if (currentStamina < maxStamina)
        {
            float regenRate = 1f / staminaRegenTime;   // points per second
            currentStamina += regenRate * Time.deltaTime;
            if (currentStamina > maxStamina)
                currentStamina = maxStamina;
        }
    }
}
