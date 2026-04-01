using UnityEngine;

// Marker component to indicate an object can be pushed by the ExtendablePlane.
[RequireComponent(typeof(Rigidbody))]
public class Pushable : MonoBehaviour
{
    // Intentionally minimal: serves as a marker and ensures a Rigidbody exists.
}
