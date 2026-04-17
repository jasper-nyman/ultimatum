using UnityEngine;

public class EnemyLookAt : MonoBehaviour
{
    private Transform player;

    private void Start()
    {
        player = GameObject.Find("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        // Look at the player
        Vector3 direction = player.position - transform.position;
        direction.y = -180; // Keep the enemy upright
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }
    }
}
