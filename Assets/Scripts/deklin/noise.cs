using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class noise : MonoBehaviour
{
    public float noiserange = 30f;

    void Update()
    {
        if (GetComponent<AudioSource>().isPlaying == true)
        {
            float distance = GetDistance(transform.position, GameObject.FindWithTag("Percy").transform.position);

            if (distance < noiserange)
            {
                GameObject.FindWithTag("Percy").GetComponent<Enemy>().wanderTarget.transform.position = transform.position;
            }
        }
    }

    public float GetDistance(Vector3 positionA,Vector3 positionB) 
    { 
        float A = positionA.x - positionB.x;
        float B = positionA.y - positionB.y;
        float C = positionA.z - positionB.z;
        float Asquared = A * A;
        float Bsquared = B * B;
        float Csquared = C * C;
        float suareroot = Mathf.Sqrt(Asquared + Bsquared + Csquared);

        return suareroot;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, noiserange);
    }
}
