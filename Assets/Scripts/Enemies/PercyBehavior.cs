using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PercyBehavior : MonoBehaviour
{
    private NavMeshAgent agent;
    private GameObject player;
    private Transform playerTransform;
    private PlayerVariables pvar;
    private bool caughtPlayer;

    private AudioSource aSource;
    public AudioClip jumpscareSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.Find("Player");
        playerTransform = player.transform;
        pvar = player.GetComponent<PlayerVariables>();
        aSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            agent.isStopped = true; // Stop moving towards the player
            pvar.isActive = false; // Disable player movement and look
            pvar.canMove = false;
            pvar.canLook = false;
            pvar.canCrouch = false;
            caughtPlayer = true;
            aSource.PlayOneShot(jumpscareSound); // Play the jumpscare sound
            Invoke(nameof(Lose), 1f); // Delay the lose condition to allow the player to see Percy
            Debug.Log("Percy caught Player");
        }
    }

    private void Update()
    {
        if (caughtPlayer)
        {
            CameraController cc = Camera.main.GetComponent<CameraController>();
            // force camera to look at Percy
            cc.rotation = Quaternion.LookRotation((transform.position + new Vector3(0, 0.9f, 0)) - Camera.main.transform.position).eulerAngles;
            float shakeIntensity = 0.5f;
            float ranX = Random.Range(-shakeIntensity, shakeIntensity);
            float ranY = Random.Range(-shakeIntensity, shakeIntensity);
            float ranZ = Random.Range(-shakeIntensity, shakeIntensity);
            cc.rotation += new Vector3(ranX, ranY, ranZ); // Add random shake to the camera
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, 45, Time.deltaTime * 10);
        }
    }

    private void Lose()
    {
        // Implement lose condition (e.g., show game over screen, reset level, etc.)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Reload the current scene
        Debug.Log("Player loses!");
    }
}
