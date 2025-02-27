using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using OpenAI.Samples.Chat;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Whisper.Samples;
using Vector3 = UnityEngine.Vector3;

public class KeyboardInput : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 4f;
    
    public ExitScenario exitScenario;
    public Rigidbody rb;
    private ChatBehaviour chatBehaviour;
    private StreamingSampleMic streamingSampleMic;

    public TimelineManager timelineManager;
    // private RecorderManager recorderManager;
    private JSONToLLM jsonToLLM;
    public TextMeshProUGUI countdownText;
    // private SynthConnect synthConnect;
    private JSONDirectory jsonDirectory;
    public GameObject saveDemoCanvas;
    public bool restarting = false;
    public Dictionary<int, object> annotation = new Dictionary<int, object>();
    public Dictionary<int, string> annotationDescriptions = new Dictionary<int, string>();
    private Dictionary<GameObject, int> objectToKey = new Dictionary<GameObject, int>();
    public Dictionary<int, float> annotationTimes = new Dictionary<int, float>();
    public int clickOrder = 0;
    private bool isAnnotationMode = false;
    private bool isReferenceMode = false;
    private bool isPositionMode = false;
    public string explanation;
    
    public bool segmentStarted = false; 
    public bool activationConditionMet = false; 

    public Vector3 movement;
    private ActionAPI actionAPI;
    public bool canClick = true; // Add a boolean to track if clicking is allowed

    void Start()
    {
        actionAPI = GameObject.FindGameObjectWithTag("player").GetComponent<ActionAPI>();
        // Initialize the necessary components and references at the start of the scene
        // rb = GetComponent<Rigidbody>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        // recorderManager = GameObject.FindGameObjectWithTag("RecorderManager").GetComponent<RecorderManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        countdownText = GameObject.FindGameObjectWithTag("countdown").GetComponent<TextMeshProUGUI>();
        chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponent<ChatBehaviour>();
        streamingSampleMic = GameObject.FindGameObjectWithTag("stream").GetComponent<StreamingSampleMic>();
        // synthConnect = GameObject.FindGameObjectWithTag("connect").GetComponent<SynthConnect>();
        jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        Debug.Log("KeyboardInput script initialized");
        StartCoroutine(CallHandleSegmentAfterDelay());
    }
    private IEnumerator CallHandleSegmentAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        HandleSegment();
    }
    // Handles user input and triggers different actions based on key presses
    void Update()
    {
        // Check and assign the ExitScenario component if not already assigned
        if (exitScenario == null && GameObject.FindGameObjectWithTag("human") != null)
        {
            exitScenario = GameObject.FindGameObjectWithTag("human").GetComponent<ExitScenario>();
        }
        
        if (Input.GetKeyDown(KeyCode.N))
        {
            Vector3 newPosition = actionAPI.transform.position + actionAPI.transform.forward * 2f;

            actionAPI.FactoryMoveToPos(newPosition, 2f, false);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            Vector3 newPosition = new Vector3(10f,0f,0f);
            // Call FactoryMoveToPos with the new position
            actionAPI.PutDown(newPosition);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            Vector3 newPosition = new Vector3(10f,0f,0f);
            // Call FactoryMoveToPos with the new position
            actionAPI.PickUp(newPosition);
        }
        // End the scenario when the 'E' key is pressed
        if (Input.GetKeyDown(KeyCode.E) && gameObject.CompareTag("keyboard"))
        {
            // exitScenario.EndScenario();
            HandleRestart();
        }
        
        if (Input.GetKeyDown(KeyCode.B) && gameObject.CompareTag("keyboard"))
        {
            HandleSegment();
        }
        
        // Pause the scenario and start recording speech when the 'P' key is pressed
        if (Input.GetKeyDown(KeyCode.P) && gameObject.CompareTag("keyboard"))
        {
            HandlePause();
        }
        
        // Enable annotation or position marking with mouse clicks
        if (canClick)
        {
            if (isAnnotationMode && Input.GetMouseButtonDown(0))
            {
                StartCoroutine(HandleClickWithDelay(HandleAnnotationMode));
            }

            if (isPositionMode && Input.GetMouseButtonDown(0))
            {
                StartCoroutine(HandleClickWithDelay(HandlePositionMode));
            }
        }
    
        // Debugging: Print current object mappings when 'K' key is pressed
        if (Input.GetKeyDown(KeyCode.K))
        {
            foreach (var annotation in annotation)
            {
                if (annotation.Value is GameObject gameObject)
                {
                    Debug.Log($"Key: {annotation.Key}, Object: {gameObject.name}");
                }
                else if (annotation.Value is Vector3 vector)
                {
                    Debug.Log($"Key: {annotation.Key}, Reference Vector: {vector}");
                }
            }
        }
    }
    
    // Resets JSON data of the current segment to prepare for the next segment
    private void ResetJsonData()
    {
        // if (recorderManager.RecorderController.IsRecording())
        // {
        //     recorderManager.StopRecording();
        // }
        annotation.Clear();
        annotationDescriptions.Clear();
        objectToKey.Clear();
        annotationTimes.Clear();
        clickOrder = 0;
        streamingSampleMic.ResetTranscriptionData();
        jsonToLLM.ResetSegmentData();
    }

    // Handles the position click functionality and marks position-based annotations
    public void HandlePositionClick()
    {
        StartCoroutine(HandleClickWithDelay(HandlePositionMode));
    }
    
    // Handles the annotation click functionality and marks object-based annotations
    public void HandleAnnotationClick()
    {
        StartCoroutine(HandleClickWithDelay(HandleAnnotationMode));
    }

    // HandlePause() should only pause or unpause, segment handling now done in HandleSegment()
    public void HandlePause()
    {
        if (timelineManager.Paused)
        {
            timelineManager.Unpause();
            
            activationConditionMet = true; 
            
            if (segmentStarted && activationConditionMet && !jsonToLLM.isLogging)
            {
                jsonToLLM.isLogging = true;
            }
        }
        else
        {
            timelineManager.Pause();
        }
    }

    public void HandleRestart()
    {
        restarting = true;
        // make sure segment recordings are stopped before restarting
        StopSegment();
        
        // if scene is not paused, pause it
        if (!timelineManager.Paused)
        {
            timelineManager.Pause();
        }

        canClick = false;
        
        // enable button UI for the user to confirm the restart
        saveDemoCanvas.SetActive(true);
    }

    // When unpaused, HandleSegment() should pause the scenario and start/stop recording a segment and set isRecordingSegment to true/false
    // When paused, and isRecordingSegment is true, HandleSegment() should end recording the segment and set isRecordingSegment to false (SHOULD NOT UNPAUSE)
    // When paused, and isRecordingSegment is false, HandleSegment() should start recording the segment and set isRecordingSegment to true (SHOULD NOT UNPAUSE)
    public void HandleSegment()
    {
        if (!timelineManager.Paused)
        {
            timelineManager.Pause();
            if (timelineManager.isRecordingSegment)
            {
                // jsonToLLM.voiceActivated = false;
                // StopSegment(); // Stop the segment if already recording
            }
            else
            {
                StartSegment(); // Start a new segment if not recording
            }
        }
        else
        {
            if (timelineManager.isRecordingSegment)
            {
                // jsonToLLM.voiceActivated = false;
                // Debug.Log("voice check deactivated");
                // StopSegment(); // Stop the segment if already recording
            }
            else
            {
                StartSegment(); // Start a new segment if not recording
            }
        }
    }


    public void StartSegment()
    {
        if (segmentStarted)
        {
            return;
        }
        
        timelineManager.isRecordingSegment = true;
        segmentStarted = true;
        
        timelineManager.segmentCount++;
        
        // this is to reset time index since we dont unpause on system recording
        if (jsonToLLM.activateSystemRecording)
        {
            // reset time index on unpause
            timelineManager.TimeIndex = 0;
            timelineManager.RewindTimeIndex = 0;
        }
        
        if (timelineManager.segmentCount <= 0)
        {
            Debug.Log("Started first segment recording");
            jsonDirectory.IncrementDemoNum();
            
            // only used for system recording
            if (jsonToLLM.activateSystemRecording)
            {
                jsonDirectory.transcriptNum++;
            }
            
            if (jsonDirectory.InitialDemo)
            {
                jsonDirectory.InstantiateInitialFolders();
                jsonDirectory.InitialDemo = false;
            }
            jsonDirectory.InstantiateDemoFolders();
            if (!jsonToLLM.activateSystemRecording)
            {
                streamingSampleMic.OnButtonPressed();
                Debug.Log("recording started");
            }
        }
        else
        {
            Debug.Log("Started new segment recording");
            if (!jsonToLLM.activateSystemRecording)
            {
                streamingSampleMic.OnButtonPressed();
            }
        }
    }

    public void StopSegment()
    {
        if (segmentStarted == false)
        {
            return;
        }

        jsonToLLM.voiceActivated = false;
        streamingSampleMic.microphoneRecord.StopRecord();
        Debug.Log("Stopped segment recording");
        timelineManager.isRecordingSegment = false;
        jsonToLLM.isLogging = false;
        segmentStarted = false;
        activationConditionMet = false; 
        GroundSelection groundSelection = GameObject.FindGameObjectWithTag("Ground").GetComponent<GroundSelection>();
        groundSelection.ClearGroundHighlights();
        if (!jsonToLLM.activateSystemRecording)
        {
            StartCoroutine(ChainedCoroutines());
        } else if (jsonToLLM.activateSystemRecording) // dont need to process transcript if recording system
        {
            jsonToLLM.CreateJSONString();
            ResetJsonData();
            jsonDirectory.demoNum = -1;
        }
        
    }

    // Handles a mouse click with a delay to prevent rapid clicks
    private IEnumerator HandleClickWithDelay(System.Action handleClickAction)
    {
        canClick = false;
        handleClickAction();
        yield return new WaitForSeconds(0.5f);
        canClick = true;
    }
    
    
    // Handles annotation mode functionality and stores clicked objects as annotations
    private void HandleAnnotationMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;

            // Always add new annotation without checking for existing keys
            annotation.Add(clickOrder, clickedObject);
            annotationDescriptions.Add(clickOrder, GetDescriptionAnnotation(clickedObject));
            objectToKey[clickedObject] = clickOrder; // Map object to key
            annotationTimes.Add(clickOrder, jsonToLLM.time);
            Debug.Log($"Added {clickedObject.name} to annotations with key {clickOrder}");
            clickOrder++;
        }
    }

    // Handles position mode functionality and stores clicked positions as annotations
    private void HandlePositionMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 clickedPosition = hit.point;
            annotation.Add(clickOrder, clickedPosition);
            annotationDescriptions.Add(clickOrder, $"(Position at {clickedPosition})");
            annotationTimes.Add(clickOrder, jsonToLLM.time);
            Debug.Log($"Added position {clickedPosition} to annotations with key {clickOrder}");
            clickOrder++;
        }
    }

    // Generates a description for an annotation based on the object clicked
    private string GetDescriptionAnnotation(GameObject gameObject)
    {
        return $"(Coach is pointing at {gameObject.name})";
    }

    // Generates a description for an annotation based on the position clicked
    private string GetDescriptionPosition(Vector3 vector)
    {
        return $"(Coach is set on the position vector, {vector})";
    }

    // Handles the final transcription process and stores the explanation
    public void OnTranscriptionFinished(string finalTranscription)
    {
        Debug.Log("Processed Transcription: " + finalTranscription);
        explanation = finalTranscription;
    }

    // Converts annotations to a JSON-like structure for further processing
    public List<Dictionary<string, object>> GetAnnotationsAsJson()
{
    List<Dictionary<string, object>> annotationsList = new List<Dictionary<string, object>>();

    foreach (var entry in annotation)
    {
        int id = entry.Key;
        object value = entry.Value;

        if (value is GameObject gameObject)
        {
            annotationsList.Add(new Dictionary<string, object>
            {
                { "id", id.ToString() },
                { "type", "Reference" },
                { "obj", gameObject.name }
            });
        }
        else if (value is Vector3 vector)
        {
            annotationsList.Add(new Dictionary<string, object>
            {
                { "id", id.ToString() },
                { "type", "Point" },
                { "point", new { x = vector.x, y = vector.z } },
            });
        }
        else if (value is Dictionary<string, object> actionData)
        {
            string actionType = actionData["type"].ToString();
            if (actionType == "Intercept")
            {
                annotationsList.Add(new Dictionary<string, object>
                {
                    { "id", id.ToString() },
                    { "type", actionType }
                });
            }
            else if (actionType == "Pass")
            {
                annotationsList.Add(new Dictionary<string, object>
                {
                    { "id", id.ToString() },
                    { "type", actionType },
                    { "from", actionData["from"] },
                    { "to", actionData["to"] }
                });
            }
            else if (actionType == "Through Pass")
            {
                var point = actionData["to"] as Dictionary<string, float>;
                annotationsList.Add(new Dictionary<string, object>
                {
                    { "id", id.ToString() },
                    { "type", actionType },
                    { "from", actionData["from"]},
                    { "to", point }
                });
            }
        }
    }

    return annotationsList;
}


    // Handles sending JSON data to the synthesis component
    IEnumerator JSONCoroutine()
    {
        Debug.Log("Started JSON Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(1);
        // synthConnect.SendScene();
        yield return new WaitForSeconds(1);
        // synthConnect.SendSceneAndExplanation();
    }
    
    // Chains the execution of multiple coroutines sequentially
    IEnumerator ChainedCoroutines()
    {
        yield return FileCoroutine();
        // yield return JSONCoroutine();
    }
    
    // Writes the segment data to a file and resets JSON data
    IEnumerator FileCoroutine()
    {
        Debug.Log("Started File Coroutine at timestamp : " + Time.time);
        yield return ProcessingTranscript();
        jsonToLLM.WriteFile();
        ResetJsonData();
    }
    
    
    private IEnumerator ProcessingTranscript()
    {
        jsonToLLM.isTranscriptionComplete = false;
        countdownText.gameObject.SetActive(true);
        countdownText.color = Color.red;

        string baseText = "TRANSCRIPTION PROCESSING";
        int dotCount = 0;
        countdownText.fontSize = 100;

        while (!jsonToLLM.isTranscriptionComplete) 
        {
            countdownText.text = $"{baseText}{new string('.', dotCount % 4)}";
            dotCount++;
            yield return new WaitForSeconds(1f); 
        }

        countdownText.gameObject.SetActive(false);
        countdownText.color = Color.red;

        if (!restarting)
        {
            StartSegment();
        }
    }


    void FixedUpdate()
    {
        if (rb == null)
        {
            GameObject coachObject = GameObject.FindGameObjectWithTag("human");
            if (coachObject != null)
            {
                rb = coachObject.GetComponent<Rigidbody>();
            }
        }

        if (rb == null)
        {
            return;
        }
        // Ensure movement only occurs if not paused and Rigidbody is available
        if (timelineManager.Paused)
        {
            return;
        }
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        Vector3 forwardDirection = transform.forward;
        movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized * moveSpeed;
        
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }
}