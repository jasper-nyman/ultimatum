using UnityEngine;
using UnityEngine.UI;

public class FadeIn : MonoBehaviour
{
    Image fade;
    public float fadeTime = 2f;
    float time = 0;

    private void Awake()
    {
        fade = GetComponent<Image>();
        time = 0;
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (time > 1f)
        {
            fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, Mathf.MoveTowards(fade.color.a, 0, Time.deltaTime / fadeTime));
        }
    }
}