using UnityEngine;
using UnityEngine.SceneManagement;

public class Start : MonoBehaviour
{
    public void OnStartButtonClick()
    {
        SceneManager.LoadScene("IntroScene");
    }

}