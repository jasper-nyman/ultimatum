using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SpawnedLifetime : MonoBehaviour
{
    public float lifetime = 60f;
    private double _startTime;

    private void OnEnable()
    {
        _startTime = Time.realtimeSinceStartupAsDouble;
    }

    private void Update()
    {
        if (lifetime <= 0f) return;
        double elapsed = Time.realtimeSinceStartupAsDouble - _startTime;
        if (elapsed >= lifetime)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                // In editor, destroy immediately
                #if UNITY_EDITOR
                DestroyImmediate(gameObject);
                #else
                Destroy(gameObject);
                #endif
            }
        }
    }
}
