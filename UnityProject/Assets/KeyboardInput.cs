using System.Collections;
using System.Collections.Generic;
using OpenAI.Samples.Chat;
using UnityEngine;
using Utilities.Audio;
using Utilities.Encoding.Wav;

public class KeyboardInput : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 4f;
    public ExitScenario exitScenario;
    private Rigidbody rb;
    private ChatBehaviour chatBehaviour;

    private TimelineManager timelineManager;
    private JSONToLLM jsonToLLM;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponent<ChatBehaviour>();
        Debug.Log("KeyboardInput script initialized");
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
                chatBehaviour.ToggleRecording(); // Start recording again
            }
            else
            {
                timelineManager.Pause();
                chatBehaviour.ToggleRecording(); // Stop recording
            }
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
           
            chatBehaviour.ToggleRecording();
        }
       

        if (Input.GetKeyUp(KeyCode.O))
        {
            jsonToLLM.CreateJSONString();
            chatBehaviour.SubmitCombinedInput();
            jsonToLLM.WriteJSON();
            Debug.Log($"Processed {chatBehaviour.userInput} after pausing");
        }

        if (timelineManager.Paused)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (!timelineManager.advancing)
                {
                    timelineManager.rewinding = !timelineManager.rewinding;
                }
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                if (!timelineManager.rewinding)
                {
                    timelineManager.advancing = !timelineManager.advancing;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space) && !timelineManager.rewinding && !timelineManager.advancing)
            {
                jsonToLLM.PopulateSceneObjects();
                jsonToLLM.CreateJSONString();
                chatBehaviour.SubmitCombinedInput();
                jsonToLLM.WriteJSON();
                Debug.Log("Sent input");
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

        Vector3 forwardDirection = transform.forward;
        Vector3 movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized *
                           moveSpeed;

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }
}
