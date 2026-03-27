using UnityEngine;
using UnityEngine.InputSystem; // Use the new Input System's types (Mouse, Pointer, etc.)
using UnityEngine.Events;

// This component listens for right mouse clicks and spawns an extendable plane when the
// click is not over an in-world item. Configure the `planePrefab` in the inspector to
// use the `ExtendablePlane` prefab (or leave it null and configure runtime defaults).
[DisallowMultipleComponent]
public class PlaneShooter : MonoBehaviour
{
    [Tooltip("Prefab that has an ExtendablePlane component. If null, a default GameObject with ExtendablePlane will be created at runtime.")]
    public GameObject planePrefab;
    [Tooltip("Optional visual prefab to override the ExtendablePlane's visual. If set, this will be used as the visualPrefab on spawned planes.")]
    public GameObject overrideVisualPrefab;

    [Tooltip("Optional explicit visual child name to assign to spawned planes. Useful when your plane prefab has a child named e.g. 'Visual'.")]
    public string visualChildNameOverride;

    [Tooltip("Optional explicit visual root transform to assign to spawned planes at runtime.")]
    public Transform visualRootOverride;

    [Tooltip("Transform used as the origin (usually the player). If null, the GameObject tagged 'Player' will be used.")]
    public Transform originOverride;

    [Tooltip("Layers considered to be 'items' - clicks on these will NOT spawn the plane if hit.")]
    public LayerMask itemLayers = 1 << 0; // default to Default layer

    [Tooltip("Layers that raycasts should detect for determining if click hit something meaningful (optional).")]
    public LayerMask raycastLayers = ~0;

    // Optional defaults to apply to a newly-created ExtendablePlane if the plane prefab doesn't set them.
    [Header("Optional defaults applied to spawned ExtendablePlane (only used if planePrefab doesn't already set them)")]
    public float defaultExtendSpeed = 10f;
    public float defaultRetractSpeed = 5f;
    public float defaultMaxDuration = 10f;
    public float defaultMaxLength = 50f;

    private Transform _origin;

    // Track how many active planes are present so we only restore movement when all are gone
    private static int s_activePlanes = 0;

    // Cached reference to player variables on origin so we can toggle movement/looking
    private PlayerVariables _originVars;

    private void Awake()
    {
        // Resolve origin: use override or find object tagged "Player" in scene
        if (originOverride != null) _origin = originOverride;
        else
        {
            var player = GameObject.FindWithTag("Player");
            if (player) _origin = player.transform;
        }

        // If still null, fall back to this component's transform
        if (_origin == null) _origin = transform;

        // Cache PlayerVariables on the origin if available
        _originVars = _origin.GetComponent<PlayerVariables>();
    }

    private void Update()
    {
        // Using the new Input System to detect right mouse button presses via Mouse.current.
        var mouse = Mouse.current;
        if (mouse == null) return; // no mouse available

        if (mouse.rightButton.wasPressedThisFrame)
        {
            var cam = Camera.main;
            if (cam == null) return;

            // Raycast from the camera through the mouse position
            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePos);

            // Do physics raycast using the configured raycast layers
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, raycastLayers, QueryTriggerInteraction.Ignore))
            {
                // If the hit object is on the itemLayers then do nothing
                if (((1 << hit.transform.gameObject.layer) & itemLayers) != 0)
                {
                    // Click was on an item layer — skip spawning
                    return;
                }

                // Also check if the hit object (or its parents) contains an ItemInstance component
                // We avoid a compile-time dependency by searching components and matching by type name.
                var comps = hit.transform.GetComponentsInParent<Component>();
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    if (c.GetType().Name == "ItemInstance")
                    {
                        // Clicked an in-world item — skip
                        return;
                    }
                }
            }

            // Otherwise spawn the extendable plane
            SpawnPlane();
        }
    }

    private void SpawnPlane()
    {
        GameObject go;

        if (_origin == null)
        {
            // If origin isn't resolved for some reason, fallback to this transform
            _origin = transform;
        }

        if (planePrefab != null)
        {
            // Instantiate the prefab at the origin position with rotation matching the origin
            // so the visual starts already aligned and doesn't have to snap in Start.
            var rot = Quaternion.LookRotation(_origin.TransformDirection(Vector3.forward), Vector3.up);
            go = Instantiate(planePrefab, _origin.position + _origin.TransformDirection(Vector3.forward) * 0.5f, rot);
        }
        else
        {
            // Create an empty GameObject with ExtendablePlane component and a basic name
            go = new GameObject("ExtendablePlane_Instance");
            go.AddComponent<ExtendablePlane>();
            // Position it at the origin so Start sets it correctly
            go.transform.position = _origin.position + _origin.TransformDirection(Vector3.forward) * 0.5f;
        }

        // Parent it to the scene root (optional)
        go.transform.SetParent(null);

        var ep = go.GetComponent<ExtendablePlane>();
        if (ep == null)
        {
            Debug.LogError("Spawned plane prefab does not contain an ExtendablePlane component.");
            Destroy(go);
            return;
        }

        // Assign origin so the plane knows where to come from
        ep.origin = _origin;

        // Increase active plane count and disable movement/looking on the player while the plane exists
        s_activePlanes++;
        if (_originVars != null)
        {
            _originVars.canMove = false;
            _originVars.canLook = false;
        }

        // Subscribe to finish event so we can restore movement when this plane is done
        ep.onFinished = ep.onFinished ?? new UnityEvent();
        ep.onFinished.AddListener(() => {
            s_activePlanes = Mathf.Max(0, s_activePlanes - 1);
            if (s_activePlanes == 0 && _originVars != null)
            {
                _originVars.canMove = true;
                _originVars.canLook = true;
            }
        });

        // If shooter has override visuals specified in the inspector, pass them through
        if (overrideVisualPrefab != null) ep.visualPrefab = overrideVisualPrefab;
        if (!string.IsNullOrEmpty(visualChildNameOverride)) ep.visualChildName = visualChildNameOverride;
        if (visualRootOverride != null) ep.visualRootOverride = visualRootOverride;

        // Apply optional default values if the prefab left them as zero/empty defaults
        if (Mathf.Approximately(ep.extendSpeed, 0f)) ep.extendSpeed = defaultExtendSpeed;
        if (Mathf.Approximately(ep.retractSpeed, 0f)) ep.retractSpeed = defaultRetractSpeed;
        if (Mathf.Approximately(ep.maxDuration, 0f)) ep.maxDuration = defaultMaxDuration;
        if (Mathf.Approximately(ep.maxLength, 0f)) ep.maxLength = defaultMaxLength;

        // Position and rotation will be handled by the ExtendablePlane's Awake/Update which uses ep.origin
    }
}
