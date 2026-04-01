using System.Collections;
using System.Diagnostics;
using UnityEngine.Events;
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
    [Tooltip("Optional name of a child transform inside this GameObject to use as the visual. If specified the child with this name will be used.")]
    public string visualChildName;

    [Tooltip("Optional explicit Transform to use as the visual. If set, this transform will be used instead of creating/instantiating visuals.")]
    public Transform visualRootOverride;

    [Tooltip("Material to apply to the generated primitive (ignored if you provide visualPrefab).")]
    public Material overrideMaterial;

    [Tooltip("How quickly the plane extends (units per second).")]
    public float extendSpeed = 10f;

    [Tooltip("How quickly the plane retracts (units per second).")]
    public float retractSpeed = 12f;

    [Tooltip("Linear slide speed (units/sec) applied to a pushable object while the plane pushes it.")]
    public float pushSpeed = 5f;

    [Tooltip("Small distance used to check whether the pushed object is already contacting a non-pushable obstacle.")]
    public float pushContactCheckDistance = 0.05f;

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

    [Tooltip("Vertical offset in world units to spawn the plane higher on the Y axis relative to the origin.")]
    public float verticalSpawnOffset = 1.5f;

    // ---------------------------------------

    private enum State { Extending, Retracting, Finished }
    private State _state = State.Extending;

    // Pushing state
    private bool _isPushing = false;
    private Rigidbody _pushedRb = null;
    private Collider _pushedCollider = null;
    private float _baseRetractSpeed = 0f;
    // Saved original physics state for the pushed object so we can restore it when done
    private bool _pushedOriginalKinematic = false;
    private RigidbodyConstraints _pushedOriginalConstraints = RigidbodyConstraints.None;
    private bool _pushedOriginalDetectCollisions = true;

    // The visual child that will be scaled and placed — either created or instantiated from prefab.
    private Transform _visual;

    // Remember original visual local scale so we can preserve X/Y when using custom prefabs.
    private Vector3 _originalVisualScale = Vector3.one;

    // Whether we created a primitive fallback visual (cube). If true we apply planeWidth/planeHeight.
    private bool _primitiveVisual = false;

    // Current length of the plane along local +Z
    private float _currentLength = 0f;

    // Timer for extension duration
    private float _elapsed = 0f;

    // Cached forward direction in world space
    private Vector3 _worldForward = Vector3.forward;

    // Safety minimum length so we don't divide by zero or create degenerate scaling
    private const float MinLength = 0.001f;

    // Event invoked when the plane has fully retracted and is about to be destroyed.
    // Other systems (e.g., player controller) can listen to this to restore movement.
    public UnityEvent onFinished;

    private void Awake()
    {
        // Keep Awake lightweight. We intentionally defer visual creation and positioning
        // to Start so that spawners (which usually set the `origin` field immediately
        // after Instantiate) have a chance to assign the origin before initialization.
    }

    // Also guard against Edit-time creation via OnEnable/OnValidate so any accidental
    // editor instantiation is removed immediately and won't leave duplicates in the Hierarchy.
    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            // Log a stack trace so we can identify what code is instantiating the plane in Edit mode.
            // This helps track down editor-time duplication sources.
            var st = new StackTrace(true);
            UnityEngine.Debug.LogError($"ExtendablePlane created in Edit mode and will be destroyed. StackTrace:\n{st}", this);
            DestroyImmediate(gameObject);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && this != null)
        {
            // In some editor workflows OnValidate may be called before OnEnable; ensure
            // we also remove the instance here to be extra-safe.
            UnityEngine.Object.DestroyImmediate(this.gameObject);
        }
    }

    private void Start()
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
        // If the user assigned an explicit visualTransform, prefer that.
        if (visualRootOverride != null)
        {
            _visual = visualRootOverride;
            _visual.SetParent(transform, false);
            var colExisting = _visual.GetComponent<Collider>();
            if (colExisting != null) Destroy(colExisting);
        }
        else if (!string.IsNullOrEmpty(visualChildName))
        {
            var childFound = transform.Find(visualChildName);
            if (childFound != null)
            {
                _visual = childFound;
                _visual.SetParent(transform, false);
                var colExisting = _visual.GetComponent<Collider>();
                if (colExisting != null) Destroy(colExisting);
            }
        }
        else if (transform.childCount == 0)
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
                _primitiveVisual = true;

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
            // Try to find a suitable visual child (prefer one that has a Renderer).
            _visual = null;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.GetComponent<Renderer>() != null || child.GetComponent<SpriteRenderer>() != null)
                {
                    _visual = child;
                    break;
                }
            }

            // Fall back to first child if no renderer-present child found
            if (_visual == null) _visual = transform.GetChild(0);

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
            transform.position = origin.position + origin.TransformDirection(originLocalForward.normalized) * startOffset + Vector3.up * verticalSpawnOffset;
            transform.rotation = Quaternion.LookRotation(origin.TransformDirection(originLocalForward.normalized), Vector3.up);
            _worldForward = transform.forward;
        }
        else
        {
            // If origin is still null, use current transform forward.
            _worldForward = transform.forward;
            // Apply vertical offset even if no origin was provided so prefab preview matches runtime.
            transform.position += Vector3.up * verticalSpawnOffset;
        }

        // Remember original scale so we can respect X/Y for custom visuals
        if (_visual != null) _originalVisualScale = _visual.localScale;

        // Cache base retract speed so we can temporarily increase it when needed
        _baseRetractSpeed = retractSpeed;

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
            transform.position = origin.position + _worldForward * startOffset + Vector3.up * verticalSpawnOffset; // keep root at origin + offset and raised
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

    private void FixedUpdate()
    {
        // Slide the pushed object by directly moving its transform for stable, non-physical motion.
        if (_isPushing && _pushedRb != null && _state == State.Extending)
        {
            // If the pushed rigidbody has been destroyed, stop pushing.
            if (_pushedRb == null)
            {
                StopPushingAndRetract(true);
                return;
            }

            // Check slightly in front of the pushed object to see if it is contacting a non-pushable obstacle.
            Vector3 checkOrigin = _pushedCollider != null ? _pushedCollider.bounds.center : _pushedRb.position;
            if (Physics.Raycast(checkOrigin, _worldForward, out RaycastHit frontHit, pushContactCheckDistance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                // If the thing in front is not the pushed collider and is not pushable, stop pushing and retract.
                if (frontHit.collider != null && frontHit.collider != _pushedCollider)
                {
                    if (!TryGetPushableRigidbody(frontHit.collider, out var frontRb))
                    {
                        StopPushingAndRetract(true);
                        return;
                    }
                }
            }

            // Move the pushed object's transform directly to produce a smooth sliding motion
            // that isn't affected by standard physics jitter. This ignores collisions while
            // moving — we rely on the front obstacle check above to stop pushing.
            var move = _worldForward * (pushSpeed * Time.fixedDeltaTime);
            _pushedRb.transform.position += move;
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
            // If we hit something that is pushable, begin pushing its Rigidbody instead of immediately retracting.
            if (TryGetPushableRigidbody(hit.collider, out var hitRb))
            {
                // Start pushing this object. Set current length to contact point so visual looks correct.
                _currentLength = Mathf.Max(hit.distance - 0.01f, MinLength);
                BeginPushing(hitRb, hit.collider);
            }
            else
            {
                // Hit a non-pushable object — set length to hit and start retracting.
                _currentLength = Mathf.Max(hit.distance - 0.01f, MinLength);
                StartRetracting();
            }
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

    private void BeginPushing(Rigidbody pushedRb, Collider hitCollider)
    {
        if (pushedRb == null) return;
        _isPushing = true;
        _pushedCollider = hitCollider;
        _pushedRb = pushedRb;

        // Save original physics state
        _pushedOriginalKinematic = _pushedRb.isKinematic;
        _pushedOriginalConstraints = _pushedRb.constraints;
        _pushedOriginalDetectCollisions = _pushedRb.detectCollisions;

        // Switch to kinematic and disable collision detection so we can slide it deterministically.
        _pushedRb.isKinematic = true;
        _pushedRb.detectCollisions = false;

        // While pushing, allow the plane to still extend until it reaches maxLength.
        // Also slightly increase retract speed so retract happens faster when we stop.
        retractSpeed = _baseRetractSpeed * 2f;
    }

    private void StopPushingAndRetract(bool hitObstacle)
    {
        _isPushing = false;
        // Restore pushed object's physics state if still present
        if (_pushedRb != null)
        {
            _pushedRb.isKinematic = _pushedOriginalKinematic;
            _pushedRb.constraints = _pushedOriginalConstraints;
            _pushedRb.detectCollisions = _pushedOriginalDetectCollisions;
        }
        _pushedRb = null;
        _pushedCollider = null;
        // If we hit an obstacle, retract faster.
        retractSpeed = _baseRetractSpeed * (hitObstacle ? 3f : 1f);
        StartRetracting();
    }

    // Attempts to find a Rigidbody belonging to a pushable object. We avoid a compile-time
    // dependency on a specific Pushable type by using a runtime check for a component
    // named "Pushable" on the hit collider's GameObject or its parents.
    private bool TryGetPushableRigidbody(Collider col, out Rigidbody rb)
    {
        rb = null;
        if (col == null) return false;
        var go = col.gameObject;
        // Search up the parent chain for a component named "Pushable". If found, return its Rigidbody.
        Transform t = go.transform;
        while (t != null)
        {
            var comps = t.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                if (c.GetType().Name == "Pushable")
                {
                    rb = t.GetComponent<Rigidbody>();
                    return rb != null;
                }
            }
            t = t.parent;
        }

        return false;
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
            // Notify listeners that the plane has finished before destroying it
                onFinished?.Invoke();

                // If we were pushing an object, restore its physics state
                if (_isPushing && _pushedRb != null)
                {
                    _pushedRb.isKinematic = _pushedOriginalKinematic;
                    _pushedRb.constraints = _pushedOriginalConstraints;
                    _pushedRb.detectCollisions = _pushedOriginalDetectCollisions;
                }
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
        if (_primitiveVisual)
        {
            // For the cube primitive, apply the configured width/height.
            _visual.localScale = new Vector3(planeWidth, planeHeight, l);
        }
        else
        {
            // For custom visuals, preserve X and Y scale and only set Z to the current length.
            _visual.localScale = new Vector3(_originalVisualScale.x, _originalVisualScale.y, l);
        }

        // Move the visual so its back (z=0 in local space) aligns with the transform.position (origin + offset).
        // Since scaling stretches equally around the visual's pivot (center), we translate it forward by half its length.
        _visual.localPosition = new Vector3(0f, 0f, l * 0.5f);
    }
}
