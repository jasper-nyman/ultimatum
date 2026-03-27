using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public float interactRange = 3f;
    public LayerMask interactableLayer;

    public void Interact(InputAction.CallbackContext context)
    {
        if (context.started)
        {
           if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, interactRange, interactableLayer))
           {
               IInteractable interactable = hit.collider.GetComponent<IInteractable>();
               interactable?.Interact();
           }
        }
    }
}
