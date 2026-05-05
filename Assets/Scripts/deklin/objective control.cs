using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class objectivecontrol : MonoBehaviour, IInteractable
{
    public UnityEvent<string> onObjectiveCaptured;
    public NavMeshSurface surface;
    public bool objectivecapture;
    public float objectiverange = 5f;

    public static int objectivecounter = 0;
    public bool objectivecomplete;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (objectivecomplete == true)
        {
            SceneManager.LoadScene("Win");
        }
        if (objectivecounter == 10)
        {
            objectivecomplete = true;
        }
    }
    public void Interact()
    {
        if (objectivecomplete == false)
        {
        NewWanderPosition();
        objectivecounter++;
        onObjectiveCaptured.Invoke(objectivecounter.ToString());
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

        transform.parent.position = new Vector3(x, y, z);
    }
}
