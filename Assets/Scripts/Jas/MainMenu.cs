using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        Debug.Log("Play button pressed");
    }

    public void Quit()
    {
        Application.Quit();
        Debug.Log("Exit button pressed");
    }
}
