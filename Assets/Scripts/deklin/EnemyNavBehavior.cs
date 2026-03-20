using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;

public class EnemyNavBehavior : MonoBehaviour
{
    // The range within which the enemy can spot the target using a raycast
    public int RayCastRange = 50;
    Transform target;
    bool targetSpotted = false;
    public LayerMask obstacleMask;
    public bool searchingForTarget = false;
    Vector3 wonderingPosition;
    public Transform player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find the player GameObject in the scene and assign it to the target variable
        
       
    }

    // Update is called once per frame
    void Update()
    {
        CanSeePlayer();

        if (Vector3.Distance(transform.position, target.position) == 0)
        {
            wonderingPosition = new Vector3
            (
                x: UnityEngine.Random.Range(0, 60),
                y: 0,
                z: UnityEngine.Random.Range(0, 30)
            );
            GameObject.Find("wondertarget").transform.position = wonderingPosition;

        }
    }

    void CanSeePlayer()
    {
        // Check if the target is within line of sight using a raycast
        targetSpotted = Physics.Linecast(transform.position, player.transform.position, obstacleMask);

        // If the target is spotted, set the destination of the NavMeshAgent to the target's position
        if (targetSpotted)
        {
            target = GameObject.Find("Player").transform;
            GetComponent<NavMeshAgent>().SetDestination(target.transform.position);
        }
        else
        {
            target = GameObject.Find("wondertarget").transform;
        }
        //if (target == null)
        //{
        //    wonderi = true;
        //}
       
    }
    
}
