using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;

public class ExitScenario : MonoBehaviour
{
    // public OVRInput.Button button;
    public OVRInput.Button indexTrigger;
    public OVRInput.Controller controllerRight;
    public OVRInput.Controller controllerLeft;
    public OVRInput.Button buttonX;
    public OVRInput.Button buttonY;
    // public bool recordingActive = false;
    
    private KeyboardInput keyboardInput;

    public bool endScenario;

    // public GameObject playerHuman;
    // public GameObject bodyTrackingTarget;
    //
    // private ReplayRecordOperation recordOp;
    // private ReplayStorage storage;

    // Start is called before the first frame update
    void Start()
    {
        endScenario = false;
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        // storage = new ReplayMemoryStorage("MyReplay");
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(buttonX, controllerRight))
        {
            EndScenario();
        }
        
        if (OVRInput.GetDown(buttonY, controllerRight))
        {
            Debug.LogError("I HIT THE PAUSE BUTTON");
            keyboardInput.HandlePause();
        }
        
        // TODO: enable this for recording & playback, put this in another class and call from Keyboard Input
        // if(OVRInput.GetDown(buttonX, controllerLeft) && !recordingActive)
        // {
        //     recordingActive = true;
        //     recordOp = ReplayManager.BeginRecording(storage);
        // } else if (OVRInput.GetDown(buttonX, controllerLeft) && recordingActive)
        // {
        //     recordingActive = false;
        //     recordOp.StopRecording();
        // }
        // if(OVRInput.GetDown(buttonY, controllerLeft) && !recordingActive)
        // {
        //     bodyTrackingTarget.GetComponent<OVRBody>().enabled = false;
        //     // detach from parent
        //     bodyTrackingTarget.transform.parent = null;
        //     ReplayPlaybackOperation playbackOp = ReplayManager.BeginPlayback(storage);
        //     
        // }
        
        // if (Input.GetKeyDown("space") && !recordingActive)
        // {
        //     recordingActive = true;
        //     recordOp = ReplayManager.BeginRecording(storage);
        //     
        // } else if (Input.GetKeyDown("space") && recordingActive)
        // {
        //     recordingActive = false;
        //     recordOp.StopRecording();
        // }
        //
        // if (Input.GetKeyDown(KeyCode.P) && !recordingActive)
        // {
        //     bodyTrackingTarget.GetComponent<OVRBody>().enabled = false;
        //     // detach from parent
        //     bodyTrackingTarget.transform.parent = null;
        //     ReplayPlaybackOperation playbackOp = ReplayManager.BeginPlayback(storage);
        // }

    }
    
    // Sets the endScenario flag to true. Scenic reads this and will terminate, and generate a new simulation
    // human fades and moves position instead of spawning a new human; in InstantiateScenicObject.cs
    // sends endScenario to JSONStatusMaker.cs
    public void EndScenario()
    {
        GameObject.FindGameObjectWithTag("goal").GetComponent<ParticleSystem>().Stop();
        endScenario = true;
        // bodyTrackingTarget.transform.parent = playerHuman.transform;
        // bodyTrackingTarget.GetComponent<OVRBody>().enabled = true;
    }

}