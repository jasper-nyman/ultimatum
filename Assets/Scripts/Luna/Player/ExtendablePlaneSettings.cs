using UnityEngine;

[CreateAssetMenu(fileName = "ExtendablePlaneSettings", menuName = "Luna/ExtendablePlaneSettings", order = 1)]
public class ExtendablePlaneSettings : ScriptableObject
{
    // ScriptableObject used to share plane configuration across multiple
    // PlaneShooter instances. Create via Create->Luna->ExtendablePlaneSettings
    // and assign to the `PlaneShooter.settingsAsset` field to centrally tune
    // behavior without modifying prefab assets.
    public float extendSpeed = 10f;
    public float retractSpeed = 80f;
    public float maxDuration = 10f;
    public float maxLength = 50f;
    public float verticalSpawnOffset = 1.5f;
    public float pushSpeed = 5f;
    public float pushContactCheckDistance = 0.05f;
}
