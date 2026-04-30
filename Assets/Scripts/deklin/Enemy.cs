using System.Collections;
using System.Linq;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum TargetType
    {
        Player,
        OpenDoor
    }

    public TargetType targetType;
    public NavMeshSurface surface;
    public Transform wanderTarget;
    public Transform player;

    public float distanceToNewWanderTarget = 2f;
    public LayerMask obstacleMask;
    public bool papadopoulos;
    public float doorRange = 5f;
    public LayerMask doorMask;
    public bool canOpenDoors;
    public bool canCloseDoors;
    private Transform currentTarget;

    private void Start()
    {
        NewWanderPosition();
    }

    private void Update()
    {
        if (papadopoulos == false)
        {
            if (targetType == TargetType.Player)
            {
                TryTrackTarget(player);
            }
            else if (targetType == TargetType.OpenDoor && door.openDoor != null)
            {
                TryTrackTarget(door.openDoor);
            }
        }
        else
        {
            currentTarget = wanderTarget;
        }

        if (currentTarget)
        {
            if (Vector3.Distance(transform.position, currentTarget.position) < distanceToNewWanderTarget)
            {
                NewWanderPosition();
            }

            GetComponent<NavMeshAgent>().SetDestination(currentTarget.transform.position);
        }


        if (canOpenDoors)
        {
            OpenNearbyDoors();
        }

        if (canCloseDoors)
        {
            CloseNearbyDoors();
        }

        if (papadopoulos == true)
        {
            followcamera();
        }
    }

    public void NewWanderPosition()
    {
        float x = Random.Range(surface.navMeshData.sourceBounds.min.x, surface.navMeshData.sourceBounds.max.x);
        float y = 3.072f;
        float z = Random.Range(surface.navMeshData.sourceBounds.min.z, surface.navMeshData.sourceBounds.max.z);

        if (NavMesh.SamplePosition(new Vector3(x, y, z), out NavMeshHit hit, 100.0f, NavMesh.AllAreas))
        {
            x = hit.position.x;
            y = hit.position.y;
            z = hit.position.z;
        }

        wanderTarget.transform.position = new Vector3(x, y, z);
    }

    private void TryTrackTarget(Transform desiredTarget)
    {
        // Check if the target is within line of sight using a raycast
        bool hitWall = Physics.Linecast(transform.position, desiredTarget.position, obstacleMask);

        // If the target is spotted, set the destination of the NavMeshAgent to the target's position
        if (hitWall)
        {
            // If the target was the player, set the wondertarget's position to the player's position
            if (currentTarget == desiredTarget)
            {
                wanderTarget.position = desiredTarget.position;
            }

            currentTarget = wanderTarget.transform;
        }
        else
        {
            currentTarget = desiredTarget;
        }
    }

    private door[] GetNearbyDoors()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, doorRange, doorMask);
        door[] doors = colliders.Select(collider => collider.GetComponent<door>()).ToArray();

        return doors;
    }

    public void OpenNearbyDoors()
    {
        foreach (door doorComponent in GetNearbyDoors())
        {
            if (doorComponent.state == door.doorstate.close)
            {
                doorComponent.open();
            }
        }
    }

    public void CloseNearbyDoors()
    {
        foreach (door doorComponent in GetNearbyDoors())
        {
            if (doorComponent.state == door.doorstate.open)
            {
                doorComponent.close();
            }
        }
    }

    public void followcamera()
    {
        if (currentTarget)
        {
            wanderTarget.position = FindAnyObjectByType<Camera>().transform.position + FindAnyObjectByType<Camera>().transform.forward * 5f;
            
        }
    }
    
}
