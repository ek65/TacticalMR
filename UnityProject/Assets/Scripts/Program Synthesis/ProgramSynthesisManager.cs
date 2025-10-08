using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Meta.WitAi.Windows;
using OpenAI.Samples.Chat;
using TMPro;
using UnityEngine;


/*
 * Contains functions related to generating to creating the JSON for program synthesis,
 * such as recording, pausing/unpausing, and annotating.
 * JSON is constructed in JSONToLLM.cs
 */
public class ProgramSynthesisManager : NetworkBehaviour
{
    [Header("Scene References")]
    public ExitScenario exitScenario;
    public TimelineManager timelineManager;
    private JSONToLLM jsonToLLM;
    private ChatBehaviour chatBehaviour;
    private JSONDirectory jsonDirectory;
    private RecorderManager recorderManager;
    private GameManager gameManager;
    private AnnotationManager annotationManager;

    [Header("UI / Output")]
    public TextMeshProUGUI countdownText;
    public GameObject saveDemoCanvas;

    [Header("Audio Recording")]
    public RecordAudio recordAudio;

    [Header("Segment / Logging State")]
    public bool segmentStarted = false;
    public bool activationConditionMet = false;
    public bool restarting = false;
    public bool canClick = true;
    public float segmentStartTime = 0f;
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
        annotationManager = this.gameObject.GetComponent<AnnotationManager>();

        Debug.Log("KeyboardInput script initialized");
    }

    void Update()
    {
        if (exitScenario == null)
        {
            GameObject human = GameObject.FindGameObjectWithTag("human");
            if (human != null)
                exitScenario = human.GetComponent<ExitScenario>();
        }
    }
    
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
                annotationManager.CreatePauseActionAnnotation(segmentStartTime);
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
    
    public void OnTranscriptionFinished(string finalTranscription)
    {
        Debug.Log("Processed Transcription: " + finalTranscription);
        explanation = finalTranscription;
    }

    public IEnumerator HandleClickWithDelay(Action handleClickAction)
    {
        canClick = false;
        handleClickAction();
        yield return new WaitForSeconds(0.5f);
        canClick = true;
    }
    
    
    public void HandleAnnotationMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
#if UNITY_ANDROID && !UNITY_EDITOR
        ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
#endif
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;

            annotationManager.CreateObjectClickAnnotation(clickedObject, segmentStartTime);
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
            
            annotationManager.CreateObjectClickAnnotation(clickedObject, segmentStartTime);
        }
    }

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
            
            annotationManager.CreatePositionClickAnnotation(clickedPosition, segmentStartTime);
        }
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
        annotationManager.annotation.Clear();
        annotationManager.annotationDescriptions.Clear();
        annotationManager.objectToKey.Clear();
        annotationManager.annotationTimes.Clear();
        annotationManager.clickOrder = 0;
        annotationManager.annotationsReady = false;
        jsonToLLM.ResetSegmentData();
    }
}
