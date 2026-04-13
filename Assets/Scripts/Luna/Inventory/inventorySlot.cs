using UnityEngine;

// Simple helper component placed on each UI slot. The slot assumes its first child
// is the visual InventoryItemInstance that displays the sprite and reacts to clicks.
public class inventorySlot : MonoBehaviour
{
    // Set the item to display in this slot. If `item` is null the slot's child is
    // deactivated so the slot appears empty.
    public void SetItem(ItemData item)
    {
        if (transform.childCount == 0) return;
        var child = transform.GetChild(0);
        var inst = child.GetComponent<InventoryItemInstance>();
        if (inst != null)
        {
            inst.SetData(item);
        }
        // Activate/deactivate the visual child GameObject based on whether an item
        // is present (non-null). Using a boolean avoids ambiguity with GameObject.SetActive overloads.
        child.gameObject.SetActive(item != null);
    }
}
