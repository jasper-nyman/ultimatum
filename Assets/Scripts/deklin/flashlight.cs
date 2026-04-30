using UnityEngine;
using UnityEngine.InputSystem;

public class flashlight : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    
    
    }
    
    public void ToggleFlashlight(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            GetComponent<Light>().enabled = true;
        }
        else if (context.canceled)
        {
            GetComponent<Light>().enabled = false;
        }
    }
    



}

