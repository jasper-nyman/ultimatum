using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image), typeof(Button))]
public class InventoryItemInstance : MonoBehaviour
{
    public ItemData data;

    private Image _image;


    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.sprite = data.sprite;
    }


    public void Use()
    {

    }
}
