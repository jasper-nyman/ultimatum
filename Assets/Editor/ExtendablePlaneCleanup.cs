#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Editor helper: remove any ExtendablePlane instances created in the scene while not in Play mode.
// This prevents accidental duplication caused by editor script/serialization cycles when editing
// inspector values on the PlaneShooter GameObject.
[InitializeOnLoad]
public static class ExtendablePlaneCleanup
{
    static ExtendablePlaneCleanup()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        // Also run once at load
        EditorApplication.delayCall += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged()
    {
        if (Application.isPlaying) return;

        // Find all ExtendablePlane components in the editor scene and destroy their GameObjects
        var planes = new System.Collections.Generic.List<UnityEngine.Component>();
        foreach (var comp in UnityEngine.Object.FindObjectsOfType<UnityEngine.Component>())
        {
            if (comp == null) continue;
            if (comp.GetType().Name == "ExtendablePlane") planes.Add(comp);
        }
        foreach (var p in planes)
        {
            if (p == null) continue;
            var go = p.gameObject;
            if (go == null) continue;
            // Only destroy scene objects, not assets. Check if the GameObject belongs to a loaded scene.
            if (go.scene.IsValid())
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
