using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    // References
    private Rigidbody rb;
    private PlayerVariables var;
    private PlayerController ctrl;
    private CameraTracker camTracker;

    void Start()
    {
        // Get references
        rb = GetComponent<Rigidbody>();
        var = GetComponent<PlayerVariables>();
        ctrl = GetComponent<PlayerController>();
        camTracker = GetComponent<CameraTracker>();

        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Scale the camera tracker's height with the player object's
        camTracker.positionOffset.y = 1.8f * transform.localScale.y;

        // Check if the player is grounded and/or under an object(s)
        var.isGrounded = Physics.OverlapBox(
            transform.position, 
            new Vector3(0.5f, 0.1f, 0.5f), 
            transform.rotation, 
            LayerMask.GetMask("Surface")
        ).Length > 0;

        float headHeight = 2 * transform.localScale.y;
        Vector3 offset = new Vector3(0, headHeight, 0);

        var.isUnderObject = Physics.OverlapBox(
            transform.position + offset, 
            new Vector3(0.5f, 0.1f, 0.5f),
            transform.rotation, 
            LayerMask.GetMask("Surface")
        ).Length > 0;

        // Update move speed based on sprinting and crouching states
        if (!var.isCrouching)
        {
            if (var.isSprinting)
            {
                // Sprinting
                ctrl.moveSpeed = var.sprintSpeed;
            }
            else
            {
                // Walking
                ctrl.moveSpeed = var.moveSpeed;
            }
        }
        else
        {
            // Crouching
            ctrl.moveSpeed = var.moveSpeed / 2;
        }
    }
}
