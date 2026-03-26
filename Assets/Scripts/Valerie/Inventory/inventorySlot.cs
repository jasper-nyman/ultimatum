using UnityEngine;

// Simple helper component placed on each UI slot. The slot assumes its first child
// is the visual InventoryItemInstance that displays the sprite and reacts to clicks.
public class inventorySlot : MonoBehaviour
{
    // Set the item to display in this slot. If `item` is null the slot's child is
    // deactivated so the slot appears empty.
    public void SetItem(ItemData item)
    {
        transform.GetChild(0).GetComponent<InventoryItemInstance>().data = item;
        transform.GetChild(0).gameObject.SetActive(item);
    }
}
