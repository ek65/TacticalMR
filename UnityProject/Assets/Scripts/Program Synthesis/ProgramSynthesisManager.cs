using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Meta.WitAi.Windows;
using Old.OpenAI.Samples.Chat;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the program synthesis recording workflow including segment control, audio recording,
/// annotation handling, and coordination between multiple systems for demonstration capture.
/// Handles both laptop mode (single player) and VR multiplayer scenarios with proper synchronization.
/// </summary>
public class ProgramSynthesisManager : NetworkBehaviour
{
    [Header("Core System References")]
    public ExitScenario exitScenario;
    public TimelineManager timelineManager;
    private JSONToLLM jsonToLLM;
    private OldChatBehaviour chatBehaviour;
    private JSONDirectory jsonDirectory;
    private RecorderManager recorderManager;
    private GameManager gameManager;
    private AnnotationManager annotationManager;

    [Header("UI Components")]
    public TextMeshProUGUI countdownText;
    public GameObject saveDemoCanvas;
    public GameObject recordingDot;

    [Header("Audio Recording")]
    public RecordAudio recordAudio;

    [Header("Segment Control State")]
    public bool segmentStarted = false;
    public bool activationConditionMet = false;
    public bool restarting = false;
    public bool canClick = true;
    public float segmentStartTime = 0f;
    
    [Header("Interaction Modes")]
    private bool isAnnotationMode = false;
    private bool isPositionMode = false;
    private bool isReferenceMode = false;
    
    [Header("Output Data")]
    public string explanation;
    
    /// <summary>
    /// Initialize all component references and system dependencies
    /// </summary>
    void Start()
    {
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        if (GameObject.FindGameObjectWithTag("countdown"))
        {
            countdownText = GameObject.FindGameObjectWithTag("countdown").GetComponent<TextMeshProUGUI>();
        }
        if (GameObject.FindGameObjectWithTag("Character"))
        {
            chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponent<OldChatBehaviour>();
        }
        jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
#if UNITY_EDITOR
        recorderManager = GameObject.FindGameObjectWithTag("RecorderManager").GetComponent<RecorderManager>();
#endif
        annotationManager = this.gameObject.GetComponent<AnnotationManager>();

        Debug.Log("ProgramSynthesisManager initialized");
    }

    /// <summary>
    /// Update loop to handle dynamic reference resolution
    /// </summary>
    void Update()
    {
        // Resolve exit scenario reference if not set
        if (exitScenario == null)
        {
            GameObject human = GameObject.FindGameObjectWithTag("human");
            if (human != null)
                exitScenario = human.GetComponent<ExitScenario>();
        }
    }
    
    #region Timeline Control Methods
    
    /// <summary>
    /// Handle pause/unpause toggle for the timeline
    /// Manages logging state and creates pause annotations during segments
    /// </summary>
    public void HandlePause()
    {
        if (timelineManager.Paused)
        {
            // Unpause timeline and activate logging if segment is running
            timelineManager.Unpause();
            activationConditionMet = true;

            if (segmentStarted && activationConditionMet && !jsonToLLM.isLogging)
            {
                jsonToLLM.isLogging = true;
            }
        }
        else
        {
            // Create pause annotation if segment is active
            if (segmentStarted)
            {
                annotationManager.CreatePauseActionAnnotation(segmentStartTime);
            }
            
            // Clear UI elements and pause timeline
            GroundSelection groundSelection = GameObject.FindGameObjectWithTag("Ground")
                .GetComponent<GroundSelection>();
            groundSelection.ClearGroundHighlights();
        
            timelineManager.Pause();
        }
    }

    /// <summary>
    /// Handle scenario restart with proper state management
    /// Shows save dialog and manages canvas positioning for VR mode
    /// </summary>
    public void HandleRestart()
    {
        // Prevent restart during active logging
        if (jsonToLLM.isLogging)
        {
            return;
        }
        
        // Hide save canvas if already shown
        if (saveDemoCanvas.activeSelf)
        {
            saveDemoCanvas.SetActive(false);
            return;
        }
        
        restarting = true;
        canClick = false;

        if (gameManager.laptopMode)
        {
            // Simple activation for laptop mode
            saveDemoCanvas.SetActive(true);
        }
        else
        {
            // VR mode: Position canvas in front of user
            RPC_CanvasSetActive(true);
            ObjectsList objectsList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        
            Transform vrTrans = GameObject.FindGameObjectWithTag("human").GetComponent<HumanInterface>().vrTransform;
        
            if (objectsList.viewerPlayer != null)
            {
                vrTrans = objectsList.viewerPlayer.GetComponent<HumanInterface>().vrTransform;
            }
        
            // Position canvas in front of VR user
            Vector3 canvasPos = vrTrans.position + vrTrans.forward * 5f;
            canvasPos.y = Mathf.Max(canvasPos.y, 3f);
            saveDemoCanvas.transform.position = canvasPos;
        
            saveDemoCanvas.transform.rotation = UnityEngine.Quaternion.LookRotation(saveDemoCanvas.transform.position - vrTrans.position);
        }
    }
    
    /// <summary>
    /// Network RPC to synchronize canvas state across all clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CanvasSetActive(bool active)
    {
        saveDemoCanvas.SetActive(active);
    }

    #endregion

    #region Segment Recording Control

    /// <summary>
    /// Toggle segment recording state based on current timeline state
    /// Handles pause/unpause coordination with segment start/stop
    /// </summary>
    public void HandleSegment()
    {
        // Ensure timeline is paused before toggling segment
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
            // Toggle segment state when already paused
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

    /// <summary>
    /// Start segment recording with proper mode detection
    /// Initiates audio recording and sets up data collection
    /// </summary>
    public void StartSegment()
    {
        if (gameManager.laptopMode)
        {
            StartSegmentInternal();
        }
        else
        {
            RPC_StartSegment();
        }
    }
    
    /// <summary>
    /// Network RPC to start segment recording on all clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartSegment()
    {
        StartSegmentInternal();
    }

    /// <summary>
    /// Internal implementation of segment start logic
    /// Resets previous recording state and initializes new recording session
    /// </summary>
    public void StartSegmentInternal()
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        
        if (segmentStarted) return;
        
        // Reset state from previous recordings
        jsonToLLM.PrepareForNewRecording();
        
        // Re-initialize recorder for new session
#if UNITY_EDITOR
        if (recorderManager != null)
        {
            recorderManager.Initialize();
            Debug.Log("Re-initialized RecorderManager for new segment");
        }
#endif

        // Set recording state
        timelineManager.isRecordingSegment = true;
        segmentStarted = true;
        timelineManager.segmentCount++;

        recordingDot.SetActive(true);
        segmentStartTime = Time.time;

        // Start audio recording for host/laptop mode
        // NOTE: Audio recording is now handled by ChatBehaviour.cs for FactoryScenarioCreation mode
        // We don't start recordAudio here to avoid mic conflicts
        if (gm.isHost || gm.laptopMode)
        {
            if (recordAudio != null && ScenarioTypeManager.Instance != null && 
                ScenarioTypeManager.Instance.currentScenario != ScenarioTypeManager.ScenarioType.FactoryScenarioCreation)
            {
                recordAudio.StartRecording();
                Debug.Log("Audio recording started with the segment.");
            }
            else if (ScenarioTypeManager.Instance != null && 
                     ScenarioTypeManager.Instance.currentScenario == ScenarioTypeManager.ScenarioType.FactoryScenarioCreation)
            {
                Debug.Log("FactoryScenarioCreation mode: Audio handled by ChatBehaviour");
            }
            else
            {
                Debug.LogWarning("No RecordAudio reference set!");
            }
        }

        // Enable JSON data logging
        jsonToLLM.isLogging = true;

#if UNITY_EDITOR
        // Set up directory structure for first demo
        if (jsonDirectory.InitialDemo)
        {
            jsonDirectory.InstantiateInitialFolders();
            jsonDirectory.InitialDemo = false;
            Debug.Log("Directory created");
        }
        jsonDirectory.InstantiateDemoFolders();
        
        Debug.Log("Started new segment recording. Video will auto-start in FixedUpdate.");
#endif
    }

    /// <summary>
    /// Stop segment recording and begin processing pipeline
    /// Triggers audio stop and initiates file processing coroutines
    /// </summary>
    public void StopSegment()
    {
        if (gameManager.laptopMode)
        {
            StopSegmentInternal();
        }
        else
        {
            RPC_StopSegment();
        }
    }
    
    /// <summary>
    /// Network RPC to stop segment recording on all clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StopSegment()
    {
        StopSegmentInternal();
    }
    
    /// <summary>
    /// Internal implementation of segment stop logic
    /// Stops recording, cleans up UI, and starts processing pipeline
    /// </summary>
    private void StopSegmentInternal()
    {
        if (!segmentStarted) return;

        Debug.Log("Stopped segment recording");
        timelineManager.isRecordingSegment = false;
        jsonToLLM.isLogging = false;
        segmentStarted = false;
        
        recordingDot.SetActive(false);
        
        // Stop audio recording for host/laptop mode
        // NOTE: Audio recording is now handled by ChatBehaviour.cs for FactoryScenarioCreation mode
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost || gm.laptopMode)
        {
            if (recordAudio != null && Microphone.IsRecording(null) && 
                ScenarioTypeManager.Instance != null && 
                ScenarioTypeManager.Instance.currentScenario != ScenarioTypeManager.ScenarioType.FactoryScenarioCreation)
            {
                recordAudio.StopRecording();
                Debug.Log("Audio recording stopped with the segment.");
            }
            else if (ScenarioTypeManager.Instance != null && 
                     ScenarioTypeManager.Instance.currentScenario == ScenarioTypeManager.ScenarioType.FactoryScenarioCreation)
            {
                Debug.Log("FactoryScenarioCreation mode: Audio stop handled by ChatBehaviour");
            }
        }

        // Clean up UI
        GroundSelection groundSelection = GameObject.FindGameObjectWithTag("Ground")
            .GetComponent<GroundSelection>();
        groundSelection.ClearGroundHighlights();

        // Start file processing pipeline
        StartCoroutine(ChainedCoroutines());
    }

    #endregion

    #region Processing Pipeline Coroutines
    
    /// <summary>
    /// JSON processing coroutine - placeholder for future expansion
    /// Currently provides timing coordination between processing steps
    /// </summary>
    IEnumerator JSONCoroutine()
    {
        Debug.Log("Started JSON Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(1);
        yield return new WaitForSeconds(1);
    }

    /// <summary>
    /// Main coroutine chain coordinator
    /// Orchestrates the sequence of file processing operations
    /// </summary>
    IEnumerator ChainedCoroutines()
    {
        yield return FileCoroutine();
    }

    /// <summary>
    /// File processing coroutine that handles transcription, JSON creation, and video processing
    /// Coordinates complex synchronization between multiple systems and network clients
    /// </summary>
    IEnumerator FileCoroutine()
    {
        Debug.Log("Started File Coroutine at timestamp : " + Time.time);
        
        // Wait for transcription processing to complete
        yield return ProcessingTranscript();
        
#if UNITY_EDITOR
        // Write JSON file after transcription is complete
        jsonToLLM.WriteFile();
        Debug.Log("JSON file has been successfully written");
#endif
    
        // Reset JSON data for next recording
        ResetJsonData();
        
#if UNITY_EDITOR
        // Handle video recording stop and processing
        if (recorderManager != null && recorderManager.RecorderController != null)
        {
            Debug.Log("Now stopping video recording...");
            recorderManager.StopRecording();
            
            // Client-specific video processing in multiplayer mode
            if (!gameManager.laptopMode && gameManager._runner.IsClient)
            {
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
            
                // Clean up and notify server
                recorderManager.ReleaseRecorderResources();
                jsonToLLM.RPC_NotifyVideoSaveComplete();
                Debug.Log("CLIENT: Notified server that video save is complete");
            }
        }
#endif
    }

    /// <summary>
    /// Transcription processing coroutine with mode-specific handling
    /// Manages UI feedback and client-server synchronization for transcription completion
    /// </summary>
    private IEnumerator ProcessingTranscript()
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        
        // Laptop mode: Simple transcription waiting
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
        
        // Multiplayer host mode: Complex synchronization with clients
        if (gm.isHost && !gm.laptopMode)
        {
            jsonToLLM.isTranscriptionComplete = false;
            countdownText.gameObject.SetActive(true);
            countdownText.color = Color.red;

            string baseText = "TRANSCRIPTION PROCESSING \n(DO NOT RESTART YET)";
            int dotCount = 0;
            countdownText.fontSize = 40;

            // Wait for transcription processing
            while (!jsonToLLM.isTranscriptionComplete)
            {
                countdownText.text = $"{baseText}{new string('.', dotCount % 4)}";
                dotCount++;
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("SERVER: Transcription processing complete");
            
            // Wait for client data reception confirmation
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
            
            // Wait for client video save confirmation
            baseText = "WAITING FOR VIDEO SAVE \n(DO NOT RESTART YET)";
            dotCount = 0;

            startTime = Time.time;
            timeout = 30f;

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
            // Client mode: Wait for data reception and processing
            countdownText.gameObject.SetActive(true);
            countdownText.color = Color.red;
            countdownText.fontSize = 40;
        
            string chunkText = "RECEIVING DATA CHUNKS";
            int chunkDotCount = 0;
        
            float startTime = Time.time;
            float timeout = 30f;
        
            // Wait for both dictionary chunks and annotations
            while ((!jsonToLLM.AreAllChunksReceived() || !annotationManager.AreAnnotationsSynced()) && (Time.time - startTime < timeout))
            {
                if (!jsonToLLM.AreAllChunksReceived())
                {
                    chunkText = "RECEIVING DATA CHUNKS";
                }
                else if (!annotationManager.AreAnnotationsSynced())
                {
                    chunkText = "RECEIVING ANNOTATIONS";
                }
            
                countdownText.text = $"{chunkText} {new string('.', chunkDotCount % 4)}";
                chunkDotCount++;
                yield return new WaitForSeconds(0.5f);
            }
        
            countdownText.gameObject.SetActive(false);
        
            if (!jsonToLLM.AreAllChunksReceived() || !annotationManager.AreAnnotationsSynced())
            {
                Debug.LogError($"CLIENT: Timed out waiting for data! Chunks: " +
                               $"{jsonToLLM.totalChunksReceived}/{jsonToLLM.totalChunksSent}, Annotations synced: {annotationManager.AreAnnotationsSynced()}");
                // Notify server to prevent deadlock
                jsonToLLM.RPC_NotifyChunksReceived();
            }
            else
            {
                Debug.Log("CLIENT: All data successfully received");
                jsonToLLM.RPC_NotifyChunksReceived();
            }
        }
    }

    #endregion

    #region Event Handlers and Utilities
    
    /// <summary>
    /// Handle transcription completion event
    /// </summary>
    /// <param name="finalTranscription">The completed transcription text</param>
    public void OnTranscriptionFinished(string finalTranscription)
    {
        Debug.Log("Processed Transcription: " + finalTranscription);
        explanation = finalTranscription;
    }

    /// <summary>
    /// Execute an action with a delay to prevent rapid successive clicks
    /// </summary>
    /// <param name="handleClickAction">Action to execute after delay</param>
    public IEnumerator HandleClickWithDelay(Action handleClickAction)
    {
        canClick = false;
        handleClickAction();
        yield return new WaitForSeconds(0.5f);
        canClick = true;
    }

    #endregion

    #region Annotation and Interaction Handling
    
    /// <summary>
    /// Handle object annotation mode - allows clicking on objects to create annotations
    /// Uses raycasting to detect clicked objects and creates corresponding annotations
    /// </summary>
    public void HandleAnnotationMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
#if UNITY_ANDROID && !UNITY_EDITOR
        ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
#endif
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;
            annotationManager.CreateObjectClickAnnotation(clickedObject, segmentStartTime);
        }
    }

    /// <summary>
    /// Network RPC version of annotation mode handling
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HandleAnnotationMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
#if UNITY_ANDROID && !UNITY_EDITOR
        ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
#endif
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;
            annotationManager.CreateObjectClickAnnotation(clickedObject, segmentStartTime);
        }
    }

    /// <summary>
    /// Handle position annotation mode - allows clicking on ground positions
    /// Creates position markers and annotations for spatial references
    /// </summary>
    public void HandlePositionMode()
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

            annotationManager.CreatePositionClickAnnotation(clickedPosition, segmentStartTime);
        }
    }

    /// <summary>
    /// Network RPC version of position mode handling
    /// </summary>
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
            annotationManager.CreatePositionClickAnnotation(clickedPosition, segmentStartTime);
        }
    }

    #endregion

    #region Data Management
    
    /// <summary>
    /// Reset all JSON data and annotation state
    /// Chooses between local and networked reset based on game mode
    /// </summary>
    public void ResetJsonData()
    {   
        if (gameManager.laptopMode)
        {
            ResetJsonDataInternal();
        }
        else
        {
            RPC_ResetJsonData();
        }
    }
    
    /// <summary>
    /// Network RPC to reset JSON data on all clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetJsonData()
    {
        ResetJsonDataInternal();
    }

    /// <summary>
    /// Internal implementation of data reset
    /// Clears all collected annotation and segment data
    /// </summary>
    private void ResetJsonDataInternal()
    {
        annotationManager.annotation.Clear();
        annotationManager.annotationDescriptions.Clear();
        annotationManager.objectToKey.Clear();
        annotationManager.annotationTimes.Clear();
        annotationManager.clickOrder = 0;
        annotationManager.annotationsReady = false;
        jsonToLLM.ResetSegmentData();
    }

    #endregion
}