using System.Collections.Generic;
using UnityEngine;

// Attach to an empty GameObject. Configure the box `areaSize`, the player transform
// and a list of prefabs to spawn. Press the context-menu "SpawnItemsNow" in the
// inspector (or call SpawnOnce() at runtime) to spawn a random number of items.
// Spawns will be placed using a downward raycast so they land on surfaces in the world.
[ExecuteAlways]
public class RandomSurfaceSpawner : MonoBehaviour
{
    [Header("Area")]
    [Tooltip("Size of the spawn area (local space). Centered on this GameObject.")]
    public Vector3 areaSize = new Vector3(10f, 0f, 10f);

    [Header("Spawn settings")]
    public GameObject[] prefabs;
    [Tooltip("Minimum number of items to spawn")]
    public int minCount = 3;
    [Tooltip("Maximum number of items to spawn")]
    public int maxCount = 8;
    [Tooltip("Maximum attempts when trying to find a valid spawn position per item")] 
    public int attemptsPerItem = 20;
    [Tooltip("Vertical raycast height above the candidate point when sampling surfaces")]
    public float raycastUp = 5f;
    [Tooltip("Maximum distance down to search for a surface")]
    public float raycastDown = 10f;
    [Tooltip("Prefab will be placed slightly above the hit point by this offset to avoid z-fighting")]
    public float spawnHeightOffset = 0.02f;
    [Tooltip("Layers considered to be valid surfaces for spawning")] 
    public LayerMask surfaceMask = ~0;

    [Header("Player distance")]
    [Tooltip("Player transform reference used to enforce min/max spawn distance")]
    public Transform player;
    [Tooltip("Minimum distance from player to spawn (inclusive)")]
    public float minDistanceFromPlayer = 5f;
    [Tooltip("Maximum distance from player to spawn (inclusive). Set to 0 to ignore.")]
    public float maxDistanceFromPlayer = 0f;

    [Header("Behavior")]
    [Tooltip("If true, the spawner will run automatically at Start (once)")]
    public bool spawnOnStart = false;
    [Tooltip("If true, allow spawning from the editor via the context menu (SpawnItemsNow)")]
    public bool allowEditorSpawn = true;

    [Header("Spawned lifetime")]
    [Tooltip("Lifetime in seconds for each spawned object. <=0 means never auto-destroy.")]
    public float spawnedLifetime = 60f;

     [SerializeField]
    private bool hasSpawned = false;

    // Container to parent spawned instances for cleanliness
    private GameObject _spawnContainer;

    private void Start()
    {
        if (spawnOnStart && !hasSpawned)
        {
            SpawnOnce();
        }
    }

    /// <summary>
    /// Spawns items once. Will not run again unless ResetSpawn() is called.
    /// </summary>
    public void SpawnOnce()
    {
        if (hasSpawned) return;

        // If not configured to forcibly spawn on start, block spawning entirely
        // when the player is outside the configured distance range from the spawner.
        if (!spawnOnStart && player != null)
        {
            float d = Vector3.Distance(player.position, transform.position);
            if (d < minDistanceFromPlayer) {
                Debug.Log("RandomSurfaceSpawner: player too close to spawn area; aborting spawn.");
                return;
            }
            if (maxDistanceFromPlayer > 0f && d > maxDistanceFromPlayer) {
                Debug.Log("RandomSurfaceSpawner: player too far from spawn area; aborting spawn.");
                return;
            }
        }

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("RandomSurfaceSpawner: no prefabs assigned.");
            return;
        }

        int count = Random.Range(minCount, maxCount + 1);
        if (count <= 0) return;

        _spawnContainer = new GameObject(name + "_Spawned");
        _spawnContainer.transform.SetParent(transform, false);

        int spawned = 0;
        int tries = 0;

        for (int i = 0; i < count; i++)
        {
            bool placed = false;
            for (int a = 0; a < attemptsPerItem; a++)
            {
                tries++;
                Vector3 local = new Vector3(
                    Random.Range(-0.5f * areaSize.x, 0.5f * areaSize.x),
                    0f,
                    Random.Range(-0.5f * areaSize.z, 0.5f * areaSize.z)
                );

                Vector3 worldCandidate = transform.TransformPoint(local + new Vector3(0f, areaSize.y * 0.5f, 0f));

                // enforce distance from player if provided
                if (player != null)
                {
                    float d = Vector3.Distance(player.position, worldCandidate);
                    if (d < minDistanceFromPlayer) continue;
                    if (maxDistanceFromPlayer > 0f && d > maxDistanceFromPlayer) continue;
                }

                // Raycast down from above the candidate point
                Vector3 rayOrigin = worldCandidate + Vector3.up * raycastUp;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastUp + raycastDown, surfaceMask, QueryTriggerInteraction.Ignore))
                {
                    // place object at hit point
                    var chosen = prefabs[Random.Range(0, prefabs.Length)];
                    if (chosen == null) break;

                    var go = Instantiate(chosen, hit.point + hit.normal * spawnHeightOffset, Quaternion.FromToRotation(Vector3.up, hit.normal));
                    // random yaw so objects don't all face same direction
                    go.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.Self);
                    go.transform.SetParent(_spawnContainer.transform, true);

                    // If spawned object has a Rigidbody ensure it's not kinematic so physics work
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                    }

                    // Attach or set spawned lifetime behaviour so spawned items auto-destroy
                    if (spawnedLifetime > 0f)
                    {
                        var life = go.GetComponent<SpawnedLifetime>();
                        if (life == null) life = go.AddComponent<SpawnedLifetime>();
                        life.lifetime = spawnedLifetime;
                    }

                    spawned++;
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                // Optionally log failed placement attempts for debugging
                // Debug.Log("RandomSurfaceSpawner: failed to place item after attempts.");
            }
        }

        hasSpawned = true;
        Debug.Log($"RandomSurfaceSpawner: spawned {spawned}/{count} items after {tries} tries.");
    }

    /// <summary>
    /// Resets the spawner so it can spawn again. Also clears the spawned instances it created.
    /// </summary>
    public void ResetSpawn()
    {
        hasSpawned = false;
        if (_spawnContainer != null)
        {
            #if UNITY_EDITOR
            // DestroyImmediate in editor
            DestroyImmediate(_spawnContainer);
            #else
            Destroy(_spawnContainer);
            #endif
            _spawnContainer = null;
        }
    }

    [ContextMenu("SpawnItemsNow")]
    private void SpawnItemsNowContext()
    {
        if (!allowEditorSpawn) return;
        SpawnOnce();
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.25f);
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.up * (areaSize.y * 0.5f), areaSize);
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.9f);
        Gizmos.DrawWireCube(Vector3.up * (areaSize.y * 0.5f), areaSize);
        Gizmos.matrix = prev;
    }
}
