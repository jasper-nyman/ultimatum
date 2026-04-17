using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerVariables))]

// PlayerController handles player movement and looking by reading input actions and
// applying them to a Rigidbody and the Camera. It queries permissions from the
// PlayerVariables component so other systems can enable/disable movement or look.
public class PlayerController : MonoBehaviour
{
    // Cached references to commonly-used components
    private PlayerVariables var; // holds flags and parameters for movement/looking
    private Rigidbody rb; // physics rigidbody used for movement
    private PlayerManager manager; // optional manager that handles higher-level states

    // Movement and look values populated by input callbacks
    [HideInInspector] public Vector2 move; // input vector for movement (x = strafe, y = forward)
    [HideInInspector] public float moveSpeed; // actual move speed applied (initialized from PlayerVariables)
    private Vector2 look; // input vector for mouse/gamepad look

    private void Start()
    {
        // Cache components on Start for efficiency
        rb = GetComponent<Rigidbody>();
        var = GetComponent<PlayerVariables>();
        manager = GetComponent<PlayerManager>();

        // Initialize moveSpeed from player variables so it can be modified in the inspector
        moveSpeed = var.moveSpeed;
    }

    private void Update()
    {
        // Handle player look/rotation. Only apply look if the player is active (not in UI etc.)
        if (var.isActive)
        {
            // Rotate the player horizontally based on the look.x input
            transform.Rotate(Vector3.up * look.x * var.lookSensitivity * Time.deltaTime);

            // Rotate the camera vertically via CameraController (if present)
            CameraController cc = Camera.main.GetComponent<CameraController>();

            if (cc != null)
            {
                // Adjust camera pitch and clamp to avoid flipping
                cc.rotation.x -= look.y * var.lookSensitivity * Time.deltaTime;
                cc.rotation.x = Mathf.Clamp(cc.rotation.x, -90f, 90f);
                cc.rotation.y = transform.eulerAngles.y; // keep camera yaw in sync with player
            }
        }

        // Falling and other physics-related state updates are handled in PlayerManager
    }

    private void FixedUpdate()
    {
        // FixedUpdate is used for physics-driven movement. Compute a movement vector in world space
        // using the player's transform and apply it to the rigidbody's linearVelocity.
        Vector3 movement = (transform.right * move.x + transform.forward * move.y) * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    // Input callback invoked by the Input System for movement (Vector2)
    public void Move(InputAction.CallbackContext context)
    {
        if (var.isActive && var.canMove)
        {
            // When movement is allowed, read the input vector and store it for FixedUpdate
            move = context.ReadValue<Vector2>();
        }
        else
        {
            // If movement is disabled by other systems, zero the movement input to stop the player
            move = Vector2.zero;
        }
    }

    // Input callback invoked by the Input System for look (Vector2)
    public void Look(InputAction.CallbackContext context)
    {
        if (var.canLook)
        {
            // Store look vector which will be applied in Update
            look = context.ReadValue<Vector2>();
        }
        else
        {
            // If looking is disabled, ignore input
            look = Vector2.zero;
        }
    }

    // Crouch input is forwarded to PlayerManager which owns the crouch implementation
    public void Crouch(InputAction.CallbackContext context)
    {
        if (!var.canCrouch)
            return;

        if (manager != null)
        {
            if (context.started)
                manager.SetCrouchQueued(true);
            else if (context.canceled)
                manager.SetCrouchQueued(false);
        }
    }

    // Jump input applies vertical velocity if allowed and the player is grounded
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started && var.canJump && var.isGrounded && !var.isCrouching)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, var.jumpForce, rb.linearVelocity.z);
            var.isJumping = true;
        }
    }

    // Sprint toggles sprinting state on the PlayerVariables which other systems may query
    public void Sprint(InputAction.CallbackContext context)
    {
        if (var.canSprint)
        {
            if (context.started && var.stamina > 0f)
            {
                var.isSprinting = true;
            }
            else if (context.canceled)
            {
                var.isSprinting = false;
            }
        }
    }
}
