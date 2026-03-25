using UnityEngine;

public class inventorySlot : MonoBehaviour
{
    public void SetItem(ItemData item)
    {
        transform.GetChild(0).GetComponent<InventoryItemInstance>().data = item;
        transform.GetChild(0).gameObject.SetActive(item);
    }
}
