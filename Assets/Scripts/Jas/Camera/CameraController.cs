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
    private float speedReal;

    private void Start()
    {
        QualitySettings.vSyncCount = 1;
    }

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
                offset = tracker.positionOffset;
                position = target.transform.position + offset;

                // Determine the speed to use for the camera's movement based on the speedOverride value in the CameraTracker component
                switch (tracker.speedOverride)
                {
                    case -1:
                        speedReal = float.MaxValue;
                        break;

                    case 0:
                        speedReal = speed;
                        break;
                }

                if (tracker.speedOverride > 0)
                {
                    speedReal = tracker.speedOverride;
                }
            }
            else
            {
                // If the target does not have a CameraTracker component, use the target's position without any offset
                position = target.transform.position;
                speedReal = speed;
            }
        }
    }

    private void LateUpdate()
    {
        // Smoothly interpolate the camera's position and rotation towards the target position and rotation
        transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * speedReal);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(rotation), Time.deltaTime * speedReal);

        // If the camera is very close to the target position, snap it to the target position to prevent jittering
        if (Vector3.Distance(transform.position, position) < 0.01f)
        {
            transform.position = position;
        }
    }
}