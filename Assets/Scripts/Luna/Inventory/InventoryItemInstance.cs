using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// This component represents an item inside an inventory UI slot. It holds a reference
// to the ItemData and is responsible for showing the sprite and performing the
// item's behavior when used.
[RequireComponent(typeof(Image), typeof(Button))]
public class InventoryItemInstance : MonoBehaviour
    , IPointerEnterHandler, IPointerExitHandler
{
    // Summary:
    // This component lives on the visual child inside each inventory slot. It
    // displays an `ItemData` sprite, handles pointer enter/exit events (so the
    // shooter can detect when the user is hovering UI), and exposes `Use()` to
    // trigger the configured `itemBehavior` on the assigned `ItemData`.

    // The ItemData that this UI instance displays and will use when activated.
    public ItemData data;

    // Cached Image component used to set the sprite.
    private Image _image;

    private void Awake()
    {
        // Grab the Image component and set its sprite according to the assigned ItemData.
        _image = GetComponent<Image>();
        // If no ItemData is assigned (empty slot prefab), ensure we don't try to
        // dereference it. The slot controller will call SetData when assigning items.
        _image.sprite = data != null ? data.sprite : null;
    }

    // Helper used by inventorySlot to update the displayed ItemData at runtime.
    public void SetData(ItemData newData)
    {
        data = newData;
        if (_image == null) _image = GetComponent<Image>();
        _image.sprite = data != null ? data.sprite : null;
    }

    // Tracks how many inventory item UI elements currently have the pointer over them.
    // Use a counter to be robust to multiple pointers/entries.
    private static int s_pointerOverCount = 0;

    // Returns true if the mouse pointer is currently over any inventory item UI.
    public static bool IsPointerOverAnyItem() => s_pointerOverCount > 0;

    // IPointerEnterHandler implementation
    public void OnPointerEnter(PointerEventData eventData)
    {
        s_pointerOverCount = Mathf.Max(0, s_pointerOverCount) + 1;
    }

    // IPointerExitHandler implementation
    public void OnPointerExit(PointerEventData eventData)
    {
        s_pointerOverCount = Mathf.Max(0, s_pointerOverCount - 1);
    }

    // Called when the player 'uses' the item (for example via UI button or hotkey).
    // This invokes the UnityEvent `itemBehavior` defined on the ItemData (this can
    // be configured in the inspector to trigger effects), then removes the item
    // from the Inventory list and tells the Inventory to refresh the UI.
    public void Use(InputAction.CallbackContext ctx)
    {
        if (!ctx.started)
            return;

        if (data == null)
        {
            Debug.LogWarning("InventoryItemInstance.Use called but data is null", this);
            return;
        }

        if (data.itemBehavior != null)
        {
            data.itemBehavior.Invoke();
        }

        // If this item is configured to restore stamina, apply it to the player's PlayerStamina component
        if (data.restoreFullStaminaOnUse)
        {
            var ps = FindFirstObjectByType<PlayerStamina>();
            if (ps != null)
            {
                ps.RestoreFull();
            }
        }

        if (data.isConsumable)
        {
            var inv = FindFirstObjectByType<Inventory>();
            if (inv != null)
            {
                inv.items.Remove(data);
                inv.EvaluateInventory();
            }
        }
    }
}
