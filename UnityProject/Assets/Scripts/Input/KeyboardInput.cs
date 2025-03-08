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
    [Header("Movement / Speed")]
    [SerializeField] public float moveSpeed = 4f;
    [Header("Scene References")]
    public ExitScenario exitScenario;
    public TimelineManager timelineManager;
    private JSONToLLM jsonToLLM;
    private ChatBehaviour chatBehaviour;
    private JSONDirectory jsonDirectory;
#if UNITY_EDITOR
    private RecorderManager recorderManager;
#endif

    [Header("UI / Output")]
    public TextMeshProUGUI countdownText;
    public GameObject saveDemoCanvas;

    [Header("Audio Recording")]
    public RecordAudio recordAudio;

    [Header("Annotations")]
    public Dictionary<int, object> annotation = new Dictionary<int, object>();
    public Dictionary<int, string> annotationDescriptions = new Dictionary<int, string>();
    private Dictionary<GameObject, int> objectToKey = new Dictionary<GameObject, int>();
    public Dictionary<int, float> annotationTimes = new Dictionary<int, float>();
    public int clickOrder = 0; 

    [Header("Segment / Logging State")]
    public bool segmentStarted = false;
    public bool activationConditionMet = false;
    public bool restarting = false;
    public bool canClick = true;
    public float segmentStartTime = 0f;
    public Rigidbody rb;
    public Vector3 movement;
    private bool isAnnotationMode = false;
    private bool isPositionMode = false;
    private bool isReferenceMode = false;
    public string explanation;

    void Start()
    {
        // Basic references
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        countdownText = GameObject.FindGameObjectWithTag("countdown").GetComponent<TextMeshProUGUI>();
        chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponent<ChatBehaviour>();
        jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
#if UNITY_EDITOR
        recorderManager = GameObject.FindGameObjectWithTag("RecorderManager").GetComponent<RecorderManager>();
#endif        

        Debug.Log("KeyboardInput script initialized");
        
        // StartCoroutine(StartSegmentAfterDelay(15f));
        // StartCoroutine(StopSegmentAfterDelay(20f));
    }
    private IEnumerator StartSegmentAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Auto-starting segment after delay for VR debugging.");
        StartSegment();
    }
    
    private IEnumerator StopSegmentAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Auto-stopping segment after delay for VR debugging.");
        StopSegment();
    }

    void Update()
    {
        if (exitScenario == null)
        {
            GameObject human = GameObject.FindGameObjectWithTag("human");
            if (human != null)
                exitScenario = human.GetComponent<ExitScenario>();
        }

        // Press E to restart
        if (Input.GetKeyDown(KeyCode.E) && gameObject.CompareTag("keyboard"))
        {
            HandleRestart();
        }

        // Press B to toggle segment
        if (Input.GetKeyDown(KeyCode.B) && gameObject.CompareTag("keyboard"))
        {
            HandleSegment();
        }

        // Press P to pause/unpause
        if (Input.GetKeyDown(KeyCode.P) && gameObject.CompareTag("keyboard"))
        {
            HandlePause();
        }

        // Annotation clicks
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

        // Debug: Press K 
        if (Input.GetKeyDown(KeyCode.K))
        {
            foreach (var ann in annotation)
            {
                if (ann.Value is GameObject obj)
                {
                    Debug.Log($"Annotation Key {ann.Key}, Object: {obj.name}");
                }
                else if (ann.Value is Vector3 vector)
                {
                    Debug.Log($"Annotation Key {ann.Key}, Position: {vector}");
                }
            }
        }
    }

    // For annotation clicks
    public void HandlePositionClick()
    {
        StartCoroutine(HandleClickWithDelay(HandlePositionMode));
    }

    public void HandleAnnotationClick()
    {
        StartCoroutine(HandleClickWithDelay(HandleAnnotationMode));
    }

    // Pause/unpause
    public void HandlePause()
    {
        if (timelineManager.Paused)
        {
            timelineManager.Unpause();
            activationConditionMet = true;

            // If a segment is running, tell JSONToLLM to start logging
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

    // Restart scenario
    public void HandleRestart()
    {
        restarting = true;
        StopSegment();

        if (!timelineManager.Paused)
        {
            timelineManager.Pause();
        }

        canClick = false;
        saveDemoCanvas.SetActive(true);
    }

    // Press B: toggle segment (start/stop)
    public void HandleSegment()
    {
        // If unpaused, pause it first
        if (!timelineManager.Paused)
        {
            timelineManager.Pause();
            if (timelineManager.isRecordingSegment)
            {
                StopSegment();
            }
            else
            {
                StartSegment();
            }
        }
        else
        {
            // If paused, toggle the segment
            if (timelineManager.isRecordingSegment)
            {
                StopSegment();
            }
            else
            {
                StartSegment();
            }
        }
    }

    // Start the segment + audio
    // (Video is started automatically in JSONToLLM.FixedUpdate if isLogging==true)
    public void StartSegment()
    {
    
        if (segmentStarted) return;

        timelineManager.isRecordingSegment = true;
        segmentStarted = true;
        timelineManager.segmentCount++;

        segmentStartTime = Time.time;

        // Start audio only
        if (recordAudio != null)
        {
            recordAudio.StartRecording();
            Debug.Log("Audio recording started with the segment.");
        }
        else
        {
            Debug.LogWarning("No RecordAudio reference set in KeyboardInput!");
        }

        // Instead of calling recorderManager.StartRecording(), we rely on JSONToLLM
        // to do that in its FixedUpdate when isLogging==true.

        // Force JSONToLLM to start logging so video can start in FixedUpdate
        jsonToLLM.isLogging = true;

        // Folder setup if needed
        #if UNITY_EDITOR
        if (jsonDirectory.InitialDemo)
        {
            jsonDirectory.InstantiateInitialFolders();
            jsonDirectory.InitialDemo = false;
        }
        jsonDirectory.InstantiateDemoFolders();
        #endif

        Debug.Log("Started new segment recording (audio). JSONToLLM will auto-start video in FixedUpdate.");
    }

    // Stop the segment + audio
    // (Video is stopped automatically in JSONToLLM.FixedUpdate if isLogging==false)
    public void StopSegment()
    {
        if (!segmentStarted) return;

        jsonToLLM.voiceActivated = false;
        Debug.Log("Stopped segment recording");
        timelineManager.isRecordingSegment = false;
        jsonToLLM.isLogging = false; // triggers video stop in JSONToLLM
        segmentStarted = false;
        activationConditionMet = false;
        // Stop audio
        if (recordAudio != null && Microphone.IsRecording(null))
        {
            recordAudio.StopRecording();
            Debug.Log("Audio recording stopped with the segment.");
        }
        
        // if (recorderManager.RecorderController.IsRecording())
        // {
        //     recorderManager.StopRecording();
        //     Debug.Log("Video recording stopped with the segment.");
        // }
        #if UNITY_EDITOR
        GroundSelection groundSelection = GameObject.FindGameObjectWithTag("Ground")
            .GetComponent<GroundSelection>();
        groundSelection.ClearGroundHighlights();
        #endif

        StartCoroutine(ChainedCoroutines());
    }

    private IEnumerator HandleClickWithDelay(Action handleClickAction)
    {
        canClick = false;
        handleClickAction();
        yield return new WaitForSeconds(0.5f);
        canClick = true;
    }

    private void HandleAnnotationMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;
            annotation.Add(clickOrder, clickedObject);
            annotationDescriptions.Add(clickOrder, GetDescriptionAnnotation(clickedObject));
            objectToKey[clickedObject] = clickOrder;

            float annotationRelativeTime = Time.time - segmentStartTime;
            annotationTimes.Add(clickOrder, annotationRelativeTime);

            Debug.Log($"Added {clickedObject.name} to annotations at {annotationRelativeTime:F2}s, key {clickOrder}");
            clickOrder++;
        }
    }

    private void HandlePositionMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 clickedPosition = hit.point;
            annotation.Add(clickOrder, clickedPosition);
            annotationDescriptions.Add(clickOrder, $"(Position at {clickedPosition})");

            float annotationRelativeTime = Time.time - segmentStartTime;
            annotationTimes.Add(clickOrder, annotationRelativeTime);

            Debug.Log($"Added position {clickedPosition} to annotations at {annotationRelativeTime:F2}s, key {clickOrder}");
            clickOrder++;
        }
    }

    private string GetDescriptionAnnotation(GameObject go)
    {
        return $"(Coach is pointing at {go.name})";
    }

    public void OnTranscriptionFinished(string finalTranscription)
    {
        Debug.Log("Processed Transcription: " + finalTranscription);
        explanation = finalTranscription;
    }

    public List<Dictionary<string, object>> GetAnnotationsAsJson()
    {
        var annotationsList = new List<Dictionary<string, object>>();

        foreach (var entry in annotation)
        {
            int id = entry.Key;
            object value = entry.Value;
            if (value is GameObject go)
            {
                annotationsList.Add(new Dictionary<string, object>
                {
                    { "id", id.ToString() },
                    { "type", "Reference" },
                    { "obj", go.name }
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
        }
        return annotationsList;
    }

    IEnumerator JSONCoroutine()
    {
        Debug.Log("Started JSON Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(1);
        yield return new WaitForSeconds(1);
    }

    IEnumerator ChainedCoroutines()
    {
        yield return FileCoroutine();
    }

    IEnumerator FileCoroutine()
    {
        Debug.Log("Started File Coroutine at timestamp : " + Time.time);
        yield return ProcessingTranscript();
        #if UNITY_EDITOR
        jsonToLLM.WriteFile();
        #endif
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
        
        // If you don’t want to auto-restart the segment, comment the next line:
        // StartSegment();
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            GameObject coachObject = GameObject.FindGameObjectWithTag("human");
            if (coachObject != null)
            {
                rb = coachObject.GetComponent<Rigidbody>();
                Debug.Log("found human rigidbody");
            }
        }
        if (rb == null) return;
        if (timelineManager.Paused) return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        Vector3 forwardDirection = transform.forward;
        movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized * moveSpeed;
        
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
        Debug.Log("moved");
    }

    private void ResetJsonData()
    {   
#if UNITY_EDITOR
        if (recorderManager.RecorderController.IsRecording())
        {
            recorderManager.StopRecording();
        }
#endif

        annotation.Clear();
        annotationDescriptions.Clear();
        objectToKey.Clear();
        annotationTimes.Clear();
        clickOrder = 0;
        jsonToLLM.ResetSegmentData();
    }
}
