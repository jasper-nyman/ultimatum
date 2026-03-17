using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

public class CameraController : MonoBehaviour
{
    // References
    [Header("References")]
    public GameObject target;

    // Parameters
    [Header("Parameters")]
    public Vector3 position;
    public Vector3 rotation;
    public float speed;

    private void Update()
    {
        // Check if the target is assigned and has a transform component
        Transform transform = target.transform;

        // Update the camera's position and rotation to follow the target
        if (target != null && transform != null)
        {
            Vector3 offset;
            CameraTracker tracker;

            if (target.TryGetComponent<CameraTracker>(out tracker))
            {
                // If the target has a CameraTracker component, use its position and its offset
                offset = tracker.position;
                position = target.transform.position + offset;
            }
            else
            {
                // If the target does not have a CameraTracker component, use only its position
                position = target.transform.position;
            }
        }
    }

    private void LateUpdate()
    {
        // Smoothly interpolate the camera's position and rotation towards the target position and rotation
        transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * speed);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(rotation), Time.deltaTime * speed);
    }
}