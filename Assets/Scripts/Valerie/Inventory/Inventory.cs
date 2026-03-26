
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    public List<ItemData> items = new();
    public float index;
    public RectTransform Selector;
    public RectTransform[] Slots;
    public float ScrollSpeed;

    public void EvaluateInventory()
    {
        for (int i = 0; i < slots.Length; ++i)
        {
            slots[i].SetItem(i < items.Count ? items[i] : null);
        }
    }

    private inventorySlot[] slots;

    private void Awake()
    {
        slots = GetComponentsInChildren<inventorySlot>(true);
    }

    public void Scroll(InputAction.CallbackContext ctx)
    {
        index += ctx.ReadValue<float>()/ScrollSpeed;
    }

    public void UseItem(InputAction.CallbackContext ctx)
    {
        int i = (int)Mathf.Repeat(index, Slots.Length);
        Slots[i].GetComponentInChildren<InventoryItemInstance>(false)?.Use();
    }

    private void Update()
    {
        int i = (int)Mathf.Repeat(index, Slots.Length);
        Selector.anchoredPosition = Slots[i].anchoredPosition;
    }
}

