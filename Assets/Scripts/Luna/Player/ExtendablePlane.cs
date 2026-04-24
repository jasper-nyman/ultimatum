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
    // Summary:
    // ExtendablePlane visually extends along its local +Z from the provided `origin`.
    // It performs a raycast to determine collisions and can "push" objects that
    // are tagged with a `Pushable` component. While pushing, the object's Rigidbody
    // is made kinematic and its transform is moved directly to produce a smooth
    // sliding motion without physics jitter. The plane retracts when it hits a
    // non-pushable object, when it times out, or when explicitly instructed.

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

    // Cached renderers belonging to the visual so we can hide them when the camera is too close
    private Renderer[] _visualRenderers;
    // A cloned visual used to show the far portion of the beam when the near portion
    // must be hidden to avoid occluding the camera. This keeps the beam visible.
    private Transform _farVisual;
    private Renderer[] _farVisualRenderers;

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

    // The world-space origin position and forward captured at spawn time so the plane
    // extends along a fixed direction and does not follow the player while extending.
    private Vector3 _spawnOriginPosition;
    private Vector3 _spawnRootPosition;
    private Vector3 _spawnWorldForward;
    private bool _lockedToSpawn = false;

    // Safety minimum length so we don't divide by zero or create degenerate scaling
    private const float MinLength = 0.001f;

    // Event invoked when the plane has fully retracted and is about to be destroyed.
    // Other systems (e.g., player controller) can listen to this to restore movement.
    public UnityEvent onFinished;
    // Guard to ensure we notify listeners exactly once even if destroyed unexpectedly
    private bool _finishedNotified = false;

    private void NotifyFinishedOnce()
    {
        if (_finishedNotified) return;
        _finishedNotified = true;
        try { onFinished?.Invoke(); } catch { }
    }

    private void OnDestroy()
    {
        // Ensure listeners are notified if the plane is destroyed by any other means
        NotifyFinishedOnce();
    }

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
            // Ensure the plane root is placed at or beyond startOffset relative to the origin.
            // If the player's camera is closer than startOffset, push the plane start forward
            // so it does not sit between the camera and the scene (which would occlude view).
            float effectiveStart = startOffset;
            try
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    float camLocalZ = origin.InverseTransformPoint(cam.transform.position).z;
                    // leave a small margin so plane starts slightly in front of camera
                    effectiveStart = Mathf.Max(startOffset, camLocalZ + 0.15f);
                }
            }
            catch { }
            transform.position = origin.position + origin.TransformDirection(originLocalForward.normalized) * effectiveStart + Vector3.up * verticalSpawnOffset;
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

        // Cache renderers for hide/show logic
        if (_visual != null)
        {
            _visualRenderers = _visual.GetComponentsInChildren<Renderer>(true);
            // Create a far visual clone so we can hide/thin the near portion without
            // removing the visible beam further out. Clone only once.
            try
            {
                var cloneGo = Instantiate(_visual.gameObject, transform);
                cloneGo.name = _visual.name + "_Far";
                _farVisual = cloneGo.transform;
                _farVisual.SetParent(transform, false);
                // Remove any collider on the clone as well
                var colc = _farVisual.GetComponent<Collider>(); if (colc != null) Destroy(colc);
                _farVisualRenderers = _farVisual.GetComponentsInChildren<Renderer>(true);
            }
            catch { /* non-fatal if clone fails for complex visuals */ }
        }

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
            // Compute an effective start so the plane doesn't start between the camera and the world
            float effectiveStart = startOffset;
            if (Camera.main != null)
            {
                float camLocalZ = origin.InverseTransformPoint(Camera.main.transform.position).z;
                effectiveStart = Mathf.Max(startOffset, camLocalZ + 0.15f);
            }
            transform.position = origin.position + _worldForward * effectiveStart + Vector3.up * verticalSpawnOffset; // keep root at origin + offset and raised
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

        // If we have a visual and a camera, hide the visual only when the camera is actually
        // inside the visual's world bounds. This is more accurate than clamping by startOffset
        // and avoids hiding the entire plane when the camera is slightly forward but still
        // should be able to see the beam shooting out.
        if (_visual != null && _visualRenderers != null && Camera.main != null && _visualRenderers.Length > 0)
        {
            var camPos = Camera.main.transform.position;

            // Hide only the renderer(s) whose bounds actually contain the camera position.
            // This prevents hiding the entire beam when a single part intersects the camera.
            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                var r = _visualRenderers[i];
                if (r == null) continue;
                bool contains = r.bounds.Contains(camPos);
                if (r.enabled == contains)
                    r.enabled = !contains;
            }
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

        // If we've reached maximum length without hitting anything, start retracting immediately
        // instead of waiting for the maxDuration timeout.
        if (_currentLength >= maxLength)
        {
            StartRetracting();
            UpdateVisual();
            return;
        }

        // Raycast along the beam and consider the closest relevant hit. Use RaycastAll to
        // be robust to complex colliders and ensure we find pushable objects even if the
        // first returned hit is a child collider or otherwise ordered differently.
        var originPos = transform.position;
        var rayDir = transform.forward;
        var hits = Physics.RaycastAll(originPos, rayDir, _currentLength, collisionMask, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            // Sort hits by distance so we handle the nearest first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            bool handled = false;
            foreach (var hh in hits)
            {
                if (hh.collider == null) continue;
                // If this hit corresponds to a pushable, begin pushing it.
                if (TryGetPushableRigidbody(hh.collider, out var hitRb))
                {
                    _currentLength = Mathf.Max(hh.distance - 0.01f, MinLength);
                    BeginPushing(hitRb, hh.collider);
                    handled = true;
                    break;
                }

                // If the hit is not pushable, treat it as an obstacle and retract.
                _currentLength = Mathf.Max(hh.distance - 0.01f, MinLength);
                StartRetracting();
                handled = true;
                break;
            }

            if (handled)
            {
                // we've processed the nearest hit
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
                NotifyFinishedOnce();

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
        // If the camera is very close to the origin, reduce the visual's thickness so it
        // doesn't occlude the first-person view. We keep the Z length intact so the beam
        // remains visible but is very thin from the player's perspective.
        float thicknessFactor = 1f;
        // If the camera is actually inside the visual bounds, thin the beam but keep it visible.
        if (Camera.main != null && _visualRenderers != null && _visualRenderers.Length > 0)
        {
            var camPos = Camera.main.transform.position;
            Bounds combined = _visualRenderers[0].bounds;
            for (int i = 1; i < _visualRenderers.Length; i++)
            {
                if (_visualRenderers[i] == null) continue;
                combined.Encapsulate(_visualRenderers[i].bounds);
            }
            if (combined.Contains(camPos))
            {
                // Use a larger thickness than before so the beam remains visible in first-person.
                thicknessFactor = 0.25f;
            }
        }

        // If we have a cloned far-visual, split the beam into a near segment (which may be thinned/hidden)
        // and a far segment so the player can still see the beam while the near geometry won't occlude.
        float nearLen = 0f;
        float farLen = l;
        if (Camera.main != null && origin != null && _farVisual != null)
        {
            // Camera local Z relative to origin
            float camLocalZ = origin.InverseTransformPoint(Camera.main.transform.position).z;
            // We want to hide the portion of the beam that is in front of the camera. Compute nearLen as
            // how much of the beam is between origin and the camera (minus a small margin).
            float margin = 0.05f;
            nearLen = Mathf.Clamp(camLocalZ - margin, 0f, l);
            farLen = Mathf.Max(0f, l - nearLen);
        }

        // Update near visual (original)
        if (_primitiveVisual)
        {
            _visual.localScale = new Vector3(planeWidth * thicknessFactor, planeHeight * thicknessFactor, Mathf.Max(nearLen, MinLength));
        }
        else
        {
            _visual.localScale = new Vector3(_originalVisualScale.x * thicknessFactor, _originalVisualScale.y * thicknessFactor, Mathf.Max(nearLen, MinLength));
        }
        _visual.localPosition = new Vector3(0f, 0f, Mathf.Max(nearLen, MinLength) * 0.5f);

        // Update far visual (clone) to show the remainder of the beam beyond the near segment
        if (_farVisual != null)
        {
            if (farLen <= MinLength)
            {
                _farVisual.gameObject.SetActive(false);
            }
            else
            {
                _farVisual.gameObject.SetActive(true);
                if (_primitiveVisual)
                {
                    _farVisual.localScale = new Vector3(planeWidth, planeHeight, farLen);
                }
                else
                {
                    _farVisual.localScale = new Vector3(_originalVisualScale.x, _originalVisualScale.y, farLen);
                }
                // Position far visual so its start aligns immediately after the near segment
                _farVisual.localPosition = new Vector3(0f, 0f, nearLen + (farLen * 0.5f));
            }
        }
    }
}
