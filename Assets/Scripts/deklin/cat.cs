using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using static door;
using static UnityEngine.GraphicsBuffer;

public class cat : MonoBehaviour
{
    public int RayCastRange = 50;
    public float doorRange = 5f;
    Transform target;
    bool hitwall = false;
    public LayerMask obstacleMask;
    public LayerMask doormask;
    public bool searchingForTarget = false;
    Vector3 wonderingPosition;
    public Transform opendoor;
    public Transform wonderTarget;
    public NavMeshSurface surface;
    private object position;

    void Start()
    {
        newposition();
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<door>().state == doorstate.open)
        {
            wonderingPosition = wonderTarget.transform.position;

        }
        else
        {


        }

            canSeeOpenDoor();

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

    void newposition()
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

        wonderingPosition = new Vector3(x, y, z);
        wonderTarget.transform.position = wonderingPosition;
    }

    void canSeeOpenDoor()
    {
        // If the target is spotted, set the destination of the NavMeshAgent to the target's position
        if (hitwall)
        {
            // If the target was the player, set the wondertarget's position to the player's position
            if (target == opendoor)
            {
                wonderTarget.transform.position = opendoor.transform.position;
                //wonderTarget.navmesh direction = (wonderTarget.transform.position - transform.position).normalized;
            }


            target = wonderTarget.transform;
        }
        else
        {
            target = opendoor.transform;
        }

        GetComponent<NavMeshAgent>().SetDestination(target.transform.position);
    }
    IEnumerator<WaitForSeconds> CapturedPlayer()
    {
        if (Vector3.Distance(transform.position, GameObject.FindWithTag("Player").transform.position) < 2)
        {
            GetComponent<PlayerVariables>().canMove = false;
            GetComponent<EnemyNavBehavior>().wonderTarget.position = transform.position;
            yield return new WaitForSeconds(5);
            GetComponent<PlayerVariables>().canMove = true;
        }
    }
}
