using UnityEngine;
using UnityEngine.AI;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    { /*this allows the enemy to follow the player*/
        GetComponent<NavMeshAgent>().SetDestination(GameObject.Find("Player").transform.position);
    }
}
