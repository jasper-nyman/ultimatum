using System.Collections;
using UnityEngine;

// This component controls a single extendable plane that grows outward from an origin
// and then retracts back after hitting something or after a timeout.
// The visual can be a simple primitive cube (created automatically) or a custom prefab
// assigned via `visualPrefab`. The prefab should be oriented so its forward is +Z
// and 1 unit long along Z (so scaling on Z changes the length correctly).
[DisallowMultipleComponent]
public class ExtendablePlane : MonoBehaviour
{
    // -------- Editable in inspector --------

    [Tooltip("Optional visual prefab to instantiate as the plane. If null, a Cube primitive will be used.")]
    public GameObject visualPrefab;

    [Tooltip("Material to apply to the generated primitive (ignored if you provide visualPrefab).")]
    public Material overrideMaterial;

    [Tooltip("How quickly the plane extends (units per second).")]
    public float extendSpeed = 10f;

    [Tooltip("How quickly the plane retracts (units per second).")]
    public float retractSpeed = 5f;

    [Tooltip("How long (seconds) the plane will keep extending before forcing a retract if nothing is hit.")]
    public float maxDuration = 10f;

    [Tooltip("Maximum length (world units) the plane may reach while extending.")]
    public float maxLength = 50f;

    [Tooltip("Half-width of the plane (X axis) when using a cube primitive.")]
    public float planeWidth = 0.2f;

    [Tooltip("Height (Y axis) of the plane when using a cube primitive.")]
    public float planeHeight = 0.2f;

    [Tooltip("Layers that the plane will collide with (used for raycast).")]
    public LayerMask collisionMask = ~0; // default everything

    [Tooltip("Transform that represents the origin (player) where the plane starts from.")]
    public Transform origin;

    [Tooltip("Local forward direction to use relative to the origin. Usually Vector3.forward.")]
    public Vector3 originLocalForward = Vector3.forward;

    [Tooltip("Optional offset from the origin along the forward direction so the plane doesn't start inside the player.")]
    public float startOffset = 0.5f;

    // ---------------------------------------

    private enum State { Extending, Retracting, Finished }
    private State _state = State.Extending;

    // The visual child that will be scaled and placed — either created or instantiated from prefab.
    private Transform _visual;

    // Current length of the plane along local +Z
    private float _currentLength = 0f;

    // Timer for extension duration
    private float _elapsed = 0f;

    // Cached forward direction in world space
    private Vector3 _worldForward = Vector3.forward;

    // Safety minimum length so we don't divide by zero or create degenerate scaling
    private const float MinLength = 0.001f;

    private void Awake()
    {
        // Ensure there's an origin set. If not, try to find a GameObject tagged "Player".
        if (origin == null)
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null) origin = playerGo.transform;
        }

        // Instantiate or create the visual representation of the plane.
        // Only instantiate the `visualPrefab` when there are no child visuals already present
        // on this GameObject. This avoids duplicating visuals when the plane prefab
        // already contains its visual as a child.
        if (transform.childCount == 0)
        {
            if (visualPrefab != null)
            {
                var go = Instantiate(visualPrefab, transform);
                _visual = go.transform;
            }
            else
            {
                // Create a simple cube primitive as a default visual.
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(transform, false);
                _visual = cube.transform;

                // Optionally apply the override material if provided
                if (overrideMaterial != null)
                {
                    var rend = cube.GetComponent<Renderer>();
                    rend.material = overrideMaterial;
                }

                // Remove collider from the visual cube — the plane's collisions are handled via raycasts only.
                var col = cube.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
        }
        else
        {
            // Use the first child as the visual. This allows packaging a visual inside the
            // prefab itself and prevents the script from creating a default cube on top.
            _visual = transform.GetChild(0);

            // Ensure the visual is parented correctly in case the prefab had different setup.
            _visual.SetParent(transform, false);

            // Remove any collider on the visual to avoid interfering with raycasts handled by this script.
            var colExisting = _visual.GetComponent<Collider>();
            if (colExisting != null) Destroy(colExisting);
        }

        // Initial setup of transform. We will position the whole object at the origin and rotate it
        // so +Z points outward. Positioning immediately prevents visual jitter when the prefab
        // is instantiated elsewhere in the scene.
        if (origin != null)
        {
            transform.position = origin.position + origin.TransformDirection(originLocalForward.normalized) * startOffset;
            transform.rotation = Quaternion.LookRotation(origin.TransformDirection(originLocalForward.normalized), Vector3.up);
            _worldForward = transform.forward;
        }
        else
        {
            // If origin is still null, use current transform forward.
            _worldForward = transform.forward;
        }

        // Start with a minimal length so it appears to grow from the origin.
        _currentLength = MinLength;
        UpdateVisual();
    }

    private void Update()
    {
        // Update world forward every frame in case the origin has rotated (so the beam stays oriented to the player)
        if (origin != null)
        {
            // Recompute world forward and then immediately set the transform's rotation and position
            // so the visual follows the player's orientation without delay.
            _worldForward = origin.TransformDirection(originLocalForward.normalized);
            transform.rotation = Quaternion.LookRotation(_worldForward, Vector3.up);
            transform.position = origin.position + _worldForward * startOffset; // keep root at origin + offset
        }

        switch (_state)
        {
            case State.Extending:
                HandleExtending();
                break;
            case State.Retracting:
                HandleRetracting();
                break;
            case State.Finished:
                // nothing left to do
                break;
        }
    }

    // While extending: increase length, test for collisions and max duration
    private void HandleExtending()
    {
        float dt = Time.deltaTime;
        _elapsed += dt;

        // Increase length
        _currentLength += extendSpeed * dt;
        if (_currentLength > maxLength) _currentLength = maxLength;

        // Raycast from origin (not from visual center) to check if we've hit something within the current length.
        var originPos = origin != null ? origin.position : transform.position;
        if (Physics.Raycast(originPos, _worldForward, out RaycastHit hit, _currentLength, collisionMask, QueryTriggerInteraction.Ignore))
        {
            // We hit a wall — set the current length to the hit distance and start retracting.
            _currentLength = Mathf.Max(hit.distance - 0.01f, MinLength); // small offset so the plane doesn't overlap the wall
            StartRetracting();
        }
        else if (_elapsed >= maxDuration)
        {
            // Timed out — start retracting
            StartRetracting();
        }

        // Finally update the visual transform based on the new length
        UpdateVisual();
    }

    private void StartRetracting()
    {
        _state = State.Retracting;
    }

    private void HandleRetracting()
    {
        float dt = Time.deltaTime;
        _currentLength -= retractSpeed * dt;
        if (_currentLength <= MinLength)
        {
            // We're done — destroy this GameObject
            _currentLength = 0f;
            UpdateVisual();
            _state = State.Finished;
            Destroy(gameObject);
            return;
        }

        UpdateVisual();
    }

    // Place and scale the visual child so the plane appears to start at the origin and extend along +Z.
    private void UpdateVisual()
    {
        if (_visual == null) return;

        // Some visuals may be built with a default size of 1 unit in all axes. We want the visual to have
        // Length = _currentLength along Z, Width = planeWidth (X), Height = planeHeight (Y).
        // For a cube primitive that is 1x1x1, scaling directly suffices. For custom prefabs, we assume
        // they are built with unit length along Z as well.

        float l = Mathf.Max(_currentLength, MinLength);
        _visual.localScale = new Vector3(planeWidth, planeHeight, l);

        // Move the visual so its back (z=0 in local space) aligns with the transform.position (origin + offset).
        // Since scaling stretches equally around the visual's pivot (center), we translate it forward by half its length.
        _visual.localPosition = new Vector3(0f, 0f, l * 0.5f);
    }
}
