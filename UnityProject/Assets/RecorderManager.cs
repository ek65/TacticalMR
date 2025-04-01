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
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);

        // Video settings
        m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        m_Settings.name = "My Video Recorder";
        m_Settings.Enabled = true;
        m_Settings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        m_Settings.VideoBitRateMode = VideoBitrateMode.Medium;

        m_Settings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1200,
            OutputHeight = 1080
        };

        m_Settings.AudioInputSettings.PreserveAudio = m_RecordAudio;

        // Setup recording
        controllerSettings.AddRecorderSettings(m_Settings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 60.0f;
    }

    public void StartRecording()
    {
        recordingNum++;

        JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        m_Settings.OutputFile = jsonDirectory.InstantiateVideoFilePath(recordingNum);

        RecorderOptions.VerboseMode = false;
        m_RecorderController.PrepareRecording();
        m_RecorderController.StartRecording();

        Debug.Log($"Started recording for file {OutputFile.FullName}");
    }

    public void StopRecording()
    {
        m_RecorderController.StopRecording();
    }

    void OnDisable()
    {
        StopRecording();
    }
#endif
}
