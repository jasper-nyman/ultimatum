using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;
using static door;

public class EnemyNavBehavior : MonoBehaviour
{
    // The range within which the enemy can spot the target using a raycast
    public int RayCastRange = 50;
    public float doorRange = 5f;
    Transform target;
    bool hitwall = false;
    public LayerMask obstacleMask;
    public LayerMask doormask;
    public bool searchingForTarget = false;
    Vector3 wonderingPosition;
    public Transform player;
    public Transform wonderTarget;
    public NavMeshSurface surface;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       newposition();
    }

    // Update is called once per frame
    void Update()
    {
        CanSeePlayer();

        if (Vector3.Distance(transform.position, target.position) < 2)
        {
            newposition();
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, doorRange, doormask);
        foreach (Collider collider in colliders)
        {
            
            if (collider.TryGetComponent(out door doorComponent))
            {
                if (doorComponent.state == doorstate.close)
                {
                    doorComponent.open();
                }

            }
        }
    }

    public void newposition()
    {
        float x = Random.Range(surface.navMeshData.sourceBounds.min.x, surface.navMeshData.sourceBounds.max.x);
        float y = 3.072f;
        float z = Random.Range(surface.navMeshData.sourceBounds.min.z, surface.navMeshData.sourceBounds.max.z);

        if(NavMesh.SamplePosition(new Vector3(x, y, z), out NavMeshHit hit, 100.0f, NavMesh.AllAreas))
        {
            x = hit.position.x;
            y = hit.position.y;
            z = hit.position.z;
        }

        wonderingPosition = new Vector3(x, y, z);
        wonderTarget.transform.position = wonderingPosition;
    }

    void CanSeePlayer()
    {
        // Check if the target is within line of sight using a raycast
        hitwall = Physics.Linecast(transform.position, player.transform.position, obstacleMask);

        // If the target is spotted, set the destination of the NavMeshAgent to the target's position
        if (hitwall)
        {
            // If the target was the player, set the wondertarget's position to the player's position
            if (target == player)
            {
                wonderTarget.transform.position = player.transform.position;
                //wonderTarget.navmesh direction = (wonderTarget.transform.position - transform.position).normalized;
            }

            
                target = wonderTarget.transform;
        }
        else
        {
            target = player.transform;
        }

        GetComponent<NavMeshAgent>().SetDestination(target.transform.position);
    }
}
