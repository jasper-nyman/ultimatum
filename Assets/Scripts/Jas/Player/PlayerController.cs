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

    private void Awake()
    {
        // Get references to the PlayerVariables component and its Rigidbody, and initialize move speed
        var = GetComponent<PlayerVariables>();
        rb = var.rb;
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

        // Crouching
        if (var.isCrouching)
        {
            // Crouch down
            transform.localScale = Vector3.MoveTowards(
                transform.localScale,
                new Vector3(1, var.crouchHeight, 1),
                Time.deltaTime / var.crouchSpeed
            );
        }
        else
        {
            // Stand up
            transform.localScale = Vector3.MoveTowards(
                transform.localScale,
                new Vector3(1, 1, 1),
                Time.deltaTime / var.crouchSpeed
            );
        }

        // Stand up if not allowed to crouch while not under an object
        if (!var.canCrouch && var.isCrouching && !var.isUnderObject)
        {
            var.isCrouching = false;
        }
    }

    private void FixedUpdate()
    {
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

    public void Sprint(InputAction.CallbackContext context)
    {
        // Set sprinting state based on input if allowed to sprint
        if (var.canSprint)
        {
            // Start sprinting when the input is performed, and stop sprinting when the input is canceled
            if (context.performed)
            {
                var.isSprinting = true;
            }
            else if (context.canceled)
            {
                var.isSprinting = false;
            }
        }
    }

    public void Crouch(InputAction.CallbackContext context)
    {
        // Set crouching state based on input if allowed to crouch
        if (var.canCrouch)
        {
            // Start crouching when the input is performed, and stop crouching when the input is canceled
            if (context.started)
            {
                var.isCrouching = true;
            }
            else if (context.canceled)
            {
                var.isCrouching = false;
            }
        }
    }
}