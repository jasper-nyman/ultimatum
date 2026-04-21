using System.Collections.Generic;
using UnityEngine;

public class walkpartical : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!(GetComponent<PlayerVariables>().isMoving = true))
        {
            StartCoroutine(walkparticalspawn());
        }
    }
    IEnumerator<WaitForSeconds> walkparticalspawn()
    {
        while (true)
        {
            if (GetComponent<PlayerVariables>().isMoving)
            {
                GetComponent<ParticleSystem>().Emit(5);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
