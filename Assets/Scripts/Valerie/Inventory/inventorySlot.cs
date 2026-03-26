using UnityEngine;
using UnityEngine.UI;

public class inventorySlot : MonoBehaviour
{
    public void SetItem(ItemData item)
    {
        transform.GetChild(0).GetComponent<InventoryItemInstance>().data = item;
        transform.GetChild(0).GetComponent<Image>().sprite = item ? item.sprite : null;
        transform.GetChild(0).gameObject.SetActive(item);
    }
}
