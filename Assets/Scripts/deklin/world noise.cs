using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

public class worldnoise : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<door>().GetComponent<AudioSource>().isPlaying== true )
        {
            if (GameObject.FindWithTag("Percy"))
            {
                GameObject.FindWithTag("Percy").GetComponent<Enemy>().wanderTarget.transform.position = transform.position;
            }
        }
    }
}
