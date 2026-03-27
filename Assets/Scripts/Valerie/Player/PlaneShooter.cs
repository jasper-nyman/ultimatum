using UnityEngine;
using UnityEngine.InputSystem; // Use the new Input System's types (Mouse, Pointer, etc.)

// This component listens for right mouse clicks and spawns an extendable plane when the
// click is not over an in-world item. Configure the `planePrefab` in the inspector to
// use the `ExtendablePlane` prefab (or leave it null and configure runtime defaults).
[DisallowMultipleComponent]
public class PlaneShooter : MonoBehaviour
{
    [Tooltip("Prefab that has an ExtendablePlane component. If null, a default GameObject with ExtendablePlane will be created at runtime.")]
    public GameObject planePrefab;

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

        if (planePrefab != null)
        {
            // Instantiate the prefab and ensure it has an ExtendablePlane component
            go = Instantiate(planePrefab);
        }
        else
        {
            // Create an empty GameObject with ExtendablePlane component and a basic name
            go = new GameObject("ExtendablePlane_Instance");
            go.AddComponent<ExtendablePlane>();
        }

        // Parent it to the scene root (optional) and position at the origin
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

        // Apply optional default values if the prefab left them as zero/empty defaults
        if (Mathf.Approximately(ep.extendSpeed, 0f)) ep.extendSpeed = defaultExtendSpeed;
        if (Mathf.Approximately(ep.retractSpeed, 0f)) ep.retractSpeed = defaultRetractSpeed;
        if (Mathf.Approximately(ep.maxDuration, 0f)) ep.maxDuration = defaultMaxDuration;
        if (Mathf.Approximately(ep.maxLength, 0f)) ep.maxLength = defaultMaxLength;

        // Position and rotation will be handled by the ExtendablePlane's Awake/Update which uses ep.origin
    }
}
