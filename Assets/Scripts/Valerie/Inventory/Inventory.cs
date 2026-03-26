
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Inventory component: holds a list of collected ItemData and manages the UI slots
// This component is responsible for tracking what items the player has and
// updating the on-screen UI (slots + selector) accordingly.
public class Inventory : MonoBehaviour
{
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
        for (int i = 0; i < slots.Length; ++i)
        {
            slots[i].SetItem(i < items.Count ? items[i] : null);
        }
    }

    // Cached array of inventorySlot components (children of this GameObject).
    // We use these to update individual UI slots in EvaluateInventory.
    private inventorySlot[] slots;

    private void Awake()
    {
        // Gather all inventorySlot components in children (including inactive ones)
        // so EvaluateInventory can update them even if some are currently disabled.
        slots = GetComponentsInChildren<inventorySlot>(true);
    }

    // Called by the Input System when the scroll action is used. The project
    // uses the new Input System, so this method expects an InputAction callback.
    // We read a float (usually -1..1) and add it to the index scaled by ScrollSpeed.
    public void Scroll(InputAction.CallbackContext ctx)
    {
        index += ctx.ReadValue<float>()/ScrollSpeed;
    }

    // Called by an Input Action when the 'use' input is performed. This method
    // uses the currently selected slot (computed from index) and calls Use()
    // on the InventoryItemInstance inside that slot if there is one.
    public void UseItem(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            int i = (int)Mathf.Repeat(index, Slots.Length);
            // Try to get the InventoryItemInstance in the selected slot and call Use()
            Slots[i].GetComponentInChildren<InventoryItemInstance>(false)?.Use();
        }
    }

    private void Update()
    {
        // Each frame, update the selector's position to the anchoredPosition of the
        // currently selected slot. Mathf.Repeat wraps the index so scrolling wraps.
        int i = (int)Mathf.Repeat(index, Slots.Length);
        Selector.anchoredPosition = Slots[i].anchoredPosition;
    }
}

