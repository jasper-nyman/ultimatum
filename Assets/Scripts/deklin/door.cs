using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class door : MonoBehaviour, IInteractable
{
    public enum doorstate
    {
        open,
        close
    }

    public Animator anim;
    public doorstate state;
    public AudioClip opennoise, closenoise;

    public void Interact()
    {
        if (state == doorstate.close)
        {
            open();
        }
        else
        {
            close();
        }

        FindFirstObjectByType<NavMeshSurface>().BuildNavMesh();
    }

    public void open()
    {
        anim.SetBool("isopen", true);
        state = doorstate.open;
        GetComponent<AudioSource>().PlayOneShot(opennoise);
    }

    public void close()
    {
        anim.SetBool("isopen", false);
        state = doorstate.close;
        GetComponent<AudioSource>().PlayOneShot(closenoise);
    }
}
