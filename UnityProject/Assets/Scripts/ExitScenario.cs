using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitScenario : MonoBehaviour
{
    public OVRInput.Button button;
    public OVRInput.Button trigger;
    public OVRInput.Controller controllerRight;

    public bool endScenario;

    // Start is called before the first frame update
    void Start()
    {
        endScenario = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(OVRInput.GetDown(button, controllerRight))
        {
            Debug.LogError("button pressed");
            GameObject.FindGameObjectWithTag("goal").GetComponent<ParticleSystem>().Stop();
            endScenario = true;
        }
        // else
        // {
        //     endScenario = false;
        // }

    }

}