using UnityEngine;

public class PlayerVariables : MonoBehaviour
{
    // Flags
    [Header("Flags")]
    public bool isActive; // whether player control is active (not in UI)
    public bool isGrounded;
    public bool isFalling;
    public bool isMoving;
    public bool isSprinting;
    public bool isCrouching;
    public bool isUnderObject;
    public bool isJumping;

    // Permissions: other systems can toggle these to enable/disable input handling
    [Header("Permissions")]
    public bool canMove;
    public bool canLook;
    public bool canSprint;
    public bool canCrouch;
    public bool canJump;

    // Parameters: configurable movement/look/jump values
    [Header("Parameters")]
    public float moveSpeed;
    public float sprintSpeed;
    public float stamina;
    public float staminaDrainRate;
    public float staminaRegenRate;
    public float lookSensitivity;
    public float crouchHeight;
    public float crouchSpeed;
    public float jumpForce;
}