using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class objectivecontrol : MonoBehaviour, IInteractable
{
    public NavMeshSurface surface;
    public Transform wanderTarget;
    public bool objectivecapture;
    public float objectiverange = 5f;

    public static int objectivecounter = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void Interact()
    {
            NewWanderPosition();
        objectivecounter++;
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
}
