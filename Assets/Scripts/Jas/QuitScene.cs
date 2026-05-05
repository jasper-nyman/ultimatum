using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitScene : MonoBehaviour
{
    public void OnQuitButtonClick()
    {
        SceneManager.LoadScene("QuitScene");
    }

}