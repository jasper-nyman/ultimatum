using UnityEngine;
using UnityEngine.AI;

public class ai : MonoBehaviour
{
    public bool isfindable = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isfindable == true)
        {
            GetComponent<NavMeshAgent>().SetDestination(GameObject.Find("Player").transform.position);
        }
    }
}
