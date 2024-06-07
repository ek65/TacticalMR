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
                if (!chatBehaviour.isRecording)
                {
                    StartCoroutine(ToggleCoroutine());
                }
                
            }
            else
            {
                timelineManager.Pause();
                chatBehaviour.ToggleRecording(); // Stop recording
                Debug.Log("Chat behaviour is:" + chatBehaviour.isRecording);
                if (!chatBehaviour.isRecording)
                {
                    StartCoroutine(ToggleCoroutine());
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
           
            chatBehaviour.ToggleRecording();
        }
        IEnumerator ToggleCoroutine()
        {
            //Print the time of when the function is first called.
            Debug.Log("Started Coroutine at timestamp : " + Time.time);

            //yield on a new YieldInstruction that waits for 5 seconds.
            yield return new WaitForSeconds(2);
            chatBehaviour.ToggleRecording(); // Start recording

            //After we have waited 5 seconds print the time again.
            Debug.Log("Finished Coroutine at timestamp : " + Time.time);
        }
       

        if (Input.GetKeyUp(KeyCode.O))
        {
            chatBehaviour.ToggleRecording(); // Stop recording again
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
