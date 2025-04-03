using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
#endif

public class RecorderManager : MonoBehaviour
{
#if UNITY_EDITOR
    private RecorderController m_RecorderController;
    public RecorderController RecorderController => m_RecorderController;

    public bool m_RecordAudio = true;
    internal MovieRecorderSettings m_Settings = null;
    public int recordingNum = -1;
    
    public bool IsRecordingProcessing { get; set; } = false;

    public FileInfo OutputFile
    {
        get
        {
            var fileName = m_Settings.OutputFile + ".mp4";
            return new FileInfo(fileName);
        }
    }

    void OnEnable()
    {
        Initialize();
    }
    public void Initialize()
    {
        // Clean up previous controller if it exists
        if (m_RecorderController != null)
        {
            // Make sure it's not recording
            if (m_RecorderController.IsRecording())
            {
                m_RecorderController.StopRecording();
            }
        
            // We want to clear the reference, so let's set it to null
            // The garbage collector should handle the rest
            m_RecorderController = null;
        }
        
        // Create new controller settings and controller
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);

        // Video settings
        m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        m_Settings.name = "My Video Recorder";
        m_Settings.Enabled = true;
        m_Settings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        m_Settings.VideoBitRateMode = VideoBitrateMode.Low;

        m_Settings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1200,
            OutputHeight = 1080
        };

        m_Settings.AudioInputSettings.PreserveAudio = m_RecordAudio;

        // Setup recording
        controllerSettings.AddRecorderSettings(m_Settings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 5.0f;
        
        // Make sure to set IsRecordingProcessing to false
        IsRecordingProcessing = false;
    }

    public void StartRecording()
    {
        recordingNum++;

        JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        m_Settings.OutputFile = jsonDirectory.InstantiateVideoFilePath(recordingNum);

        RecorderOptions.VerboseMode = false;
        m_RecorderController.PrepareRecording();
        m_RecorderController.StartRecording();
        
        IsRecordingProcessing = true;

        Debug.Log($"Started recording for file {OutputFile.FullName}");
    }

    public void StopRecording()
    {
        if (m_RecorderController.IsRecording())
        {
            m_RecorderController.StopRecording();
            StartCoroutine(WaitForFileCreation());
        }
        else
        {
            IsRecordingProcessing = false;
        }
    }
    
    private IEnumerator WaitForFileCreation()
    {
#if UNITY_EDITOR
        // Wait a small initial amount to let Unity start saving the file
        yield return new WaitForSeconds(0.5f);
    
        string filePath = OutputFile.FullName;
        float startTime = Time.time;
        float timeout = 15f; // Maximum wait time in seconds
    
        Debug.Log($"Waiting for file to be created: {filePath}");
    
        // Wait for the file to exist and have a non-zero size
        while (Time.time - startTime < timeout)
        {
            if (File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 0)
                {
                    Debug.Log($"Video file created successfully: {filePath} ({fileInfo.Length} bytes)");
                    IsRecordingProcessing = false;
                    yield break;
                }
            }
        
            yield return new WaitForSeconds(0.5f);
        }
    
        // If we reach here, we timed out
        Debug.LogError($"Timed out waiting for video file to be created: {filePath}");
        IsRecordingProcessing = false;
#endif
    }


    void OnDisable()
    {
        StopRecording();
        IsRecordingProcessing = false;
    }
    
    public void ReleaseRecorderResources()
    {
#if UNITY_EDITOR
        // Dispose of the current recorder controller to ensure clean state
        if (m_RecorderController != null)
        {
            m_RecorderController.StopRecording();
        }
    
        // Re-initialize to create fresh controller
        Initialize();
        IsRecordingProcessing = false;
    
        Debug.Log("RecorderManager resources released and reinitialized");
#endif
    }
    
#endif
}
