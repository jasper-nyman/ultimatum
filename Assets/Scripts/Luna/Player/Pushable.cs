using UnityEngine;

/// <summary>
/// Marker component to indicate an object can be pushed by the <see cref="ExtendablePlane"/>.
/// Attach this to any world object you want the plane to move/slide. The component
/// ensures a <see cref="Rigidbody"/> exists and documents the behavior expected by
/// the pushing system.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class Pushable : MonoBehaviour
{
    // This class intentionally contains no logic. It serves as a semantic marker
    // so the ExtendablePlane can detect whether a hit object should be slid along
    // instead of treating it as an immovable obstacle.

#if UNITY_EDITOR
    // In the editor, ensure a Rigidbody component is present and warn if it's missing.
    // This helps designers avoid runtime issues by automatically adding the required component.
    private void Reset()
    {
        if (GetComponent<Rigidbody>() == null)
            gameObject.AddComponent<Rigidbody>();
    }
#endif
}
