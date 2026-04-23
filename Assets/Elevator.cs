using UnityEngine;

public class Elevator : MonoBehaviour
{
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.transform.position = new Vector3(50f, 6f, 7.75f);
        }
    }
}