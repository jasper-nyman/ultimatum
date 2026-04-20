using UnityEngine;

// Simple component to track player stamina (0-100)
public class PlayerStamina : MonoBehaviour
{
    public float stamina = 100f;
    public float maxStamina = 100f;

    public void RestoreFull()
    {
        stamina = maxStamina;
    }
}
