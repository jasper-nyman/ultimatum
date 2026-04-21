using UnityEngine;

public class StaminaBar : MonoBehaviour
{
    RectTransform bar;
    PlayerStamina stamina;

    private void Start()
    {
        bar = GetComponent<RectTransform>();
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            stamina = player.GetComponent<PlayerStamina>();
            if (stamina == null)
            {
                // Add a PlayerStamina component if one doesn't exist so the UI has a source.
                stamina = player.AddComponent<PlayerStamina>();
            }
        }
    }

    private void Update()
    {
        if (stamina == null) return;
        float width = (stamina.stamina / Mathf.Max(1f, stamina.maxStamina));
        bar.localScale = new Vector3(Mathf.Clamp01(width), 1, 1);
    }
}
