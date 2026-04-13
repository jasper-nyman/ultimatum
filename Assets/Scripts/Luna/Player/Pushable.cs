using UnityEngine;

// Marker component to indicate an object can be pushed by the ExtendablePlane.
[RequireComponent(typeof(Rigidbody))]
public class Pushable : MonoBehaviour
{
    // Intentionally minimal: serves as a marker and ensures a Rigidbody exists.
    // Add this component to any world object you want the plane to be able to
    // slide/push. The object's Rigidbody will be temporarily set to kinematic
    // while being pushed so its transform can be moved deterministically.
}
