using UnityEngine;
using UnityEngine.InputSystem;

// Represents a world item that can be clicked to collect into the player's inventory.
// This script maps the ItemData sprite onto the object's mesh material and listens
// for mouse input to collect the item when clicked.
[RequireComponent(typeof(MeshRenderer))]
public class ItemInstance : MonoBehaviour
{
    // Reference to the ScriptableObject that contains this item's data and behavior.
    public ItemData data;

    // Cached MeshRenderer used to apply the sprite texture to the object.
    private MeshRenderer _mesh;

    private void OnEnable()
    {
        // When enabled, set the material texture to the sprite's texture so the mesh shows
        // the item's visual. This assumes the material uses a _BaseMap texture property.
        _mesh = GetComponent<MeshRenderer>();
        _mesh.material.SetTexture("_BaseMap", data.sprite.texture);
    }

    private void Update()
    {
        // Using the new Input System here: check if the left mouse button was pressed this frame.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
            {
                // The raycast hit this object specifically, so collect it.
                Collect();
            }
        }
    }

    // Collect this world item into the Inventory and destroy the game object.
    public void Collect()
    {
        FindFirstObjectByType<Inventory>().items.Add(data);
        FindFirstObjectByType<Inventory>().EvaluateInventory();

        Destroy(gameObject);
    }
}
