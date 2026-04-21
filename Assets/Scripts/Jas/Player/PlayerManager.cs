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
    private PlayerStamina stamina;

    // Stamina drain/regeneration rates (units per second)
    [Tooltip("Stamina drained per second while sprinting and moving.")]
    public float sprintStaminaDrainRate = 15f;
    [Tooltip("Stamina regenerated per second when not sprinting.")]
    public float staminaRegenRate = 10f;
    // Fraction of max stamina required before sprinting is re-enabled after depletion
    private float sprintResumeFraction = 0.2f;

    void Start()
    {
        // Get references
        rb = GetComponent<Rigidbody>();
        var = GetComponent<PlayerVariables>();
        ctrl = GetComponent<PlayerController>();
        tracker = GetComponent<CameraTracker>();

        // Ensure a PlayerStamina component exists so sprinting drains/regenerates stamina
        stamina = GetComponent<PlayerStamina>();
        if (stamina == null)
        {
            stamina = gameObject.AddComponent<PlayerStamina>();
        }

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

        // Stamina drain and regen logic
        if (stamina != null)
        {
            // If player is sprinting and moving, drain stamina
            if (var.isSprinting && var.isMoving && var.canSprint)
            {
                stamina.stamina -= sprintStaminaDrainRate * Time.deltaTime;
                if (stamina.stamina <= 0f)
                {
                    stamina.stamina = 0f;
                    // Stop sprinting and temporarily disable sprint until stamina recovers
                    var.isSprinting = false;
                    var.canSprint = false;
                }
            }
            else
            {
                // Regenerate stamina when not sprinting
                stamina.stamina += staminaRegenRate * Time.deltaTime;
                if (stamina.stamina >= stamina.maxStamina * sprintResumeFraction)
                {
                    // Re-enable sprint when sufficient stamina recovered
                    var.canSprint = true;
                }
            }

            // Clamp stamina
            stamina.stamina = Mathf.Clamp(stamina.stamina, 0f, stamina.maxStamina);
        }

        // Note: stamina is handled via the PlayerStamina component above.
    }
    
    public void SetCrouchQueued(bool queued)
    {
        crouchQueued = queued;
    }
}
