using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Recorder;
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
    private bool initialized = false;

    public FileInfo OutputFile
    {
        get
        {
            if (m_Settings == null || string.IsNullOrEmpty(m_Settings.OutputFile))
            {
                Debug.LogWarning("RecorderManager: OutputFile is null or empty.");
                return null;
            }

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
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);

        // Video settings
        m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        m_Settings.name = "My Video Recorder";
        m_Settings.Enabled = true;
        m_Settings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        m_Settings.VideoBitRateMode = VideoBitrateMode.High;

        m_Settings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };

        m_Settings.AudioInputSettings.PreserveAudio = m_RecordAudio;

        controllerSettings.AddRecorderSettings(m_Settings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 60.0f;

        initialized = true;
    }

    public void StartRecording()
    {
        if (!initialized)
        {
            Debug.LogWarning("RecorderManager: Not initialized yet. Delaying StartRecording.");
            StartCoroutine(DelayedStart());
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("RecorderManager: GameObject not active. Cannot start recording.");
            return;
        }

        StartCoroutine(DoStartRecording());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(DoStartRecording());
    }

    private IEnumerator DoStartRecording()
    {
        yield return new WaitForSeconds(0.5f);

        if (m_RecorderController == null || m_Settings == null)
        {
            Debug.LogError("RecorderManager: Controller or settings null after delay.");
            yield break;
        }

        if (m_RecorderController.IsRecording())
        {
            Debug.LogWarning("RecorderManager: Already recording.");
            yield break;
        }

        recordingNum++;

        JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager")?.GetComponent<JSONDirectory>();
        if (jsonDirectory == null)
        {
            Debug.LogError("RecorderManager: JSONDirectory not found.");
            yield break;
        }

        string path = jsonDirectory.InstantiateVideoFilePath(recordingNum);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("RecorderManager: Video file path returned null or empty. Aborting recording.");
            yield break;
        }

        m_Settings.OutputFile = path;

        try
        {
            RecorderOptions.VerboseMode = false;
            m_RecorderController.PrepareRecording();
            m_RecorderController.StartRecording();
            Debug.Log($"Started recording for file {OutputFile?.FullName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RecorderManager: Failed to start recording: {e}");
        }
    }

    public void StopRecording()
    {
        if (m_RecorderController != null && m_RecorderController.IsRecording())
        {
            m_RecorderController.StopRecording();
        }
    }

    void OnDisable()
    {
        StopRecording();
    }
#endif
}