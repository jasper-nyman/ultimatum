using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MeshRenderer))]
public class ItemInstance : MonoBehaviour
{
    public ItemData data;

    private MeshRenderer _mesh;

    private void Awake()
    {
        _mesh = GetComponent<MeshRenderer>();
        _mesh.material.SetTexture("_BaseMap", data.sprite.texture);
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
            {
                Collect();
            }
        }
    }

    private void Collect()
    {
        FindFirstObjectByType<Inventory>().items.Add(data);
        Destroy(gameObject);
    }
}
