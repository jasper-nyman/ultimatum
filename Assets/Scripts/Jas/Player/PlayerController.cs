using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // References
    private PlayerVariables var;
    private Rigidbody rb;

    // Parameters
    private Vector2 movement;
    private float moveSpeed;
    private Vector2 look;
    private bool crouchTriggered;

    private void Awake()
    {
        // Lock the cursor to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Get references to player variables and rigidbody
        var = GetComponent<PlayerVariables>();
        rb = var.rb;

        // Initialize move speed from player variables
        moveSpeed = var.moveSpeed;
    }

    private void Update()
    {
        // Move speed
        if (!var.isCrouching)
        {
            // Set move speed based on sprinting state
            if (var.isSprinting)
            {
                moveSpeed = var.sprintSpeed;
            }
            else
            {
                moveSpeed = var.moveSpeed;
            }
        }
        else
        {
            // Reduce move speed while crouching
            moveSpeed = var.moveSpeed / 2;
        }

        // Look
        if (var.isActive)
        {
            // Rotate the player and camera based on look input and sensitivity
            transform.Rotate(Vector3.up * look.x * var.lookSensitivity * Time.deltaTime);
            CameraController cc = Camera.main.GetComponent<CameraController>();
            if (cc != null)
            {
                // Pitch (x) is decreased by look.y to invert vertical look if desired
                cc.rotation.x -= look.y * var.lookSensitivity * Time.deltaTime;

                // Clamp pitch to avoid flipping (common camera constraint)
                cc.rotation.x = Mathf.Clamp(cc.rotation.x, -90f, 90f);

                // Use Euler angles (degrees) for yaw. transform.eulerAngles.y is the correct yaw in degrees.
                cc.rotation.y = transform.eulerAngles.y;
            }
        }

        var.isGrounded = Physics.OverlapBox(transform.position + new Vector3(0, 0.1f, 0), new Vector3(0.5f, 0.1f, 0.5f), Quaternion.identity, var.surfaceLayer).Length > 0;
        float headHeight = 2 * transform.localScale.y;
        var.isUnderObject = Physics.OverlapBox(transform.position + new Vector3(0, headHeight - 0.1f, 0), new Vector3(0.5f, 0.1f, 0.5f), Quaternion.identity, var.surfaceLayer).Length > 0;

        // Crouching
        if (var.isCrouching)
        {
            // Crouch down
            transform.localScale = Vector3.MoveTowards(
                transform.localScale,
                new Vector3(1, var.crouchHeight, 1),
                Time.deltaTime / var.crouchSpeed
            );

            if (!crouchTriggered && !var.isUnderObject)
            {
                // Stand up if crouch input is released and not under an object
                var.isCrouching = false;
            }
        }
        else
        {
            if (!var.isUnderObject)
            {
                // Stand up
                transform.localScale = Vector3.MoveTowards(
                    transform.localScale,
                    new Vector3(1, 1, 1),
                    Time.deltaTime / var.crouchSpeed
                );
            }
        }

        // Falling
        if (!var.isGrounded)
        {
            if (rb.linearVelocity.y < 0)
            {
                var.isFalling = true;
            }
            else
            {
                var.isFalling = false;
            }
        }
        else
        {
            var.isFalling = false;
            var.isJumping = false;
        }

        // Stand up if not allowed to crouch while not under an object
        if (!var.canCrouch && var.isCrouching && !var.isUnderObject)
        {
            var.isCrouching = false;
        }
    }

    private void FixedUpdate()
    {
        if (var.isGrounded && !var.isJumping)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }

        // Calculate movement vector based on input and player orientation, and apply it to the rigidbody's velocity
        Vector3 move = (transform.right * movement.x + transform.forward * movement.y) * moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (var.canMove)
        {
            // Set movement vector and moving state based on input
            var.isMoving = movement != Vector2.zero;
            movement = context.ReadValue<Vector2>();
        }
        else
        {
            // Stop movement if not allowed to move
            var.isMoving = false;
            movement = Vector2.zero;
        }
    }

    public void Look(InputAction.CallbackContext context)
    {
        if (var.canLook)
        {
            // Set look vector based on input
            look = context.ReadValue<Vector2>();
        }
        else
        {
            look = Vector2.zero;
        }
    }

    public void Crouch(InputAction.CallbackContext context)
    {
        // Set crouching state based on input if allowed to crouch
        if (var.canCrouch && var.isGrounded)
        {
            // Start crouching when the input is performed, and stop crouching when the input is canceled
            if (context.started)
            {
                var.isCrouching = true;
                crouchTriggered = true;
            }
            else if (context.canceled)
            {
                if (!var.isUnderObject)
                {
                    var.isCrouching = false;
                }

                crouchTriggered = false;
            }
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        // Apply jump force if allowed to jump and currently grounded
        if (context.started && var.canJump && var.isGrounded && !var.isCrouching)
        {
            var.isJumping = true;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, var.jumpForce, rb.linearVelocity.z);
        }
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        // Set sprinting state based on input if allowed to sprint
        if (var.canSprint)
        {
            // Start sprinting when the input is performed, and stop sprinting when the input is canceled
            if (context.started && var.canSprint)
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