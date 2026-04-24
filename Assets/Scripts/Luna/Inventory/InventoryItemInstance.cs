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
            // Try several ways to obtain the PlayerStamina instance. If none exists, attach one to the Player object.
            PlayerStamina ps = null;
            try { ps = FindFirstObjectByType<PlayerStamina>(); } catch { ps = null; }
            if (ps == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    ps = player.GetComponent<PlayerStamina>();
                    if (ps == null)
                    {
                        // Add the component so stamina can be tracked/restored
                        ps = player.AddComponent<PlayerStamina>();
                    }
                }
            }

            if (ps != null)
            {
                ps.RestoreFull();
            }
            else
            {
                Debug.LogWarning("No PlayerStamina found and no Player GameObject tagged 'Player' to add it to. Stamina not restored.", this);
            }
        }

        // If this item should spawn a throwable noise-maker, create one in front of the player
        if (data.spawnThrowableOnUse)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                // Determine spawn position slightly in front of player's head
                Camera cam = Camera.main;
                Vector3 spawnPos = player.transform.position + player.transform.forward * 1f + Vector3.up * 1.2f;
                Quaternion rot = Quaternion.identity;
                if (cam != null)
                {
                    spawnPos = cam.transform.position + cam.transform.forward * 0.8f;
                    rot = cam.transform.rotation;
                }

                GameObject go = null;
                if (data.throwableModel != null)
                {
                    go = Instantiate(data.throwableModel, spawnPos, rot);
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = spawnPos;
                    go.transform.rotation = rot;
                }

                // Ensure it has a Rigidbody and NoiseMakerThrown behaviour
                var rb = go.GetComponent<Rigidbody>();
                if (rb == null) rb = go.AddComponent<Rigidbody>();
                var nm = go.GetComponent<NoiseMakerThrown>();
                if (nm == null) nm = go.AddComponent<NoiseMakerThrown>();
                nm.stayDuration = Mathf.Max(0.1f, data.throwableDuration);

                // Apply initial throw force
                rb.AddForce((cam != null ? cam.transform.forward : player.transform.forward) * data.throwableThrowForce, ForceMode.VelocityChange);

                // Remove collider's mesh if we created a primitive and keep a BoxCollider instead
                var mc = go.GetComponent<MeshCollider>();
                if (mc != null) { Destroy(mc); }
                var col = go.GetComponent<Collider>();
                if (col == null) go.AddComponent<BoxCollider>();
            }
            else
            {
                Debug.LogWarning("No Player found to spawn throwable from.", this);
            }
        }

        if (data.isConsumable)
        {
            var inv = FindFirstObjectByType<Inventory>();
            if (inv != null)
            {
                inv.items.Remove(data);
                inv.EvaluateInventory();
                Inventory.NotifyItemUsed();
            }
        }
    }
}
