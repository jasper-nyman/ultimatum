using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerVariables))]

public class PlayerController : MonoBehaviour
{
    // References
    private PlayerVariables var;
    private Rigidbody rb;
    private PlayerManager manager;

    // Parameters
    [HideInInspector] public Vector2 move;
    [HideInInspector] public float moveSpeed;
    private Vector2 look;

    private void Start()
    {
        // Get references
        rb = GetComponent<Rigidbody>();
        var = GetComponent<PlayerVariables>();
        manager = GetComponent<PlayerManager>();
        
        // Initialize move speed
        moveSpeed = var.moveSpeed;
    }

    private void Update()
    {
        // Look
        if (var.isActive)
        {
            transform.Rotate(Vector3.up * look.x * var.lookSensitivity * Time.deltaTime);
            CameraController cc = Camera.main.GetComponent<CameraController>();

            if (cc != null)
            {
                // Rotate the camera and clamp its rotation
                cc.rotation.x -= look.y * var.lookSensitivity * Time.deltaTime;
                cc.rotation.x = Mathf.Clamp(cc.rotation.x, -90f,90f);
                cc.rotation.y = transform.eulerAngles.y;
            }
        }

        // Falling state is handled by PlayerManager
    }

    private void FixedUpdate()
    {
        // Get movement vector and apply it to the player object's rigidbody
        Vector3 movement = (transform.right * move.x + transform.forward * move.y) * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (var.canMove)
        {
            // Get movement vector
            move = context.ReadValue<Vector2>();
        }
        else
        {
            // Stop movement if not allowed to move
            move = Vector2.zero;
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
        // Forward crouch queueing to PlayerManager which owns crouch state application
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

    public void Jump(InputAction.CallbackContext context)
    {
        // Apply jump force if allowed to jump and currently grounded
        if (context.started && var.canJump && var.isGrounded && !var.isCrouching)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, var.jumpForce, rb.linearVelocity.z);
            var.isJumping = true;
        }
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        // Set sprinting state based on input if allowed to sprint
        if (var.canSprint)
        {
            // Start sprinting when the input is performed, and stop sprinting when the input is canceled
            if (context.started)
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