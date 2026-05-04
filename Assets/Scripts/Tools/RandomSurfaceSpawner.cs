using System.Collections.Generic;
using UnityEngine;


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

    [Header("Spawned lifetime")]
    [Tooltip("Lifetime in seconds for each spawned object. <=0 means never auto-destroy.")]
    public float spawnedLifetime = 60f;

    private bool hasSpawned = false;

    // Container to parent spawned instances for cleanliness
    private GameObject _spawnContainer;

    private void Start()
    {
        Debug.Log($"RandomSurfaceSpawner: Start called on '{name}' (spawnOnStart={spawnOnStart}, hasSpawned={hasSpawned}, isPlaying={Application.isPlaying})");
        if (spawnOnStart && !hasSpawned)
        {
            SpawnOnce();
        }
    }

    private void OnEnable()
    {
        // If entering Play mode and spawnOnStart is requested, ensure we spawn once early.
        Debug.Log($"RandomSurfaceSpawner: OnEnable called on '{name}' (spawnOnStart={spawnOnStart}, hasSpawned={hasSpawned}, isPlaying={Application.isPlaying})");
        if (Application.isPlaying && spawnOnStart && !hasSpawned)
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

        Debug.Log($"RandomSurfaceSpawner: Begin SpawnOnce on '{name}' (spawnOnStart={spawnOnStart}, player={(player==null?"null":player.name)})");

        // We'll evaluate player-distance per candidate hit point. If spawnOnStart is true
        // we bypass distance checks (spawn immediately regardless of player position).

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("RandomSurfaceSpawner: no prefabs assigned.");
            return;
        }

        int count = Random.Range(minCount, maxCount + 1);
        if (count <= 0) return;

        _spawnContainer = new GameObject(name + "_Spawned");
        // Parent to this spawner so cleanup is easy; in Edit mode we still create it but it will be destroyed on ResetSpawn
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

                // Debug candidate position and player distance check for troubleshooting
                if (player != null && !spawnOnStart)
                {
                    float debugDist = Vector3.Distance(player.position, worldCandidate);
                    Debug.Log($"RandomSurfaceSpawner: candidate world position {worldCandidate}, distance to player {debugDist:F2}");
                }

                // enforce distance from player if provided (skip this candidate if it's too close/far)
                if (!spawnOnStart && player != null)
                {
                    float d = Vector3.Distance(player.position, worldCandidate);
                    if (d < minDistanceFromPlayer)
                    {
                        // too close to player, skip this candidate
                        Debug.Log($"RandomSurfaceSpawner: skipping candidate - too close to player (d={d:F2} < min {minDistanceFromPlayer})");
                        continue;
                    }
                    if (maxDistanceFromPlayer > 0f && d > maxDistanceFromPlayer)
                    {
                        Debug.Log($"RandomSurfaceSpawner: skipping candidate - too far from player (d={d:F2} > max {maxDistanceFromPlayer})");
                        continue;
                    }
                }

                // Raycast down from above the candidate point. Use spawner's height as reference
                // so we are sure the ray comes from well above the highest expected surface.
                float topY = transform.position.y + (areaSize.y * 0.5f) + raycastUp + 5f;
                Vector3 rayOrigin = new Vector3(worldCandidate.x, topY, worldCandidate.z);
                float rayDistance = (topY - (transform.position.y - (areaSize.y * 0.5f))) + raycastDown;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, surfaceMask, QueryTriggerInteraction.Ignore))
                {
                    Debug.Log($"RandomSurfaceSpawner: candidate hit at {hit.point} (normal {hit.normal}) for prefab attempt {i}/{a} -- rayOrigin {rayOrigin}, rayDistance {rayDistance}");
                    // place object at hit point
                    var chosen = prefabs[Random.Range(0, prefabs.Length)];
                    if (chosen == null) break;
                    var go = Instantiate(chosen, hit.point + hit.normal * spawnHeightOffset, Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f));
                    go.transform.SetParent(_spawnContainer.transform, true);

                    // If spawned object has a Rigidbody ensure it's not kinematic so physics work
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                    }

                    // Prevent spawned objects from blocking the player: ignore collisions
                    // between the spawned instance and any colliders on the player.
                    if (player != null)
                    {
                        var playerColliders = player.GetComponentsInChildren<Collider>();
                        var spawnedColliders = go.GetComponentsInChildren<Collider>();
                        if (playerColliders != null && spawnedColliders != null)
                        {
                            foreach (var pc in playerColliders)
                            {
                                if (pc == null) continue;
                                foreach (var sc in spawnedColliders)
                                {
                                    if (sc == null) continue;
                                    Physics.IgnoreCollision(pc, sc, true);
                                }
                            }
                            Debug.Log($"RandomSurfaceSpawner: ignored collisions between spawned '{go.name}' and player '{player.name}' ({playerColliders.Length} player colliders, {spawnedColliders.Length} spawned colliders).");
                        }
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
                else
                {
                    // Raycast missed at this candidate
                    // Debug.Log($"RandomSurfaceSpawner: raycast miss at {rayOrigin} downwards.");
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
    }

    [ContextMenu("SpawnItemsNow")]
    private void SpawnItemsNowContext()
    {
        Debug.Log($"RandomSurfaceSpawner: SpawnItemsNow context invoked on '{name}'");
        SpawnOnce();
    }

    [ContextMenu("ForceSpawnNow")]
    private void ForceSpawnNowContext()
    {
        Debug.Log($"RandomSurfaceSpawner: ForceSpawnNow context invoked on '{name}'");
        SpawnOnce();
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
