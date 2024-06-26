using System.Collections;
using System.Collections.Generic;
using OpenAI.Samples.Chat;
using TMPro;
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
    public TextMeshProUGUI countdownText;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        countdownText = GameObject.FindGameObjectWithTag("countdown").GetComponent<TextMeshProUGUI>();
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
        }
        else
        {
            timelineManager.Pause();
            StartCoroutine(Countdown());
            chatBehaviour.ToggleRecording(); // Stop recording
            Debug.Log("Chat behaviour is:" + chatBehaviour.isRecording);
            if (!chatBehaviour.isRecording)
            {
                StartCoroutine(ToggleRecordingCoroutine());
            }
        }
    }
    if (Input.GetKeyDown(KeyCode.I))
    {
        if (chatBehaviour.isRecording)
        {
            chatBehaviour.ToggleRecording();
            StartCoroutine(JSONCoroutine());
        }
        else
        {
            chatBehaviour.ToggleRecording();
        }
    }
    
    // Coroutines
    IEnumerator ToggleRecordingCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(1);
        chatBehaviour.ToggleRecording(); // Start recording
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }
    
    IEnumerator JSONCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(2);
        chatBehaviour.SubmitCombinedInput("explain");
        // Uncomment this line to populate the scene objects
        // jsonToLLM.PopulateSegment();
        yield return new WaitForSeconds(1);
        StartCoroutine(ConditionCoroutine());
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }
    IEnumerator ConditionCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(2);
        chatBehaviour.SubmitCombinedInput("condition");
        yield return new WaitForSeconds(1);
        StartCoroutine(ActionCoroutine());
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }
    IEnumerator ActionCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(2);
        chatBehaviour.SubmitCombinedInput("action");
        yield return new WaitForSeconds(1);
        StartCoroutine(FileCoroutine());
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }
    IEnumerator FileCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(1);
        jsonToLLM.WriteFile();
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
        
        // not being used rn
        if (Input.GetKeyDown(KeyCode.Space) && !timelineManager.rewinding && !timelineManager.advancing)
        {
            jsonToLLM.PopulateSceneObjects();
            jsonToLLM.CreateJSONString();
            chatBehaviour.SubmitCombinedInput("condition"); 
            jsonToLLM.WriteFile();
            Debug.Log("Sent input");
        }
    }
}
    
    private IEnumerator Countdown()
    {
        countdownText.gameObject.SetActive(true);
        for (int i = 5; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1);
        }
        
        countdownText.text = "GO";
        yield return new WaitForSeconds(1);
        countdownText.gameObject.SetActive(false);
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
