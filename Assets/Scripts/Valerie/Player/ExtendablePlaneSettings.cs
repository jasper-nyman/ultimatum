using UnityEngine;

[CreateAssetMenu(fileName = "ExtendablePlaneSettings", menuName = "Valerie/ExtendablePlaneSettings", order = 1)]
public class ExtendablePlaneSettings : ScriptableObject
{
    public float extendSpeed = 10f;
    public float retractSpeed = 80f;
    public float maxDuration = 10f;
    public float maxLength = 50f;
    public float verticalSpawnOffset = 1.5f;
    public float pushSpeed = 5f;
    public float pushContactCheckDistance = 0.05f;
}
