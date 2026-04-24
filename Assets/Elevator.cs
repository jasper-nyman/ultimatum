using UnityEngine;

public class ElevatorI : MonoBehaviour
{
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.transform.position = new Vector3(13.5f, 6f, 7.75f);
        }
    }
}