using UnityEngine;

public class CameraController : MonoBehaviour
{
    // References
    [Header("References")]
    public GameObject target;
    public Vector3 position;
    public Vector3 offset;
    Vector3 targetOffset;
    public float trackingSpeed;

    private void Update()
    {
        if (target)
        {
            // Check if the target has a CameraTargetOffset script and get the offset
            if (target.TryGetComponent(out CameraTargetOffset targetOffsetScript))
            {
                targetOffset = targetOffsetScript.offset;
            }
            else
            {
                targetOffset = Vector3.zero;
            }

            // Update the position to the target's position
            position = target.transform.position + offset + targetOffset;
        }
    }

    private void LateUpdate()
    {
        // Track the target
        transform.position = Vector3.Lerp
        (
            a: transform.position,
            b: position,
            t: 60 / trackingSpeed * Time.deltaTime
        );
    }
}