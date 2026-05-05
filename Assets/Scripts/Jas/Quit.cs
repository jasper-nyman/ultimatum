using UnityEngine;

public class Quit : MonoBehaviour
{
    void Start()
    {
        QuitGame();
    }

    void QuitGame()
    {
        Debug.Log("Quitting application...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
