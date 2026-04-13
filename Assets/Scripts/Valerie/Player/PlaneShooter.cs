using UnityEngine;
using UnityEngine.InputSystem; // Use the new Input System's types (Mouse, Pointer, etc.)
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using System.Reflection;

// This component listens for right mouse clicks and spawns an extendable plane when the
// click is not over an in-world item. Configure the `planePrefab` in the inspector to
// use the `ExtendablePlane` prefab (or leave it null and configure runtime defaults).
[DisallowMultipleComponent]
public class PlaneShooter : MonoBehaviour
{
    // Summary:
    // The PlaneShooter is responsible for detecting player input (right-click) and
    // creating an `ExtendablePlane` instance that extends from the player's origin.
    // It contains several editor-facing fields that control runtime behavior of the
    // spawned plane (extend/retract speeds, spawn height, etc.). The shooter will
    // not spawn planes while the editor is not in Play mode to avoid accidental
    // duplication when editing inspector values.

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
    // Make retract much faster by default; can be tuned in the inspector on this shooter object.
    public float defaultRetractSpeed = 80f;
    public float defaultMaxDuration = 10f;
    public float defaultMaxLength = 50f;

    [Tooltip("Vertical offset (world units) applied when spawning the plane. Change this on the shooter GameObject to affect spawned planes.")]
    public float spawnVerticalOffset = 1.5f;

    [Header("Camera override when shooting")]
    [Tooltip("Local forward offset (meters) for the camera while the plane is active. Small positive moves camera slightly forward relative to player head so the shot stays first-person.")]
    public float cameraForwardOffset = 0.2f;
    [Tooltip("Vertical offset for the temporary camera position.")]
    public float cameraHeight = 1.5f;

    [Tooltip("Optional settings asset. If set, these values will be applied to spawned planes. Use this to edit shared plane settings without modifying the prefab directly.")]
    public UnityEngine.Object settingsAsset;

    [Tooltip("Seconds after using an inventory item during which plane spawning is suppressed to avoid accidental shots.")]
    public float suppressAfterItemUseSeconds = 0.25f;

    private Transform _origin;

    // Track how many active planes are present so we only restore movement when all are gone
    private static int s_activePlanes = 0;

    // Cached reference to player variables on origin so we can toggle movement/looking
    private PlayerVariables _originVars;

    // Save camera state while plane is active so we can restore it when done
    private Transform _savedCamParent;
    private Vector3 _savedCamLocalPos;
    private Quaternion _savedCamLocalRot;
    private GameObject _savedCamTarget;
    private bool _savedCamControllerEnabled = false;
    private float _savedCamNearClip = -1f;
    private bool _firstPlaneSetupDone = false;

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

#if UNITY_EDITOR
    // Ensure that if a scene instance was accidentally assigned as the `planePrefab` in
    // the inspector, we replace it with its corresponding prefab asset (or clear it).
    // This prevents the Editor from treating a scene object as the prefab and later
    // duplicating or instantiating it when values change.
    private void OnValidate()
    {
        if (planePrefab == null) return;
        try
        {
            var src = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(planePrefab) as GameObject;
            if (src != null)
            {
                // Replace the scene instance reference with the prefab asset so
                // we always instantiate the asset and never the scene object.
                planePrefab = src;
            }
            else
            {
                // If the assigned object is a scene object with no prefab asset, clear it
                // because using a scene object here leads to duplication when editing.
                if (planePrefab.scene.IsValid())
                {
                    Debug.LogWarning("PlaneShooter.planePrefab referenced a scene object; clearing reference to avoid accidental duplication.", this);
                    planePrefab = null;
                }
            }
        }
        catch { /* ignore editor lookup errors */ }
    }
#endif

    private void Update()
    {
        // Prevent any spawning or input handling while editing in the Unity Editor.
        // This avoids accidentally creating plane GameObjects when changing inspector
        // values outside of Play mode.
        if (!Application.isPlaying) return;
        // Using the new Input System to detect right mouse button presses via Mouse.current.
        var mouse = Mouse.current;
        if (mouse == null) return; // no mouse available

        if (mouse.rightButton.wasPressedThisFrame)
        {
            Debug.Log("PlaneShooter: right mouse pressed — attempting spawn check");
            var inv = FindFirstObjectByType<Inventory>();
            if (inv != null && inv.IsSelectingItem())
            {
                // Player currently selecting an item -> do not spawn plane
                return;
            }
            // We intentionally DO NOT block on pointer-over-UI here: the player should
            // be able to fire the plane regardless of where the camera or pointer is aimed.
            // We still keep the brief suppression after using an item to avoid accidental
            // double-activations when using an item and immediately right-clicking.
            if (Inventory.lastItemUseTime + suppressAfterItemUseSeconds > Time.time)
            {
                return;
            }

            // We intentionally DO NOT perform a world raycast here. The plane should
            // spawn regardless of camera orientation or what the cursor is over so
            // long as the player is allowed to shoot (inventory not selecting an item
            // and pointer not over UI). This lets the player fire the plane even if
            // they're looking away from the firing direction.

            SpawnPlane();
        }
    }

    // Uses the EventSystem to raycast UI elements at the current mouse position and
    // returns true if any of the hit UI objects contains an InventoryItemInstance.
    private bool IsPointerOverInventoryItem()
    {
        if (EventSystem.current == null) return false;
        var mouse = Mouse.current;
        if (mouse == null) return false;

        Vector2 pos = mouse.position.ReadValue();
        PointerEventData ped = new PointerEventData(EventSystem.current) { position = pos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        foreach (var r in results)
        {
            if (r.gameObject == null) continue;
            // If any UI Graphic or CanvasRenderer is hit, treat it as UI and block.
            if (r.gameObject.GetComponentInParent<UnityEngine.UI.Graphic>() != null) return true;
            if (r.gameObject.GetComponentInParent<CanvasRenderer>() != null) return true;
            if (r.gameObject.GetComponentInParent<InventoryItemInstance>() != null) return true;
        }

        return false;
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
            // Instantiate the prefab asset when possible. In the Editor a user may have
            // dragged a scene instance into this field; prefer the corresponding asset
            // to avoid instantiating or modifying the scene object when spawning.
#if UNITY_EDITOR
            GameObject prefabToInstantiate = planePrefab;
            try
            {
                var source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(planePrefab) as GameObject;
                if (source != null) prefabToInstantiate = source;
            }
            catch { /* ignore editor lookup failures */ }
#else
            GameObject prefabToInstantiate = planePrefab;
#endif
            var rot = Quaternion.LookRotation(_origin.TransformDirection(Vector3.forward), Vector3.up);
            Vector3 offset = Vector3.up * spawnVerticalOffset;
            go = Instantiate(prefabToInstantiate, _origin.position + _origin.TransformDirection(Vector3.forward) * 0.5f + offset, rot);
        }
        else
        {
            // Create an empty GameObject with ExtendablePlane component and a basic name
            go = new GameObject("ExtendablePlane_Instance");
            go.AddComponent<ExtendablePlane>();
            // Position it at the origin so Start sets it correctly
            Vector3 offset = Vector3.up * spawnVerticalOffset;
            go.transform.position = _origin.position + _origin.TransformDirection(Vector3.forward) * 0.5f + offset;
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

        // Increase active plane count and disable movement/looking/jump on all PlayerVariables so
        // the player cannot move/look/jump while the plane is active.
        s_activePlanes++;
        var pvs = FindObjectsOfType<PlayerVariables>();
        foreach (var pv in pvs)
        {
            pv.canMove = false;
            pv.canLook = false;
            pv.canJump = false;
        }

        // Lock inventory inputs across all Inventory instances
        var invs = FindObjectsOfType<Inventory>();
        foreach (var ii in invs)
        {
            ii.inputLocked = true;
        }

        // If this is the first plane spawned, perform one-time camera repositioning so
        // the shot is visible. We avoid repeating this for multiple concurrent planes.
        if (!_firstPlaneSetupDone)
        {
            var cam = Camera.main ?? FindObjectOfType<Camera>();
            if (cam != null)
            {
                _savedCamParent = cam.transform.parent;
                _savedCamLocalPos = cam.transform.localPosition;
                _savedCamLocalRot = cam.transform.localRotation;
                _savedCamTarget = null;
                var cc = cam.GetComponent<CameraController>();
                if (cc != null)
                {
                    _savedCamControllerEnabled = cc.enabled;
                    cc.enabled = false;
                    _savedCamTarget = cc.target;
                }

                // Save and temporarily reduce near clip plane to avoid z-fighting when camera is very close
                _savedCamNearClip = cam.nearClipPlane;
                try { cam.nearClipPlane = 0.01f; } catch { }

                // Keep camera in first-person while making the shot visible: parent it to the origin
                // and apply a small local forward and vertical offset. This avoids switching to a
                // third-person view by unparenting the camera from the player.
                cam.transform.SetParent(_origin, true);
                // Ensure the camera isn't placed inside the plane's visual (causes z-fighting / flicker).
                // Prefer the configured forward offset but clamp it to be behind the plane's startOffset
                // so the camera stays in front of the player but not intersecting the spawned plane.
                float safeLocalZ = cameraForwardOffset;
                if (ep != null)
                {
                    // leave a small margin so the camera is definitely behind the visual start
                    float margin = 0.05f;
                    safeLocalZ = Mathf.Min(cameraForwardOffset, ep.startOffset - margin);
                }
                // Prevent extreme negative values; default to configured offset if clamping produced an invalid value
                if (float.IsNaN(safeLocalZ) || safeLocalZ < -1f) safeLocalZ = cameraForwardOffset;
                cam.transform.localPosition = new Vector3(0f, cameraHeight, safeLocalZ);
                cam.transform.localRotation = Quaternion.identity;
            }
            _firstPlaneSetupDone = true;
        }

        // Subscribe to finish event so we can restore movement when this plane is done
        ep.onFinished = ep.onFinished ?? new UnityEvent();
        ep.onFinished.AddListener(() => {
            s_activePlanes = Mathf.Max(0, s_activePlanes - 1);
            if (s_activePlanes == 0)
            {
                // Restore all PlayerVariables
                var pvs2 = FindObjectsOfType<PlayerVariables>();
                foreach (var pv2 in pvs2)
                {
                    pv2.canMove = true;
                    pv2.canLook = true;
                    pv2.canJump = true;
                }

                // Unlock inventory input across all inventories
                var invs2 = FindObjectsOfType<Inventory>();
                foreach (var ii2 in invs2)
                {
                    ii2.inputLocked = false;
                }

                // Restore camera parent/transform
                var cam2 = Camera.main;
                if (cam2 != null)
                {
                    var cc2 = cam2.GetComponent<CameraController>();
                    if (cc2 != null)
                    {
                        cc2.target = _savedCamTarget;
                        cc2.enabled = _savedCamControllerEnabled;
                    }
                    // Restore saved near clip plane
                    try { if (_savedCamNearClip > 0f) cam2.nearClipPlane = _savedCamNearClip; } catch { }
                    cam2.transform.SetParent(_savedCamParent, true);
                    cam2.transform.localPosition = _savedCamLocalPos;
                    cam2.transform.localRotation = _savedCamLocalRot;
                }

                // Reset first-plane flag so next spawn will reposition camera again
                _firstPlaneSetupDone = false;
            }
        });

        // If shooter has override visuals specified in the inspector, pass them through
        if (overrideVisualPrefab != null) ep.visualPrefab = overrideVisualPrefab;
        if (!string.IsNullOrEmpty(visualChildNameOverride)) ep.visualChildName = visualChildNameOverride;
        if (visualRootOverride != null) ep.visualRootOverride = visualRootOverride;

        // Apply runtime-only settings to the spawned instance. Prefer values from a
        // provided settings asset so editing shared values doesn't modify the prefab.
        if (settingsAsset != null)
        {
            var so = settingsAsset as ScriptableObject;
            if (so != null)
            {
                var t = so.GetType();
                // Use reflection to read expected public fields if they exist.
                var f = t.GetField("extendSpeed", BindingFlags.Public | BindingFlags.Instance); if (f != null) ep.extendSpeed = ConvertToFloat(f.GetValue(so));
                f = t.GetField("retractSpeed", BindingFlags.Public | BindingFlags.Instance); if (f != null) ep.retractSpeed = ConvertToFloat(f.GetValue(so));
                f = t.GetField("maxDuration", BindingFlags.Public | BindingFlags.Instance); if (f != null) ep.maxDuration = ConvertToFloat(f.GetValue(so));
                f = t.GetField("maxLength", BindingFlags.Public | BindingFlags.Instance); if (f != null) ep.maxLength = ConvertToFloat(f.GetValue(so));
                f = t.GetField("verticalSpawnOffset", BindingFlags.Public | BindingFlags.Instance); if (f != null) ep.verticalSpawnOffset = ConvertToFloat(f.GetValue(so));
                f = t.GetField("pushSpeed", BindingFlags.Public | BindingFlags.Instance); if (f != null) ep.pushSpeed = ConvertToFloat(f.GetValue(so));
                f = t.GetField("pushContactCheckDistance", BindingFlags.Public | BindingFlags.Instance); if (f != null) ep.pushContactCheckDistance = ConvertToFloat(f.GetValue(so));
            }
        }
        else
        {
            // Apply shooter inspector values as runtime overrides (do not change the prefab asset).
            ep.extendSpeed = defaultExtendSpeed;
            ep.retractSpeed = defaultRetractSpeed;
            ep.maxDuration = defaultMaxDuration;
            ep.maxLength = defaultMaxLength;
            ep.verticalSpawnOffset = spawnVerticalOffset;
        }

        // Helper to safely convert field values to float using reflection
        float ConvertToFloat(object val)
        {
            if (val == null) return 0f;
            if (val is float f) return f;
            if (val is double d) return (float)d;
            if (val is int i) return i;
            try { return Convert.ToSingle(val); } catch { return 0f; }
        }

        // Position and rotation will be handled by the ExtendablePlane's Awake/Update which uses ep.origin
    }
}
