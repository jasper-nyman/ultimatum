using UnityEngine;
using UnityEngine.Events;

// ScriptableObject that holds data for an item. Create instances via the
// Create Asset menu ("Scriptable Objects/Item Data"). These objects are
// shared data containers and are ideal for defining items in your game.
[CreateAssetMenu(fileName = "New Item Data", menuName = "Scriptable Objects/Item Data")]
public class ItemData : ScriptableObject
{
    // The sprite used in UI and on the MeshRenderer when representing the item.
    public Sprite sprite;

    // Optional description text visible in the inspector (TextArea provides a larger field).
    [TextArea] public string description;

    public bool isConsumable;

    // UnityEvent that defines the behavior that happens when the item is used.
    // You can hook functions in the inspector to make items cause effects without
    // writing custom code for each item.
    public UnityEvent itemBehavior;

    public GameObject model;

    [Tooltip("If true, using this item will restore the player's stamina to full.")]
    public bool restoreFullStaminaOnUse = false;

    public void Punch()
    {
        FindFirstObjectByType<PlaneShooter>().SpawnPlane();
    }
}
