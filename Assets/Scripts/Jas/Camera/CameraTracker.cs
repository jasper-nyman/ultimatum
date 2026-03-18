using UnityEngine;

public class CameraTracker : MonoBehaviour
{
    public Vector3 positionOffset;
    [Header("Camera Speed Override<br><br>0 for Default Speed<br>-1 for Locked Camera")]
    public float speedOverride;
}