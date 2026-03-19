using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerVariables))]

public class PlayerController : MonoBehaviour
{
    // References
    private PlayerVariables var;
    private Rigidbody rb;

    // Parameters
    private Vector2 move;
    [HideInInspector] public float moveSpeed;
    private Vector2 look;
    // UNHIDDEN FOR VISIBILITY IN INSPECTOR DURING DEBUGGING. CHECK WHY CROUCHING DOESNT HAPPEN WHEN HITTING THE GROUND AFTER QUEUING CROUCHING IN THE AIR
    public bool crouchQueued;

    private bool GetIsCrouching()
    {
        if (!var.canCrouch || !var.isGrounded)
            return false;

        if (var.isUnderObject)
            return true;

        return crouchQueued;
    }

    private void Start()
    {
        // Get references
        rb = GetComponent<Rigidbody>();
        var = GetComponent<PlayerVariables>();
        
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
                cc.rotation.x = Mathf.Clamp(cc.rotation.x, -90f, 90f);
                cc.rotation.y = transform.eulerAngles.y;
            }
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

        var.isCrouching = GetIsCrouching();

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
        }
    }

    private void FixedUpdate()
    {
        // Get movement vector and apply it to the player object's rigidbody
        Vector3 movement = (transform.right * move.x + transform.forward * move.y) * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            Vector3.zero, 
            new Vector3(1f, 0.1f, 1f)
        );

        float headHeight = 2 * transform.localScale.y;
        Vector3 offset = new Vector3(0, headHeight, 0);

        Gizmos.DrawWireCube(
            offset, 
            new Vector3(1f, 0.1f, 1f)
        );
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (var.canMove)
        {
            // Set movement vector and moving state based on input
            var.isMoving = move != Vector2.zero;
            move = context.ReadValue<Vector2>();
        }
        else
        {
            // Stop movement if not allowed to move
            var.isMoving = false;
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
        if (var.canCrouch && var.isGrounded)
        {
            if (context.started)
            {
                crouchQueued = true;
            }
            else if (context.canceled)
            {
                crouchQueued = false;
            }
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