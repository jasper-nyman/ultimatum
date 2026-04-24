using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class CameraBobbing : MonoBehaviour
{
    public float bobbingIntensity;
    private float xBob;
    private float yBob;
    private float zBob;
    private float xBobReal;
    private float yBobReal;
    private float zBobReal;
    private float time;

    //public GameObject debugText;
    //private TextMeshProUGUI textMesh;

    private void Start()
    {
        xBobReal = 0;
        yBobReal = 0;
        zBobReal = 0;
        //textMesh = debugText.GetComponent<TextMeshProUGUI>();

        time = 0;
        Bob();
    }
    void Update()
    {
        time += Time.deltaTime;

        if (time >= 1)
        {
            time = 0;
            Bob();
        }

        xBobReal = Mathf.MoveTowards(xBobReal, xBob, bobbingIntensity * Time.deltaTime);
        yBobReal = Mathf.MoveTowards(yBobReal, yBob, bobbingIntensity * Time.deltaTime);
        zBobReal = Mathf.MoveTowards(zBobReal, zBob, bobbingIntensity * Time.deltaTime);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(-22.5f + xBobReal, yBobReal, zBobReal / 2), Time.deltaTime);
        //textMesh.text = $"xBob: {xBob}\nyBob: {yBob}\nzBob: {zBob}\ntime: {time}";
    }

    private void Bob()
    {
        float chanceX = Random.Range(0f, 1f);
        float chanceY = Random.Range(0f, 1f);
        float chanceZ = Random.Range(0f, 1f);
        if (chanceX < 0.5f)
        {
            xBob = Random.Range(-bobbingIntensity, -bobbingIntensity / 2);
        }
        else
        {
            xBob = Random.Range(bobbingIntensity, bobbingIntensity / 2);
        }

        if (chanceY < 0.5f)
        {
            yBob = Random.Range(-bobbingIntensity, -bobbingIntensity / 2);
        }
        else
        {
            yBob = Random.Range(bobbingIntensity, bobbingIntensity / 2);
        }

        if (chanceZ < 0.5f)
        {
            zBob = Random.Range(-bobbingIntensity, -bobbingIntensity / 2);
        }
        else
        {
            zBob = Random.Range(bobbingIntensity, bobbingIntensity / 2);
        }
    }
}
