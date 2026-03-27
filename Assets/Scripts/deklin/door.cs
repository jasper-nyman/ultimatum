using UnityEngine;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

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
