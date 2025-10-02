using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Fusion;
using Oculus.Interaction;
using OpenAI.Samples.Chat;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utilities.Extensions;
using Whisper.Samples;
using Vector3 = UnityEngine.Vector3;

public class KeyboardInput : NetworkBehaviour
{
    [Header("Movement / Speed")]
    [SerializeField] public float moveSpeed = 4f;
    [Header("Scene References")]
    public ExitScenario exitScenario;
    public TimelineManager timelineManager;
    private JSONToLLM jsonToLLM;
    private ChatBehaviour chatBehaviour;
    private JSONDirectory jsonDirectory;
    private RecorderManager recorderManager;
    private GameManager gameManager;

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

    public GameObject recordingDot;

    void Start()
    {
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        countdownText = GameObject.FindGameObjectWithTag("countdown").GetComponent<TextMeshProUGUI>();
        chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponent<ChatBehaviour>();
        jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
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
            // jsonDirectory.DoNotSaveDemonstrationButton();
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
    // This is the modified section of the HandlePause() method in KeyboardInput.cs

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
            // Add annotation for pausing
            if (segmentStarted)
            {
                // Create a dictionary with the PauseAction type
                Dictionary<string, object> pauseAction = new Dictionary<string, object>
                {
                    { "type", "PauseAction" }
                };
            
                // Add to annotations with current click order
                annotation.Add(clickOrder, pauseAction);
                annotationDescriptions.Add(clickOrder, "Coach paused the game");
            
                // Record the time of pause relative to segment start
                float pauseTime = Time.time - segmentStartTime;
                annotationTimes.Add(clickOrder, pauseTime);
            
                Debug.Log($"Added PauseAction annotation at {pauseTime:F2}s, key {clickOrder}");
            
                // Increment click order for next annotation
                clickOrder++;
            }
            
            // clear ground highlights upon pause
            GroundSelection groundSelection = GameObject.FindGameObjectWithTag("Ground")
                .GetComponent<GroundSelection>();
            groundSelection.ClearGroundHighlights();
        
            timelineManager.Pause();
        }
    }

    // Restart scenario
    public void HandleRestart()
    {
        if (jsonToLLM.isLogging)
        {
            return;
        }
        if (saveDemoCanvas.activeSelf)
        {
            saveDemoCanvas.SetActive(false);
            return;
        }
        restarting = true;
        // StopSegment();

        // if (!timelineManager.Paused)
        // {
        //     timelineManager.Pause();
        // }

        canClick = false;

        // saveDemoCanvas.GetComponent<Canvas>().worldCamera = GameObject.Find("CenterEyeAnchor").GetComponent<Camera>();
        if (gameManager.laptopMode)
        {
            saveDemoCanvas.SetActive(true);
        }
        else
        {
            RPC_CanvasSetActive(true);
            ObjectsList objectsList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        
            Transform vrTrans = GameObject.FindGameObjectWithTag("human").GetComponent<HumanInterface>().vrTransform;
        
            if (objectsList.viewerPlayer != null)
            {
                vrTrans = objectsList.viewerPlayer.GetComponent<HumanInterface>().vrTransform;
            }
        
            Vector3 canvasPos = vrTrans.position + vrTrans.forward * 5f;
            canvasPos.y = Mathf.Max(canvasPos.y, 3f);
            saveDemoCanvas.transform.position = canvasPos;
        
            saveDemoCanvas.transform.rotation = UnityEngine.Quaternion.LookRotation(saveDemoCanvas.transform.position - vrTrans.position);
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CanvasSetActive(bool active)
    {
        saveDemoCanvas.SetActive(active);
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
        // In laptop mode, use simple direct call
        if (gameManager.laptopMode)
        {
            StartSegmentInternal();
        }
        else
        {
            RPC_StartSegment();
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartSegment()
    {
        StartSegmentInternal();
    }

    public void StartSegmentInternal()
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        // if (!gm.isHost)
        // {
            if (segmentStarted) return;
            
            // Reset state from previous recordings
            jsonToLLM.PrepareForNewRecording();
            
            // Ensure recorder is reset for new recording
#if UNITY_EDITOR
            if (recorderManager != null)
            {
                // Force re-initialization of the recorder
                recorderManager.Initialize();
                Debug.Log("Re-initialized RecorderManager for new segment");
            }
#endif

            timelineManager.isRecordingSegment = true;
            segmentStarted = true;
            timelineManager.segmentCount++;

            recordingDot.SetActive(true);

            segmentStartTime = Time.time;

            if (gm.isHost || gm.laptopMode)
            {
                if (recordAudio != null)
                {
                    recordAudio.StartRecording();
                    Debug.Log("Audio recording started with the segment.");
                }
                else
                {
                    Debug.LogWarning("No RecordAudio reference set in KeyboardInput!");
                }
            }

        // Force JSONToLLM to start logging so video can start in FixedUpdate
            jsonToLLM.isLogging = true;

#if UNITY_EDITOR
            // Folder setup if needed
      
            if (jsonDirectory.InitialDemo)
            {
                jsonDirectory.InstantiateInitialFolders();
                jsonDirectory.InitialDemo = false;
                Debug.Log("Directory created");
            }
            jsonDirectory.InstantiateDemoFolders();
            
            Debug.Log("Started new segment recording (audio). JSONToLLM will auto-start video in FixedUpdate.");
#endif

        // }
    }

    // Stop the segment + audio
    // (Video is stopped automatically in JSONToLLM.FixedUpdate if isLogging==false)
    public void StopSegment()
    {
        // In laptop mode, use simple direct call
        if (gameManager.laptopMode)
        {
            StopSegmentInternal();
        }
        else
        {
            RPC_StopSegment();
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StopSegment()
    {
        StopSegmentInternal();
    }
    
    private void StopSegmentInternal()
    {
        if (!segmentStarted) return;

        // jsonToLLM.voiceActivated = false;
        Debug.Log("Stopped segment recording");
        timelineManager.isRecordingSegment = false;
        jsonToLLM.isLogging = false; // triggers video stop in JSONToLLM - NOT ANYMORE, now done in FileCoroutine
        segmentStarted = false;
        
        recordingDot.SetActive(false);
        // jsonToLLM.activateSystemRecording = true;
        // Stop audio
        
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost || gm.laptopMode)
        {
            if (recordAudio != null && Microphone.IsRecording(null))
            {
                recordAudio.StopRecording();
                Debug.Log("Audio recording stopped with the segment.");
            }
        }

        GroundSelection groundSelection = GameObject.FindGameObjectWithTag("Ground")
            .GetComponent<GroundSelection>();
            groundSelection.ClearGroundHighlights();

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
        
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
#if UNITY_ANDROID && !UNITY_EDITOR
        ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
#endif
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
        // RPC_HandleAnnotationMode();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HandleAnnotationMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
#if UNITY_ANDROID && !UNITY_EDITOR
        ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
#endif
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
#if UNITY_ANDROID && !UNITY_EDITOR
        ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
#endif
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 clickedPosition = hit.point;
            GameObject human = GameObject.FindGameObjectWithTag("human");
            if (human != null)
            {
                human.GetComponent<HumanInterface>().xMark = clickedPosition;
            }

            annotation.Add(clickOrder, clickedPosition);
            annotationDescriptions.Add(clickOrder, $"(Position at {clickedPosition})");

            float annotationRelativeTime = Time.time - segmentStartTime;
            annotationTimes.Add(clickOrder, annotationRelativeTime);

            Debug.Log($"Added position {clickedPosition} to annotations at {annotationRelativeTime:F2}s, key {clickOrder}");
            clickOrder++;
        }
        // RPC_HandlePositionMode();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HandlePositionMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
#if UNITY_ANDROID && !UNITY_EDITOR
        ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
#endif
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
    
    private bool annotationsReady = false;

    public bool AreAnnotationsSynced()
    {
        return gameManager.laptopMode ? true : annotationsReady;
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClearAnnotations()
    {
        if (Runner.IsClient)
        {
            annotation.Clear();
            annotationDescriptions.Clear();
            annotationTimes.Clear();
            annotationsReady = false;
            Debug.Log("CLIENT: Cleared all annotation dictionaries");
        }
    }
    
    // Struct to hold Annotation data for RPCs
    public struct AnnotationData : INetworkStruct
    {
        public int AnnotationID;
        public byte AnnotationType; // 0 = GameObject, 1 = Vector3, 2 = Dictionary
    
        // For Vector3 type
        public float PositionX;
        public float PositionY;
        public float PositionZ;
    
        // For annotation description
        [Networked, Capacity(128)]
        public NetworkString<_128> Description { get; set; }
    
        // For annotation time
        public float AnnotationTime;
    
        // For GameObject and Dictionary types - Object name/type
        [Networked, Capacity(64)]
        public NetworkString<_64> ObjectName { get; set; }
    
        // For Dictionary type additional data
        [Networked, Capacity(64)]
        public NetworkString<_64> DictType { get; set; }
    
        [Networked, Capacity(64)]
        public NetworkString<_64> DictFrom { get; set; }
    
        [Networked, Capacity(64)]
        public NetworkString<_64> DictTo { get; set; }
    
        // For dictionary point coordinates
        public float PointX;
        public float PointY;
    }
    
    public void SyncAnnotationsToClients()
    {
        if (gameManager.laptopMode || !Runner.IsServer) return;
        
        // First, clear client annotations
        RPC_ClearAnnotations();
        
        List<AnnotationData> allAnnotations = new List<AnnotationData>();
        
        // Convert all annotations to network-friendly format
        foreach (var entry in annotation)
        {
            int id = entry.Key;
            object value = entry.Value;
            string description = annotationDescriptions.ContainsKey(id) ? annotationDescriptions[id] : "";
            float time = annotationTimes.ContainsKey(id) ? annotationTimes[id] : 0f;
            
            AnnotationData data = new AnnotationData();
            data.AnnotationID = id;
            data.Description = description;
            data.AnnotationTime = time;
            
            if (value is GameObject go)
            {
                data.AnnotationType = 0; // GameObject
                data.ObjectName = go.name;
            }
            else if (value is Vector3 vector)
            {
                data.AnnotationType = 1; // Vector3
                data.PositionX = vector.x;
                data.PositionY = vector.y;
                data.PositionZ = vector.z;
            }
            else if (value is Dictionary<string, object> dictValue)
            {
                data.AnnotationType = 2; // Dictionary
                
                // Handle common dictionary fields
                if (dictValue.ContainsKey("type"))
                    data.DictType = dictValue["type"].ToString();
                    
                if (dictValue.ContainsKey("player") || dictValue.ContainsKey("from"))
                    data.DictFrom = dictValue.ContainsKey("player") ? dictValue["player"].ToString() : dictValue["from"].ToString();
                    
                if (dictValue.ContainsKey("to"))
                {
                    if (dictValue["to"] is Dictionary<string, float> pointDict)
                    {
                        data.PointX = pointDict.ContainsKey("x") ? pointDict["x"] : 0f;
                        data.PointY = pointDict.ContainsKey("y") ? pointDict["y"] : 0f;
                    }
                    else
                    {
                        data.DictTo = dictValue["to"].ToString();
                    }
                }
            }
            
            allAnnotations.Add(data);
        }
        
        // Send annotations in chunks
        const int CHUNK_SIZE = 5; // Adjust based on complexity
        
        for (int i = 0; i < allAnnotations.Count; i += CHUNK_SIZE)
        {
            int currentChunkSize = Mathf.Min(CHUNK_SIZE, allAnnotations.Count - i);
            AnnotationData[] chunk = new AnnotationData[currentChunkSize];
            
            for (int j = 0; j < currentChunkSize; j++)
            {
                chunk[j] = allAnnotations[i + j];
            }
            
            RPC_ReceiveAnnotationChunk(chunk);
        }
        
        // Signal that all annotations have been sent
        RPC_FinishAnnotationSync();
        
        Debug.Log($"SERVER: Sent {allAnnotations.Count} annotations to clients");
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReceiveAnnotationChunk(AnnotationData[] chunk)
    {
        if (Runner.IsClient)
        {
            foreach (var data in chunk)
            {
                int id = data.AnnotationID;
                
                // Add to annotationTimes
                annotationTimes[id] = data.AnnotationTime;
                
                // Add to annotationDescriptions
                annotationDescriptions[id] = data.Description.ToString();
                
                // Add to annotation based on type
                if (data.AnnotationType == 0) // GameObject
                {
                    // For GameObject references, we need to find the object by name
                    string objectName = data.ObjectName.ToString();
                    GameObject foundObj = GameObject.Find(objectName);
                    
                    if (foundObj != null)
                    {
                        annotation[id] = foundObj;
                    }
                    else
                    {
                        Debug.LogError($"CLIENT: Could not find GameObject named {objectName}");
                        // Create a placeholder dictionary to avoid null references
                        annotation[id] = new Dictionary<string, object> { { "type", "Reference" }, { "obj", objectName } };
                    }
                }
                else if (data.AnnotationType == 1) // Vector3
                {
                    annotation[id] = new Vector3(data.PositionX, data.PositionY, data.PositionZ);
                }
                else if (data.AnnotationType == 2) // Dictionary
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    
                    if (!string.IsNullOrEmpty(data.DictType.ToString()))
                        dict["type"] = data.DictType.ToString();
                        
                    if (!string.IsNullOrEmpty(data.DictFrom.ToString()))
                    {
                        // Handle player or from
                        if (data.DictType.ToString() == "ReceiveBall" || 
                            data.DictType.ToString() == "PickUp" || 
                            data.DictType.ToString() == "PutDown" || 
                            data.DictType.ToString() == "Packaging")
                        {
                            dict["player"] = data.DictFrom.ToString();
                        }
                        else
                        {
                            dict["from"] = data.DictFrom.ToString();
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(data.DictTo.ToString()))
                    {
                        dict["to"] = data.DictTo.ToString();
                    }
                    else if (data.PointX != 0 || data.PointY != 0)
                    {
                        // Point coordinates
                        dict["to"] = new Dictionary<string, float> { { "x", data.PointX }, { "y", data.PointY } };
                    }
                    
                    annotation[id] = dict;
                }
            }
            
            Debug.Log($"CLIENT: Received {chunk.Length} annotations");
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FinishAnnotationSync()
    {
        if (Runner.IsClient)
        {
            annotationsReady = true;
            Debug.Log($"CLIENT: All annotations synced. Total annotations: {annotation.Count}");
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
                    { "point", new { x = vector.x, y = vector.z } }
                });
            }
            // NEW: handle dictionary-based annotations (e.g. "PickUp", "Pass", etc.)
            else if (value is Dictionary<string, object> dictValue)
            {
                // Start by creating a fresh dictionary for the JSON entry
                var annotationDict = new Dictionary<string, object>
                {
                    ["id"] = id.ToString()
                };

                // Merge user-defined fields from your log calls
                foreach (var kvp in dictValue)
                {
                    annotationDict[kvp.Key] = kvp.Value;
                }

                annotationsList.Add(annotationDict);
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
        
        // Write the JSON file BEFORE stopping the recording
#if UNITY_EDITOR
        // First write the JSON file
        jsonToLLM.WriteFile();
        Debug.Log("JSON file has been successfully written");
#endif
    
        ResetJsonData();
        
#if UNITY_EDITOR
        // Only then stop the recording
        if (recorderManager != null && recorderManager.RecorderController != null)
        {
            Debug.Log("Now stopping video recording...");
            recorderManager.StopRecording();
            
            // Only wait for processing in multiplayer client mode
            if (!gameManager.laptopMode && gameManager._runner.IsClient)
            {
                // Wait for the recording to be processed with a timeout
                float startTime = Time.time;
                float timeout = 30f;
        
                Debug.Log("CLIENT: Waiting for video to be processed...");
        
                while (recorderManager.IsRecordingProcessing && (Time.time - startTime < timeout))
                {
                    yield return new WaitForSeconds(0.5f);
                }
        
                if (recorderManager.IsRecordingProcessing)
                {
                    Debug.LogError("CLIENT: Timed out waiting for video to be processed!");
                    recorderManager.IsRecordingProcessing = false;
                }
                else
                {
                    Debug.Log("CLIENT: Video processing completed successfully");
                }
            
                // Release resources
                recorderManager.ReleaseRecorderResources();
        
                // Notify server that video has been saved
                jsonToLLM.RPC_NotifyVideoSaveComplete();
                Debug.Log("CLIENT: Notified server that video save is complete");
            }
        }
#endif
    }

    private IEnumerator ProcessingTranscript()
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        
        // Laptop mode: Simple waiting without client synchronization
        if (gm.laptopMode)
        {
            jsonToLLM.isTranscriptionComplete = false;
            countdownText.gameObject.SetActive(true);
            countdownText.color = Color.red;

            string baseText = "TRANSCRIPTION PROCESSING \n(DO NOT RESTART YET)";
            int dotCount = 0;
            countdownText.fontSize = 40;

            while (!jsonToLLM.AreAllChunksReceived())
            {
                countdownText.text = $"{baseText}{new string('.', dotCount % 4)}";
                dotCount++;
                yield return new WaitForSeconds(0.5f);
            }

            countdownText.gameObject.SetActive(false);
            Debug.Log("Laptop Mode: Transcription processing complete");
            yield break;
        }
        
        // Multiplayer mode: Full synchronization logic
        if (gm.isHost && !gm.laptopMode)
        {
            jsonToLLM.isTranscriptionComplete = false;
            countdownText.gameObject.SetActive(true);
            countdownText.color = Color.red;

            string baseText = "TRANSCRIPTION PROCESSING \n(DO NOT RESTART YET)";
            int dotCount = 0;
            countdownText.fontSize = 40;

            while (!jsonToLLM.isTranscriptionComplete)
            {
                countdownText.text = $"{baseText}{new string('.', dotCount % 4)}";
                dotCount++;
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("SERVER: Transcription processing complete");
            
            // Now wait for client to confirm it received all data
            baseText = "WAITING FOR CLIENT \n(DO NOT RESTART YET)";
            dotCount = 0;
        
            float startTime = Time.time;
            float timeout = 30f;
        
            while (!jsonToLLM.HasClientReceivedAllData() && (Time.time - startTime < timeout))
            {
                countdownText.text = $"{baseText}{new string('.', dotCount % 4)}";
                dotCount++;
                yield return new WaitForSeconds(0.5f);
            }
            
            if (!jsonToLLM.HasClientReceivedAllData())
            {
                Debug.LogError("SERVER: Timed out waiting for client to receive data");
            }
            
            // Now wait for client to confirm it saved the video
            baseText = "WAITING FOR VIDEO SAVE \n(DO NOT RESTART YET)";
            dotCount = 0;

            startTime = Time.time;
            timeout = 30f; // Longer timeout for video saving

            while (!jsonToLLM.HasClientSavedVideo() && (Time.time - startTime < timeout))
            {
                countdownText.text = $"{baseText}{new string('.', dotCount % 4)}";
                dotCount++;
                yield return new WaitForSeconds(0.5f);
            }

            if (!jsonToLLM.HasClientSavedVideo())
            {
                Debug.LogWarning("SERVER: Timed out waiting for client to save video");
            }
    
            countdownText.gameObject.SetActive(false);
            Debug.Log("SERVER: Client video save complete or timed out");
        }
        else
        {
            // Client-specific processing
            countdownText.gameObject.SetActive(true);
            countdownText.color = Color.red;
            countdownText.fontSize = 40;
        
            string chunkText = "RECEIVING DATA CHUNKS";
            int chunkDotCount = 0;
        
            float startTime = Time.time;
            float timeout = 30f; // Increase timeout for longer recordings
        
            // Client waits for both dictionary chunks and annotations
            while ((!jsonToLLM.AreAllChunksReceived() || !AreAnnotationsSynced()) && (Time.time - startTime < timeout))
            {
                if (!jsonToLLM.AreAllChunksReceived())
                {
                    chunkText = "RECEIVING DATA CHUNKS";
                }
                else if (!AreAnnotationsSynced())
                {
                    chunkText = "RECEIVING ANNOTATIONS";
                }
            
                countdownText.text = $"{chunkText} {new string('.', chunkDotCount % 4)}";
                chunkDotCount++;
                yield return new WaitForSeconds(0.5f);
            }
        
            countdownText.gameObject.SetActive(false);
        
            if (!jsonToLLM.AreAllChunksReceived() || !AreAnnotationsSynced())
            {
                Debug.LogError($"CLIENT: Timed out waiting for data! Chunks: {jsonToLLM.totalChunksReceived}/{jsonToLLM.totalChunksSent}, Annotations synced: {AreAnnotationsSynced()}");
                // Notify server anyway to avoid deadlock
                jsonToLLM.RPC_NotifyChunksReceived();
            }
            else
            {
                Debug.Log("CLIENT: All data successfully received");
                jsonToLLM.RPC_NotifyChunksReceived();
            }
        }

        // If you don’t want to auto-restart the segment, comment the next line:
        // StartSegment();
    }

    void FixedUpdate()
    {
// #if UNITY_EDITOR
        if (rb == null)
        {
            GameObject coachObject = GameObject.FindGameObjectWithTag("human");
            if (coachObject == null)
            {
                return;
            }
            if (coachObject != null)
            {
                rb = coachObject.GetComponent<Rigidbody>();
                Debug.Log("found human rigidbody");
            }
        }
        if (rb == null) return;
        if (timelineManager.Paused) return;

        float horizontalInput = 0f;
        float verticalInput = 0f;
        
        if (gameManager.laptopMode)
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
        }
        
        Vector3 forwardDirection = transform.forward;
        movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized * moveSpeed;
        
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
// #endif
    }

    public void ResetJsonData()
    {   
// #if UNITY_EDITOR
//         if (recorderManager.RecorderController.IsRecording())
//         {
//             recorderManager.StopRecording();
//         }
// #endif
        if (gameManager.laptopMode)
        {
            ResetJsonDataInternal();
        }
        else
        {
            RPC_ResetJsonData();
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetJsonData()
    {
        ResetJsonDataInternal();
    }

    private void ResetJsonDataInternal()
    {
        annotation.Clear();
        annotationDescriptions.Clear();
        objectToKey.Clear();
        annotationTimes.Clear();
        clickOrder = 0;
        annotationsReady = false;
        jsonToLLM.ResetSegmentData();
    }
}