using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;

public class ExitScenario : MonoBehaviour
{
    public OVRInput.Button button;
    public OVRInput.Button trigger;
    public OVRInput.Controller controllerRight;
    public OVRInput.Controller controllerLeft;
    public OVRInput.Button buttonX;
    public OVRInput.Button buttonY;
    public bool recordingActive = false;

    public bool endScenario;

    public GameObject playerHuman;
    public GameObject bodyTrackingTarget;

    private ReplayRecordOperation recordOp;
    private ReplayStorage storage;

    // Start is called before the first frame update
    void Start()
    {
        endScenario = false;
        storage = new ReplayMemoryStorage("MyReplay");
    }

    // Update is called once per frame
    void Update()
    {
        if(OVRInput.GetDown(button, controllerRight))
        {
            Debug.LogError("button pressed");
            GameObject.FindGameObjectWithTag("goal").GetComponent<ParticleSystem>().Stop();
            endScenario = true;
            bodyTrackingTarget.transform.parent = playerHuman.transform;
            bodyTrackingTarget.GetComponent<OVRBody>().enabled = true;
        }
        // else
        // {
        //     endScenario = false;
        // }
        if(OVRInput.GetDown(buttonX, controllerLeft) && !recordingActive)
        {
            recordingActive = true;
            recordOp = ReplayManager.BeginRecording(storage);
        } else if (OVRInput.GetDown(buttonX, controllerLeft) && recordingActive)
        {
            recordingActive = false;
            recordOp.StopRecording();
        }
        if(OVRInput.GetDown(buttonY, controllerLeft) && !recordingActive)
        {
            bodyTrackingTarget.GetComponent<OVRBody>().enabled = false;
            // detach from parent
            bodyTrackingTarget.transform.parent = null;
            ReplayPlaybackOperation playbackOp = ReplayManager.BeginPlayback(storage);
            
        }
        
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

}