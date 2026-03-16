using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    PlayerVariables var;
    Rigidbody rb;

    Vector2 movement;
    float moveSpeed;

    private void Awake()
    {
        var = GetComponent<PlayerVariables>();
        rb = var.rb;
        moveSpeed = var.moveSpeed;
    }

    private void Update()
    {
        // Sprinting
        if (!var.isCrouching)
        {
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
        // Movement
        Vector3 move = (transform.right * movement.x + transform.forward * movement.y) * moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (var.canMove)
        {
            var.isMoving = movement != Vector2.zero;
            movement = context.ReadValue<Vector2>();
        }
        else
        {
            var.isMoving = false;
            movement = Vector2.zero;
        }
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        if (var.canSprint)
        {
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
        if (var.canCrouch)
        {
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