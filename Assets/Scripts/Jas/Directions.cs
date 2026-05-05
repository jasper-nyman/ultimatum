using UnityEngine;
using UnityEngine.InputSystem;

public class Directions : MonoBehaviour
{
    private void Update()
    {
        //check if literally anything has been pressed (mouse button, keyboard key, controller button) using the new input system
        if (Keyboard.current.anyKey.isPressed || Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed)
        {
            //load the next scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }
    }
}
