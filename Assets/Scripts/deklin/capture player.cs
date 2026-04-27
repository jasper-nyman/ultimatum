using System.Collections;
using UnityEngine;

public class captureplayer : MonoBehaviour
{
    public Transform player;
    public float captureDistance = 2f;
    public bool canCapturePlayer;
    public bool capturedPlayer;
    public bool jumpPlayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (canCapturePlayer && !capturedPlayer && Vector3.Distance(transform.position, player.position) <= captureDistance)
        {
            StartCoroutine(captureingplayer());
        }
    }

    IEnumerator captureingplayer()
    {
        Debug.Log("Captured player");

        capturedPlayer = true;
        FindFirstObjectByType<PlayerVariables>().canMove = false;
        GetComponent<Enemy>().NewWanderPosition();
        jumpPlayer = true;

        yield return new WaitForSeconds(5);
        
        jumpPlayer = false;
        GetComponent<Enemy>().CloseNearbyDoors();
        FindFirstObjectByType<PlayerVariables>().canMove = true;
        capturedPlayer = false;

        Debug.Log("Released player");
    }
}
