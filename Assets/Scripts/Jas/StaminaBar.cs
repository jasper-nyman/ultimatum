using UnityEngine;

public class StaminaBar : MonoBehaviour
{
    RectTransform bar;
    PlayerVariables var;

    private void Start()
    {
        bar = GetComponent<RectTransform>();
        var = GameObject.FindWithTag("Player").GetComponent<PlayerVariables>();
    }

    private void Update()
    {
        float width = (var.stamina / 100);
        bar.localScale = new Vector3(width, 1, 1);
    }
}
