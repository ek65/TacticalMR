using System;
using System.Collections;
using System.Collections.Generic;
using OpenAI.Samples.Chat;
using UnityEngine;
using Utilities.Audio;

public class KeyboardInput : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField] public float moveSpeed = 4f;
    public ExitScenario exitScenario;
    private Rigidbody rb;
    private ChatBehaviour chatBehaviour;
    
    private TimelineManager timelineManager;
    private JSONToLLM jsonToLLM;
    // public GameObject userCircle;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponent<ChatBehaviour>();
    }
    void Update()
    {
      
        if (Input.GetKeyDown(KeyCode.E))
        {
            exitScenario.EndScenario();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (timelineManager.Paused)
            {
                timelineManager.Unpause();
            }
            else
            {
                timelineManager.Pause();
            }
        }
        
        // Check if the T key is held down
        if (Input.GetKeyDown(KeyCode.V))
        {
            // Start recording if not already recording
            if (!RecordingManager.IsRecording)
            {
                chatBehaviour.ToggleRecording();
            }
        }
        else if (Input.GetKeyUp(KeyCode.V))
        {
            // Stop recording if currently recording
            if (RecordingManager.IsRecording)
            {
                chatBehaviour.ToggleRecording();
            }
        }

        if (timelineManager.Paused)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (timelineManager.advancing)
                {
                    // can't rewind while advancing
                }
                else
                {
                    timelineManager.rewinding = !timelineManager.rewinding;
                }
            }
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (timelineManager.rewinding)
                {
                    // can't advance while rewinding
                }
                else
                {
                    timelineManager.advancing = !timelineManager.advancing;
                }
            }
            
            if (Input.GetKeyDown(KeyCode.Space) && !timelineManager.rewinding && !timelineManager.advancing)
            {
                jsonToLLM.PopulateSceneObjects();
                jsonToLLM.WriteJSON();
            }
        }
        
        
        
        
        
    }

    void FixedUpdate()
    {
        if (timelineManager.Paused)
        {
            return;
        }
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        //Vector3 moveDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;

        //Vector3 velocity = moveDirection * moveSpeed;
        //rb.velocity = velocity;

        Vector3 forwardDirection = transform.forward;
        Vector3 movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized * moveSpeed;

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }
}
