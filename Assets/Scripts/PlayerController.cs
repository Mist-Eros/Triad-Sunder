using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;

    public enum MoveState { Grounded, Airborne, Sliding }

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Transform cameraTransform;

    private MoveState currentState = MoveState.Airborne;
    private Vector3 moveInput;
    private bool wantsJump;
    private bool wantsCrouch;
    private bool wantsSprint;
    private bool jumpPressedThisFrame;

    private bool isGrounded;
    private int airJumpsRemaining;
    private float lastJumpTime = -1f;
    private float lastGroundedTime = -1f;

    private float currentStamina;

    private float lastWallJumpTime = -1f;
    private bool wallJumpUsedThisAir;

    private float verticalRotation = 0f;
    private float mouseDeltaX;

    private Vector3 originalScale;
    private float targetCrouchScaleY;

    public float CurrentStamina => currentStamina;
    public int CurrentStaminaPills => Mathf.FloorToInt(currentStamina);
    public int MaxStaminaPills => GetStats().maxStaminaPills;
    public MoveState CurrentMoveState => currentState;
    public bool IsGrounded => isGrounded;
    public bool IsSliding => currentState == MoveState.Sliding;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        cameraTransform = GetComponentInChildren<Camera>()?.transform;

        if (rb == null || capsuleCollider == null || cameraTransform == null)
        {
            Debug.LogError("PlayerController requires Rigidbody, CapsuleCollider, and child Camera.");
            enabled = false;
            return;
        }

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        originalScale = transform.localScale;
        targetCrouchScaleY = originalScale.y;

        var stats = GetStats();
        currentStamina = stats.maxStaminaPills;
        airJumpsRemaining = stats.maxAirJumps;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        GatherMouseInput();
        GatherInput();
        RegenerateStamina();
        UpdateCrouchScale();

        if (Input.GetButtonDown("Jump"))
        {
            jumpPressedThisFrame = true;
            wantsJump = true;
        }
        else if (Input.GetButtonUp("Jump"))
        {
            wantsJump = false;
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();
        DrainSprintStamina();
        UpdateState();
        ApplyMovement();
        ApplyHorizontalRotation();
        jumpPressedThisFrame = false;
    }

    void LateUpdate()
    {
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private PlayerStats GetStats()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.Stats;
        return _fallbackStats ?? (_fallbackStats = new PlayerStats());
    }
    private static PlayerStats _fallbackStats;

    private void GatherInput()
    {
        moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        wantsSprint = Input.GetKey(KeyCode.LeftShift);
        wantsCrouch = Input.GetKey(KeyCode.LeftControl);
    }

    private void GatherMouseInput()
    {
        mouseDeltaX += Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
    }

    private void ApplyHorizontalRotation()
    {
        if (Mathf.Abs(mouseDeltaX) > 0.0001f)
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, mouseDeltaX, 0f));
        mouseDeltaX = 0f;
    }

    private void CheckGrounded()
    {
        var stats = GetStats();
        float halfHeight = (capsuleCollider.height * 0.5f) * transform.localScale.y;
        float rayLength = halfHeight + stats.GroundCheckMargin();
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayLength, groundMask);

        Debug.DrawRay(transform.position, Vector3.down * rayLength,
            isGrounded ? Color.green : Color.red);

        if (isGrounded)
        {
            if (!wasGrounded)
                OnLand();
            lastGroundedTime = Time.time;
            airJumpsRemaining = stats.maxAirJumps;
            wallJumpUsedThisAir = false;
        }
    }

    private bool CoyoteGrounded()
    {
        var stats = GetStats();
        return isGrounded || (Time.time - lastGroundedTime < stats.coyoteTime);
    }

    private void OnLand() { }

    private bool ConsumeStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            return true;
        }
        return false;
    }

    private bool HasStamina(float amount)
    {
        return currentStamina >= amount;
    }

    private void RegenerateStamina()
    {
        var stats = GetStats();
        if (currentStamina < stats.maxStaminaPills)
        {
            bool isSprinting = wantsSprint && currentStamina > 0f && moveInput.sqrMagnitude > 0.01f;
            if (!isSprinting)
            {
                currentStamina += Time.deltaTime / stats.staminaRegenTime;
                currentStamina = Mathf.Min(currentStamina, stats.maxStaminaPills);
            }
        }
    }

    private void DrainSprintStamina()
    {
        var stats = GetStats();
        bool isActuallySprinting = currentState == MoveState.Grounded
                                   && wantsSprint
                                   && moveInput.sqrMagnitude > 0.01f
                                   && currentStamina > 0f;

        if (isActuallySprinting)
        {
            currentStamina -= Time.fixedDeltaTime / stats.sprintStaminaDrainTime;
            if (currentStamina < 0f)
                currentStamina = 0f;
        }
    }

    private bool CanSprint()
    {
        return wantsSprint && currentStamina > 0f;
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case MoveState.Grounded:
                UpdateGroundedState();
                break;
            case MoveState.Airborne:
                UpdateAirborneState();
                break;
            case MoveState.Sliding:
                UpdateSlidingState();
                break;
        }
    }

    private void TransitionTo(MoveState newState)
    {
        if (currentState == MoveState.Sliding && newState != MoveState.Sliding) { }
        currentState = newState;
        if (newState == MoveState.Sliding)
            EnterSlide();
    }

    private void UpdateGroundedState()
    {
        if (!isGrounded)
        {
            TransitionTo(MoveState.Airborne);
            return;
        }

        var stats = GetStats();

        if (jumpPressedThisFrame && Time.time - lastJumpTime > stats.jumpCooldown)
        {
            DoJump(stats.jumpForce);
            TransitionTo(MoveState.Airborne);
            return;
        }

        if (wantsCrouch && wantsSprint && moveInput.sqrMagnitude > 0.01f)
        {
            if (HasStamina(stats.slideStaminaCost))
            {
                TransitionTo(MoveState.Sliding);
                return;
            }
        }
    }

    private void UpdateAirborneState()
    {
        var stats = GetStats();

        if (isGrounded && rb.velocity.y <= 0.1f)
        {
            TransitionTo(MoveState.Grounded);
            return;
        }

        if (jumpPressedThisFrame && airJumpsRemaining > 0
            && Time.time - lastJumpTime > stats.jumpCooldown)
        {
            if (HasStamina(stats.airJumpStaminaCost))
            {
                ConsumeStamina(stats.airJumpStaminaCost);
                DoJump(stats.jumpForce);
                airJumpsRemaining--;
                return;
            }
        }

        if (jumpPressedThisFrame
            && Time.time - lastWallJumpTime > stats.wallJumpCooldown
            && !wallJumpUsedThisAir
            && HasStamina(stats.wallJumpStaminaCost)
            && IsNearWall(out Vector3 wallNormal))
        {
            ConsumeStamina(stats.wallJumpStaminaCost);
            DoWallJump(wallNormal);
            return;
        }
    }

    private void EnterSlide()
    {
        var stats = GetStats();
        ConsumeStamina(stats.slideStaminaCost);

        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float currentSpeed = horizontalVel.magnitude;

        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 inputDir = (camForward * moveInput.z + camRight * moveInput.x).normalized;
        if (inputDir.sqrMagnitude < 0.01f)
            inputDir = camForward;

        if (currentSpeed < stats.slideMinSpeed)
            horizontalVel = inputDir * stats.slideMinSpeed;
        else
            horizontalVel += inputDir * stats.slideBoost;

        horizontalVel = Vector3.ClampMagnitude(horizontalVel, stats.slideMaxSpeed);
        rb.velocity = new Vector3(horizontalVel.x, rb.velocity.y, horizontalVel.z);
    }

    private void UpdateSlidingState()
    {
        var stats = GetStats();

        if (jumpPressedThisFrame && Time.time - lastJumpTime > stats.jumpCooldown)
        {
            DoSlideJump();
            TransitionTo(MoveState.Airborne);
            return;
        }

        if (!wantsCrouch)
        {
            TransitionTo(isGrounded ? MoveState.Grounded : MoveState.Airborne);
            return;
        }

        float currentSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        if (currentSpeed < stats.walkSpeed * 0.4f && isGrounded)
        {
            TransitionTo(MoveState.Grounded);
            return;
        }

        if (!isGrounded && rb.velocity.y < -0.5f)
        {
            TransitionTo(MoveState.Airborne);
            return;
        }
    }

    private void DoJump(float force)
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * force, ForceMode.VelocityChange);
        lastJumpTime = Time.time;
    }

    private void DoSlideJump()
    {
        var stats = GetStats();
        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float speed = horizontalVel.magnitude;
        Vector3 direction = speed > 0.1f ? horizontalVel.normalized : transform.forward;

        float boostedSpeed = Mathf.Max(speed, stats.slideMinSpeed) * stats.slideJumpBoostMultiplier;
        boostedSpeed = Mathf.Min(boostedSpeed, stats.slideMaxSpeed * 2f);

        rb.velocity = new Vector3(direction.x * boostedSpeed, 0f, direction.z * boostedSpeed);
        rb.AddForce(Vector3.up * stats.jumpForce, ForceMode.VelocityChange);
        lastJumpTime = Time.time;
    }

    private void DoWallJump(Vector3 wallNormal)
    {
        var stats = GetStats();

        Vector3 jumpDir = (wallNormal * stats.wallJumpAwayForce
                           + Vector3.up * stats.wallJumpUpForce).normalized;
        float magnitude = Mathf.Sqrt(
            stats.wallJumpAwayForce * stats.wallJumpAwayForce
            + stats.wallJumpUpForce * stats.wallJumpUpForce);

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(jumpDir * magnitude, ForceMode.VelocityChange);

        float awaySpeed = Vector3.Dot(rb.velocity, wallNormal);
        if (awaySpeed < stats.wallJumpMinVelocity)
            rb.velocity += wallNormal * (stats.wallJumpMinVelocity - awaySpeed);

        lastWallJumpTime = Time.time;
        lastJumpTime = Time.time;
        wallJumpUsedThisAir = true;
        airJumpsRemaining = stats.maxAirJumps;
    }

    private bool IsNearWall(out Vector3 wallNormal)
    {
        wallNormal = Vector3.zero;
        var stats = GetStats();

        Vector3 center = transform.TransformPoint(capsuleCollider.center);
        float halfHeight = (capsuleCollider.height * 0.5f) * transform.localScale.y;
        float radius = capsuleCollider.radius * transform.localScale.y;
        Vector3 top = center + Vector3.up * (halfHeight - radius);
        Vector3 bottom = center - Vector3.up * (halfHeight - radius);

        Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

        float bestDot = -1f;
        foreach (Vector3 dir in dirs)
        {
            Vector3 worldDir = transform.TransformDirection(dir);
            if (Physics.CapsuleCast(top, bottom, radius, worldDir, out RaycastHit hit,
                stats.wallCheckDistance, groundMask))
            {
                float dot = Vector3.Dot(-worldDir, hit.normal);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    wallNormal = hit.normal;
                }
            }
        }

        return bestDot > 0f;
    }

    private void ApplyMovement()
    {
        switch (currentState)
        {
            case MoveState.Grounded:
                ApplyGroundedMovement();
                break;
            case MoveState.Airborne:
                ApplyAirborneMovement();
                break;
            case MoveState.Sliding:
                ApplySlidingMovement();
                break;
        }
    }

    private void ApplyGroundedMovement()
    {
        var stats = GetStats();

        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 inputDir = (camForward * moveInput.z + camRight * moveInput.x).normalized;

        bool sprinting = CanSprint();
        float targetSpeed;

        if (wantsCrouch && !wantsSprint)
            targetSpeed = stats.crouchSpeed;
        else
            targetSpeed = sprinting ? stats.sprintSpeed : stats.walkSpeed;

        Vector3 targetVelocity = inputDir * targetSpeed;
        Vector3 currentHoriz = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 newHoriz = Vector3.MoveTowards(currentHoriz, targetVelocity,
                stats.groundAcceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector3(newHoriz.x, rb.velocity.y, newHoriz.z);
        }
        else
        {
            Vector3 newHoriz = Vector3.MoveTowards(currentHoriz, Vector3.zero,
                stats.groundFriction * Time.fixedDeltaTime);
            rb.velocity = new Vector3(newHoriz.x, rb.velocity.y, newHoriz.z);
        }

        if (isGrounded && rb.velocity.y < 0f)
            rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(rb.velocity.y, -2f), rb.velocity.z);
    }

    private void ApplyAirborneMovement()
    {
        var stats = GetStats();

        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 inputDir = (camForward * moveInput.z + camRight * moveInput.x).normalized;

        Vector3 currentHoriz = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (moveInput.sqrMagnitude > 0.01f)
        {
            float airAccel = stats.airAcceleration;
            float airMax = stats.airMaxSpeed;

            float currentSpeedInInputDir = Vector3.Dot(currentHoriz, inputDir);
            float addSpeed = airMax - currentSpeedInInputDir;
            if (addSpeed > 0f)
            {
                float accelSpeed = airAccel * Time.fixedDeltaTime;
                if (accelSpeed > addSpeed) accelSpeed = addSpeed;
                currentHoriz += inputDir * accelSpeed;
            }

            if (currentHoriz.magnitude > airMax)
                currentHoriz = currentHoriz.normalized * airMax;

            rb.velocity = new Vector3(currentHoriz.x, rb.velocity.y, currentHoriz.z);
        }
    }

    private void ApplySlidingMovement()
    {
        var stats = GetStats();
        Vector3 currentHoriz = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float speed = currentHoriz.magnitude;

        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 inputDir = (camForward * moveInput.z + camRight * moveInput.x).normalized;

        if (inputDir.sqrMagnitude > 0.01f && speed > 0.1f)
        {
            Vector3 currentDir = currentHoriz.normalized;
            Vector3 newDir = Vector3.RotateTowards(currentDir, inputDir,
                stats.slideSteering * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
            currentHoriz = newDir * speed;
        }

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f, groundMask))
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (slopeAngle > 5f)
            {
                Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                currentHoriz += slopeDir * stats.slopeSlideAccel * Time.fixedDeltaTime;
            }
            else if (speed > stats.slideMinSpeed * 0.5f)
            {
                currentHoriz -= currentHoriz.normalized * stats.slideFriction * Time.fixedDeltaTime;
            }
        }
        else
        {
            currentHoriz -= currentHoriz.normalized * (stats.slideFriction * 0.3f) * Time.fixedDeltaTime;
        }

        float newSpeed = currentHoriz.magnitude;
        if (newSpeed > stats.slideMaxSpeed)
            currentHoriz = currentHoriz.normalized * stats.slideMaxSpeed;

        rb.velocity = new Vector3(currentHoriz.x, rb.velocity.y, currentHoriz.z);

        if (isGrounded && rb.velocity.y < 0f)
            rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(rb.velocity.y, -1f), rb.velocity.z);
    }

    private void UpdateCrouchScale()
    {
        var stats = GetStats();
        targetCrouchScaleY = wantsCrouch ? stats.crouchScaleY : originalScale.y;
        Vector3 target = new Vector3(originalScale.x, targetCrouchScaleY, originalScale.z);
        transform.localScale = Vector3.Lerp(transform.localScale, target, 15f * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null) return;

        var stats = GetStats();

        Vector3 center = transform.TransformPoint(capsuleCollider.center);
        Gizmos.color = Color.yellow;
        Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (Vector3 dir in dirs)
            Gizmos.DrawRay(center, transform.TransformDirection(dir) * stats.wallCheckDistance);

        float halfHeight = (capsuleCollider.height * 0.5f) * transform.localScale.y;
        float rayLength = halfHeight + stats.GroundCheckMargin();
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector3.down * rayLength);
    }
}

public static class PlayerStatsExtensions
{
    public static float GroundCheckMargin(this PlayerStats stats)
    {
        return 0.08f;
    }
}
