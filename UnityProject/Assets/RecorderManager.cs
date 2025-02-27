// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Recorder;
// using System.ComponentModel;
// using System.IO;
// using UnityEditor;
// using UnityEditor.Recorder;
// using UnityEditor.Recorder.Input;
//
// public class RecorderManager : MonoBehaviour
// {
//     private RecorderController m_RecorderController;
//     public RecorderController RecorderController => m_RecorderController;
//     
//     public bool m_RecordAudio = true;
//     internal MovieRecorderSettings m_Settings = null;
//     public int recordingNum = -1;
//
//     public FileInfo OutputFile
//     {
//         get
//         {
//             var fileName = m_Settings.OutputFile + ".mp4";
//             return new FileInfo(fileName);
//         }
//     }
//
//     void OnEnable()
//     {
//         Initialize();
//     }
//
//     public void Initialize()
//     {
//         var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
//         m_RecorderController = new RecorderController(controllerSettings);
//
//         // Video
//         m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
//         m_Settings.name = "My Video Recorder";
//         m_Settings.Enabled = true;
//
//         // This example performs an MP4 recording
//         m_Settings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
//         m_Settings.VideoBitRateMode = VideoBitrateMode.High;
//
//         m_Settings.ImageInputSettings = new GameViewInputSettings
//         {
//             OutputWidth = 1080,
//             OutputHeight = 1920
//         };
//
//         m_Settings.AudioInputSettings.PreserveAudio = m_RecordAudio;
//
//         // Setup Recording
//         controllerSettings.AddRecorderSettings(m_Settings);
//         controllerSettings.SetRecordModeToManual();
//         controllerSettings.FrameRate = 60.0f;
//     }
//
//     public void StartRecording()
//     {
//         // var mediaOutputFolder = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "SampleRecordings"));
//         // Debug.LogError(mediaOutputFolder.FullName);
//         // Simple file name (no wildcards) so that FileInfo constructor works in OutputFile getter.
//         // m_Settings.OutputFile = mediaOutputFolder.FullName + "/" + "video" + recordingNum.ToString();
//         recordingNum++;
//         
//         JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
//         m_Settings.OutputFile = jsonDirectory.InstantiateVideoFilePath(recordingNum);
//         
//         RecorderOptions.VerboseMode = false;
//         m_RecorderController.PrepareRecording();
//         m_RecorderController.StartRecording();
//
//         Debug.Log($"Started recording for file {OutputFile.FullName}");
//     }
//     
//     public void StopRecording()
//     {
//         m_RecorderController.StopRecording();
//     }
//     
//     void OnDisable()
//     {
//         StopRecording();
//     }
//     
// }