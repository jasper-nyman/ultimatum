using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class door : MonoBehaviour, IInteractable
{
    public static Transform openDoor;

    public enum doorstate
    {
        open,
        close
    }
    public float timerset = 0.5f;
    public float interacttimer = 0.5f;
    public bool alreadyinteracted = true;
    public Animator anim;
    public doorstate state;
    public AudioClip openNoise, closeNoise;


    public void Update()
    {
        if (interacttimer > 0)
        {
            interacttimer -= Time.deltaTime;
        }
    }

    public void Interact()
    {
        if (alreadyinteracted==true && interacttimer <= 0)
        {


            if (state == doorstate.close)
            {
                open();
                alreadyinteracted = true;
                StartCoroutine(ResetInteraction());
            }
            else
            {
                close();
                alreadyinteracted = true;
                StartCoroutine(ResetInteraction());
            }
        }
        
    }

    void rebakeNavMesh()
    {
        FindFirstObjectByType<NavMeshSurface>().UpdateNavMesh(FindFirstObjectByType<NavMeshSurface>().navMeshData);
        Debug.Log("rebaked navmesh");
    }

    public void open()
    {
        anim.SetBool("isopen", true);
        state = doorstate.open;
        GetComponent<AudioSource>().PlayOneShot(openNoise);
        Invoke(nameof(rebakeNavMesh), 1f);

        openDoor = transform;
        interacttimer = timerset;
    }

    public void close()
    {
        anim.SetBool("isopen", false);
        state = doorstate.close;
        GetComponent<AudioSource>().PlayOneShot(closeNoise);
        Invoke(nameof(rebakeNavMesh), 1f);
        interacttimer = timerset;
    }
    IEnumerator ResetInteraction()
    {
        
        alreadyinteracted = false;
        yield return new WaitForSeconds(1f);
        alreadyinteracted = true;
    }
}
