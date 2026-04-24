using UnityEngine;

public class Elevator4AI : MonoBehaviour
{
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Percy"))
        {
            collision.transform.position = new Vector3(13.5f, 6f, 7.75f);
        }
    }
}