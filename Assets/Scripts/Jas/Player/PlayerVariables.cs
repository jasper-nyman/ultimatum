using UnityEngine;

public class PlayerVariables : MonoBehaviour
{
    // References
    [Header("References")]
    public Rigidbody rb;
    public CameraTargetOffset cto;

    // Flags
    [Header("Flags")]
    public bool isActive;
    public bool isGrounded;
    public bool isFalling;
    public bool isMoving;
    public bool isSprinting;
    public bool isCrouching;
    public bool isUnderObject;
    public bool isJumping;

    // Permissions
    [Header("Permissions")]
    public bool canMove;
    public bool canSprint;
    public bool canCrouch;
    public bool canJump;

    // Parameters
    [Header("Parameters")]
    public float moveSpeed;
    public float sprintSpeed;
    public float crouchHeight;
    public float crouchSpeed;
    public float jumpForce;

    private void Update()
    {
        cto.positionOffset = new Vector3
        (
            x: 0,
            y: 1.8f * transform.localScale.y,
            z: 0
        );
    }
}