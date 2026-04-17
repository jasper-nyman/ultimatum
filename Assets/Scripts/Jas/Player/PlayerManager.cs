using UnityEngine;

[RequireComponent(typeof(PlayerVariables))]
public class PlayerManager : MonoBehaviour
{
    // References
    private Rigidbody rb;
    private PlayerVariables var;
    private PlayerController ctrl;
    private CameraTracker tracker;

    // Crouch queue (exposed for PlayerController to set)
    private bool crouchQueued;

    void Start()
    {
        // Get references
        rb = GetComponent<Rigidbody>();
        var = GetComponent<PlayerVariables>();
        ctrl = GetComponent<PlayerController>();
        tracker = GetComponent<CameraTracker>();

        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Update camera tracker height if available
        if (tracker != null)
            tracker.positionOffset.y = 1.8f * transform.localScale.y;

        // Movement flag
        if (ctrl != null)
            var.isMoving = ctrl.move != Vector2.zero;

        // Check if the player is grounded
        var.isGrounded = Physics.OverlapBox(
            transform.position,
            new Vector3(0.5f, 0.1f, 0.5f),
            transform.rotation,
            LayerMask.GetMask("Surface")
        ).Length > 0;

        // Head (overhead) check: use approximately half-height so the check sits near the head
        float headHeight = 2 * transform.localScale.y;
        Vector3 offset = new Vector3(0, headHeight, 0);

        var.isUnderObject = Physics.OverlapBox(
            transform.position + offset,
            new Vector3(0.5f, 0.1f, 0.5f),
            transform.rotation,
            LayerMask.GetMask("Surface")
        ).Length > 0;

        // Determine crouch state using queued input, grounding and overhead
        bool shouldCrouch = false;

        if (crouchQueued)
        {
            if (var.canCrouch && var.isGrounded)
                shouldCrouch = true;
        }

        // If already crouching and an object is overhead, remain crouched
        if (var.isCrouching && var.isUnderObject)
            shouldCrouch = true;

        var.isCrouching = shouldCrouch;

        // Apply scale changes for crouching / standing
        if (var.isCrouching)
        {
            transform.localScale = Vector3.MoveTowards(
                transform.localScale,
                new Vector3(1f, var.crouchHeight, 1f),
                Time.deltaTime / var.crouchSpeed
            );
        }
        else
        {
            if (!var.isUnderObject)
            {
                transform.localScale = Vector3.MoveTowards(
                    transform.localScale,
                    new Vector3(1f, 1f, 1f),
                    Time.deltaTime / var.crouchSpeed
                );
            }
        }

        // If crouching is disabled while crouched and no overhead, stand up
        if (!var.canCrouch && var.isCrouching && !var.isUnderObject)
        {
            var.isCrouching = false;
        }

        // Falling / jumping state
        if (!var.isGrounded)
        {
            if (rb != null && rb.linearVelocity.y < 0f)
                var.isFalling = true;
            else
                var.isFalling = false;
        }
        else
        {
            var.isFalling = false;
            var.isJumping = false;
        }

        // Update move speed based on sprinting and crouching states
        if (ctrl != null)
        {
            if (!var.isCrouching)
            {
                ctrl.moveSpeed = var.isSprinting ? var.sprintSpeed : var.moveSpeed;
            }
            else
            {
                ctrl.moveSpeed = var.moveSpeed / 2f;
            }
        }

        if (var.isSprinting)
        {
            // Drain stamina while sprinting
            var.stamina -= var.staminaDrainRate * Time.deltaTime;
            if (var.stamina <= 0f)
            {
                var.stamina = 0f;
                var.isSprinting = false; // stop sprinting when stamina runs out
            }
        }
        else if (!var.isMoving)
        {
            // Regenerate stamina when not sprinting
            if (var.stamina < 100f)
            {
                var.stamina += var.staminaRegenRate * Time.deltaTime;
            }
        }
    }
    
    public void SetCrouchQueued(bool queued)
    {
        crouchQueued = queued;
    }
}
