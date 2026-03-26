using UnityEngine;
using UnityEngine.UI;

// This component represents an item inside an inventory UI slot. It holds a reference
// to the ItemData and is responsible for showing the sprite and performing the
// item's behavior when used.
[RequireComponent(typeof(Image), typeof(Button))]
public class InventoryItemInstance : MonoBehaviour
{
    // The ItemData that this UI instance displays and will use when activated.
    public ItemData data;

    // Cached Image component used to set the sprite.
    private Image _image;

    private void Awake()
    {
        // Grab the Image component and set its sprite according to the assigned ItemData.
        _image = GetComponent<Image>();
        _image.sprite = data.sprite;
    }

    // Called when the player 'uses' the item (for example via UI button or hotkey).
    // This invokes the UnityEvent `itemBehavior` defined on the ItemData (this can
    // be configured in the inspector to trigger effects), then removes the item
    // from the Inventory list and tells the Inventory to refresh the UI.
    public void Use()
    {
        data.itemBehavior.Invoke();

        FindFirstObjectByType<Inventory>().items.Remove(data);
        FindFirstObjectByType<Inventory>().EvaluateInventory();
    }
}
