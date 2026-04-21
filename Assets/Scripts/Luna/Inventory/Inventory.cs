using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
// using Valerie.Player;

// Inventory component: holds a list of collected ItemData and manages the UI slots
// This component is responsible for tracking what items the player has and
// updating the on-screen UI (slots + selector) accordingly.
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public static ItemData CurrentSelected
    {
        get
        {
            if (Instance == null) return null;
            int len = 0;
            if (Instance.Slots != null && Instance.Slots.Length > 0) len = Instance.Slots.Length;
            else if (Instance.slots != null && Instance.slots.Length > 0) len = Instance.slots.Length;
            if (len == 0) return null;
            int i = (int)Mathf.Repeat(Instance.index, len);
            InventoryItemInstance inst = null;
            if (Instance.Slots != null && Instance.Slots.Length > 0)
            {
                var rt = Instance.Slots[i];
                if (rt != null) inst = rt.GetComponentInChildren<InventoryItemInstance>(false);
            }
            else if (Instance.slots != null && Instance.slots.Length > 0)
            {
                var s = Instance.slots[i];
                if (s != null) inst = s.GetComponentInChildren<InventoryItemInstance>(false);
            }
            return inst != null ? inst.data : null;
        }
    }

    // Summary:
    // The Inventory component stores a list of ItemData instances the player has
    // collected. It manages UI slot components (`inventorySlot`) and a selector
    // rect transform to indicate the currently selected slot. Input actions call
    // `Scroll` to move the selector and `UseItem` to activate the selected item.

    // The list of ItemData objects the player currently has collected.
    // This is the canonical inventory content data.
    public List<ItemData> items = new();

    // A floating index used for scrolling through the slots. Using a float
    // lets the scroll input be smooth and wrap using Mathf.Repeat.
    public float index;

    // The RectTransform of the UI selector that highlights the currently selected slot.
    public RectTransform Selector;

    // Array of slot RectTransforms in the UI. Each should contain an InventoryItemInstance child.
    public RectTransform[] Slots;

    // How fast the scroll input affects the index. Larger values make scrolling slower.
    public float ScrollSpeed;

    // Update the visual slot UI to match the current contents of `items`.
    // We iterate over all slot components and call SetItem with either the
    // corresponding ItemData or null if there is no item for that slot.
    public void EvaluateInventory()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; ++i)
        {
            slots[i].SetItem(i < items.Count ? items[i] : null);
        }
    }

    // Timestamp of the last time an inventory item was used. PlaneShooter will
    // check this to avoid spawning a plane immediately after an item use.
    public static float lastItemUseTime = -Mathf.Infinity;

    // Call this when an item is used to notify other systems (like PlaneShooter)
    // so they can suppress conflicting actions for a short window.
    public static void NotifyItemUsed()
    {
        lastItemUseTime = Time.time;
    }

    // When true, input-related methods such as Scroll and UseItem should ignore input.
    // PlaneShooter sets this while a plane is active so inventory cannot be changed.
    public bool inputLocked = false;

    // Cached array of inventorySlot components (children of this GameObject).
    // We use these to update individual UI slots in EvaluateInventory.
    private inventorySlot[] slots;

    private void Awake()
    {
        // Gather all inventorySlot components in children (including inactive ones)
        // so EvaluateInventory can update them even if some are currently disabled.
        slots = GetComponentsInChildren<inventorySlot>(true);
        Instance = this;
    }

    // Called by the Input System when the scroll action is used. The project
    // uses the new Input System, so this method expects an InputAction callback.
    // We read a float (usually -1..1) and add it to the index scaled by ScrollSpeed.
    public void Scroll(InputAction.CallbackContext ctx)
    {
        if (Mathf.Approximately(ScrollSpeed, 0f)) return;
        if (inputLocked) return;
        index += ctx.ReadValue<float>()/ScrollSpeed;
    }

    // Called by an Input Action when the 'use' input is performed. This method
    // uses the currently selected slot (computed from index) and calls Use()
    // on the InventoryItemInstance inside that slot if there is one.
    public void UseItem(InputAction.CallbackContext ctx)
    {
        if (inputLocked) return;
        if (Slots == null || Slots.Length == 0) return;
        int i = (int)Mathf.Repeat(index, Slots.Length);
        // Try to get the InventoryItemInstance in the selected slot and call Use()
        Slots[i].GetComponentInChildren<InventoryItemInstance>(false)?.Use(ctx);
    }

    // Returns true if the selector currently points to a slot that contains an item.
    // This helper abstracts whether the project uses the public `Slots` array or the
    // cached `slots` inventorySlot array so callers don't need to rely on inspector setup.
    public bool IsSelectingItem()
    {
        int len = 0;
        if (Slots != null && Slots.Length > 0) len = Slots.Length;
        else if (slots != null && slots.Length > 0) len = slots.Length;
        if (len == 0) return false;

        int sel = (int)Mathf.Repeat(index, len);

        // Prefer checking the actual InventoryItemInstance on the slot so we know
        // whether a concrete ItemData is assigned.
        InventoryItemInstance inst = null;
        if (Slots != null && Slots.Length > 0)
        {
            var rt = Slots[sel];
            if (rt != null) inst = rt.GetComponentInChildren<InventoryItemInstance>(false);
        }
        else if (slots != null && slots.Length > 0)
        {
            var s = slots[sel];
            if (s != null) inst = s.GetComponentInChildren<InventoryItemInstance>(false);
        }

        return inst != null && inst.data != null;
    }

    private void Update()
    {
        // Each frame, update the selector's position to match the currently selected slot.
        // Support two workflows: user assigned a public `Slots` array in the inspector or
        // relying on the automatic `inventorySlot` children cached in `slots`.
        if (Selector == null) return;

        int len = 0;
        bool usePublic = (Slots != null && Slots.Length > 0);
        if (usePublic) len = Slots.Length;
        else if (slots != null && slots.Length > 0) len = slots.Length;
        if (len == 0) return;

        int i = (int)Mathf.Repeat(index, len);

        RectTransform targetRt = null;
        if (usePublic)
        {
            targetRt = Slots[i];
        }
        else
        {
            var s = slots[i];
            if (s != null) targetRt = s.GetComponent<RectTransform>();
        }

        if (targetRt == null) return;

        // If the selector and target share the same parent RectTransform, we can directly
        // copy the anchoredPosition which is resolution-agnostic when using a Canvas with
        // a Screen Space or Scale With Screen Size Canvas Scaler.
        var parentRt = Selector.parent as RectTransform;
        if (parentRt == null)
        {
            Selector.position = targetRt.position;
            return;
        }

        if (targetRt.parent == parentRt)
        {
            // Directly match anchoredPosition when possible — avoids camera/screen conversions
            Selector.anchoredPosition = targetRt.anchoredPosition;
            return;
        }

        // Otherwise convert the target's world position into the selector's parent local space
        // taking into account the Canvas render mode and camera.
        var canvas = targetRt.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            cam = canvas.worldCamera;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, targetRt.position);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, screenPoint, cam, out localPoint);
        Selector.anchoredPosition = localPoint;
    }
}

