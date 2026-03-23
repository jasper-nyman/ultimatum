using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;

public class EnemyNavBehavior : MonoBehaviour
{
    // The range within which the enemy can spot the target using a raycast
    public int RayCastRange = 50;
    Transform target;
    bool hitwall = false;
    public LayerMask obstacleMask;
    public bool searchingForTarget = false;
    Vector3 wonderingPosition;
    public Transform player;
    public Transform wonderTarget;
    public NavMeshSurface surface;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
        
       
    }

    // Update is called once per frame
    void Update()
    {
        CanSeePlayer();

        if (Vector3.Distance(transform.position, target.position) < 2)
        {
            float x = Random.Range(surface.navMeshData.sourceBounds.min.x, surface.navMeshData.sourceBounds.max.x);
            float y = Random.Range(surface.navMeshData.sourceBounds.min.y, surface.navMeshData.sourceBounds.max.y);
            float z = Random.Range(surface.navMeshData.sourceBounds.min.z, surface.navMeshData.sourceBounds.max.z);

            wonderingPosition = new Vector3(x, y, z);
            wonderTarget.transform.position = wonderingPosition;

        }
    }

    void CanSeePlayer()
    {
        // Check if the target is within line of sight using a raycast
        hitwall = Physics.Linecast(transform.position, player.transform.position, obstacleMask);

        // If the target is spotted, set the destination of the NavMeshAgent to the target's position
        if (hitwall)
        {
            target = wonderTarget.transform;
        }
        else
        {
            target = player.transform;
        }

        GetComponent<NavMeshAgent>().SetDestination(target.transform.position);
    }
    
}
