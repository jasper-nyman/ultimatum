using UnityEngine;

// Simple behaviour for spawned noise-maker objects: when they hit a surface they stop and stay
// for a configured lifetime, then destroy themselves.
[RequireComponent(typeof(Rigidbody))]
public class NoiseMakerThrown : MonoBehaviour
{
    public float stayDuration = 12f;
    [Tooltip("Scale multiplier applied when the object is spawned to make it smaller")]
    public float scaleFactor = 0.35f;
    [Tooltip("Optional reference to the player GameObject so the thrown object will not stick to the player")]
    public GameObject playerToIgnore;
    [Tooltip("If playerToIgnore is not set, this tag will be used to detect the player (default 'Player')")]
    public string ignoreTag = "Player";

    // Thresholds to consider the object "still" after collision
    [Tooltip("Linear speed (m/s) below which the object is considered still")]
    public float stillSpeedThreshold = 0.05f;
    [Tooltip("Angular speed (rad/s) below which the object is considered still")]
    public float stillAngularThreshold = 0.05f;
    [Tooltip("Seconds the object must remain still before the stay timer begins")]
    public float stillTimeRequired = 0.5f;

    // Track whether we've made initial contact with the world so we can begin monitoring
    // for the object to come to rest naturally.
    private bool hasCollided = false;
    private float settledTimer = 0f;
    private float lifeTimer = 0f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Apply a default scale reduction so the noise-maker is smaller by default
        if (scaleFactor > 0f)
        {
            try { transform.localScale = Vector3.Scale(transform.localScale, Vector3.one * scaleFactor); } catch { }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore collisions with the player object so the thrower doesn't interfere with the object
        if (playerToIgnore != null)
        {
            if (collision.transform.IsChildOf(playerToIgnore.transform) || collision.transform == playerToIgnore.transform) return;
        }
        else if (!string.IsNullOrEmpty(ignoreTag) && collision.gameObject.CompareTag(ignoreTag))
        {
            return;
        }

        // Mark that we've collided with something (world) and should monitor for settling.
        hasCollided = true;
    }

    private void Update()
    {
        if (!hasCollided || rb == null) return;

        // Consider the object settled when both linear and angular velocities are below thresholds
        float lin = rb.linearVelocity.sqrMagnitude;
        float ang = rb.angularVelocity.sqrMagnitude;
        bool isStill = lin <= (stillSpeedThreshold * stillSpeedThreshold) && ang <= (stillAngularThreshold * stillAngularThreshold);

        if (isStill)
        {
            settledTimer += Time.deltaTime;
            if (settledTimer >= stillTimeRequired)
            {
                lifeTimer += Time.deltaTime;
                if (lifeTimer >= stayDuration)
                {
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            // Reset timers while the object is still moving
            settledTimer = 0f;
            lifeTimer = 0f;
        }
    }
}
