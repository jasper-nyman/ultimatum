using UnityEngine;

public class PlayerVariables : MonoBehaviour
{
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
    public bool canLook;
    public bool canSprint;
    public bool canCrouch;
    public bool canJump;

    // Parameters
    [Header("Parameters")]
    public float moveSpeed;
    public float sprintSpeed;
    public float lookSensitivity;
    public float crouchHeight;
    public float crouchSpeed;
    public float jumpForce;
}