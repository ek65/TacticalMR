using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.OVR.Scripts;
using Fusion;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

/// <summary>
/// Manages directory structure and file paths for JSON data storage and demonstration recording.
/// Handles creation of folder hierarchies for participants, drills, demonstrations, and system recordings.
/// Supports both participant-specific recording and system-wide recording modes.
/// </summary>
public class JSONDirectory : NetworkBehaviour
{
    [Header("Participant and Demo Configuration")]
    public int participantID;
    public string drillID = "Test";
    public int demoNum = -1;
    public int transcriptNum = -1; // only used for system recording
    
    [Header("Directory Management")]
    private DirectoryInfo drillFolder;
    private DirectoryInfo demoFolder;
    private DirectoryInfo videoFolder;
    private DirectoryInfo jsonSegmentFolder;
    private DirectoryInfo systemTranscriptsFolder;
    
    [Header("Component References")]
    private ProgramSynthesisManager programSynthesisManager;
    private JSONToLLM jsonToLLM;
    
    [Header("State Management")]
    private bool initialDemo = true;
    
    // Properties for consistent naming conventions
    public string ParticipantID => "participant" + participantID.ToString();
    public string DemoNum => "demo" + demoNum.ToString();

    public bool InitialDemo
    {
        get => initialDemo;
        set => initialDemo = value;
    }
    
    /// <summary>
    /// Enumeration of available drill types for organizing demonstrations
    /// </summary>
    public enum Drills
    {
        Test,
        
        // Defense Drills
        OneVSOneDefensePress,
        OneVSOneDefensePressPositioningPatience,
        OneVSTwoDefense,
        ThreeAttackersTwoDefenders1,
        ThreeAttackersTwoDefenders2,
        TwoVSTwoDefense,
        
        // Offense Drills
        MidfielderSetandThrough1,
        MidfielderSetandThrough2,
        HowToPlayOutFromTheBack1,
        HowToPlayOutFromTheBack2,
        HowToPlayOutFromTheBack3,
        CreatingSpace,
        ThroughBallPassing1,
        ThroughBallPassing2,
        TriangleDrill,
        DefendingAgainstOverlap,
        DefendingAsABackFour
    }
    
    /// <summary>
    /// Serializable class to track which demonstrations are marked as usable
    /// </summary>
    [System.Serializable]
    public class UsableDemos
    {
        public List<int> demoNums = new List<int>();
    }
    
    private UsableDemos usableDemos = new UsableDemos();

    /// <summary>
    /// Initialize component references
    /// </summary>
    private void Start()
    {
        programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
    }

    /// <summary>
    /// Public method to add current demo to usable list and save to file
    /// </summary>
    public void AddAndSaveDemo()
    {
        RPC_AddAndSaveDemo();
    }
    
    /// <summary>
    /// Network RPC to synchronize demo addition across all clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AddAndSaveDemo()
    {
        if (!usableDemos.demoNums.Contains(demoNum))
        {
            usableDemos.demoNums.Add(demoNum);
            WriteUsableDemosFile();
        }
    }
    
    /// <summary>
    /// Increment the demo number for the next demonstration
    /// </summary>
    public void IncrementDemoNum()
    {
        demoNum++;
    }
    
    /// <summary>
    /// Serialize and write the usable demonstrations list to JSON file
    /// File location depends on whether system recording is active
    /// </summary>
    public void WriteUsableDemosFile()
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        string usableDemosString = JsonConvert.SerializeObject(usableDemos, settings);
        Debug.Log("Created Usable Demos JSON String: " + usableDemosString);

        string usableDemosFile = null;
        if (!jsonToLLM.activateSystemRecording)
        {
            usableDemosFile = drillFolder.FullName + "/usable_demonstrations.json";
        }
        else if (jsonToLLM.activateSystemRecording)
        {
            usableDemosFile = systemTranscriptsFolder.FullName + "/usable_demonstrations.json";
        }
        
        File.WriteAllText(usableDemosFile, usableDemosString);
    }

    /// <summary>
    /// Create the initial folder structure based on recording mode
    /// Creates either participant-specific folders or system recording folders
    /// </summary>
    public void InstantiateInitialFolders()
    {
        if (!jsonToLLM.activateSystemRecording)
        {
            // Create participant-specific folder structure
            DirectoryInfo directoryOutputFolder = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "..", "output"));
            if (!directoryOutputFolder.Exists)
            {
                directoryOutputFolder.Create();
            }
        
            DirectoryInfo participantFolder = 
                new DirectoryInfo(Path.Combine(directoryOutputFolder.FullName, "participant" + participantID.ToString()));
            if (!participantFolder.Exists)
            {
                participantFolder.Create();
            }
        
            drillFolder = 
                new DirectoryInfo(Path.Combine(participantFolder.FullName, drillID.ToString()));
            if (!drillFolder.Exists)
            {
                drillFolder.Create();
            }
        }
        else if (jsonToLLM.activateSystemRecording)
        {
            // Create system recording folder structure
            DirectoryInfo directoryOutputFolder = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "..", "output"));
            if (!directoryOutputFolder.Exists)
            {
                directoryOutputFolder.Create();
            }
            
            DirectoryInfo systemFolder = 
                new DirectoryInfo(Path.Combine(directoryOutputFolder.FullName, "system_recordings"));
            if (!systemFolder.Exists)
            {
                systemFolder.Create();
            }
            systemTranscriptsFolder = 
                new DirectoryInfo(Path.Combine(systemFolder.FullName, "transcript" + transcriptNum.ToString()));
            if (!systemTranscriptsFolder.Exists)
            {
                systemTranscriptsFolder.Create();
            }
        }
    }

    /// <summary>
    /// Create demonstration-specific folders and auto-increment demo number
    /// Creates folder structure for videos and JSON segments within each demo
    /// </summary>
    public void InstantiateDemoFolders()
    {
        // Create or verify the top-level output folder
        DirectoryInfo directoryOutputFolder = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "..", "output"));
        if (!directoryOutputFolder.Exists)
        {
            directoryOutputFolder.Create();
        }
        
        if (!jsonToLLM.activateSystemRecording)
        {
            // Participant recording mode
            DirectoryInfo participantFolder = 
                new DirectoryInfo(Path.Combine(directoryOutputFolder.FullName, "participant" + participantID.ToString()));
            if (!participantFolder.Exists)
            {
                participantFolder.Create();
            }
        
            drillFolder = new DirectoryInfo(Path.Combine(participantFolder.FullName, drillID.ToString()));
            if (!drillFolder.Exists)
            {
                drillFolder.Create();
            }

            // Auto-increment demo number to avoid overwriting existing demonstrations
            int nextDemoIndex = 0;
            while (Directory.Exists(Path.Combine(drillFolder.FullName, "demonstration" + nextDemoIndex)))
            {
                nextDemoIndex++;
            }
            demoNum = nextDemoIndex;
            demoFolder = new DirectoryInfo(Path.Combine(drillFolder.FullName, "demonstration" + demoNum));
            demoFolder.Create();
            videoFolder = new DirectoryInfo(Path.Combine(demoFolder.FullName, "videos"));
            videoFolder.Create();
            jsonSegmentFolder = new DirectoryInfo(Path.Combine(demoFolder.FullName, "json_segments"));
            jsonSegmentFolder.Create();
        }
        else
        {
            // System recording mode
            DirectoryInfo systemFolder = 
                new DirectoryInfo(Path.Combine(directoryOutputFolder.FullName, "system_recordings"));
            if (!systemFolder.Exists)
            {
                systemFolder.Create();
            }

            systemTranscriptsFolder = 
                new DirectoryInfo(Path.Combine(systemFolder.FullName, "transcript" + transcriptNum.ToString()));
            if (!systemTranscriptsFolder.Exists)
            {
                systemTranscriptsFolder.Create();
            }
            
            // Auto-increment demo number for system recordings
            int nextDemoIndex = 0;
            while (Directory.Exists(Path.Combine(systemTranscriptsFolder.FullName, "demonstration" + nextDemoIndex)))
            {
                nextDemoIndex++;
            }
            demoNum = nextDemoIndex;
            
            demoFolder = new DirectoryInfo(Path.Combine(systemTranscriptsFolder.FullName, "demonstration" + demoNum));
            demoFolder.Create();
            
            videoFolder = new DirectoryInfo(Path.Combine(demoFolder.FullName, "videos"));
            videoFolder.Create();
            jsonSegmentFolder = new DirectoryInfo(Path.Combine(demoFolder.FullName, "json_segments"));
            jsonSegmentFolder.Create();
        }

        Debug.Log($"Created new folder: {demoFolder.FullName}");
    }
    
    /// <summary>
    /// Generate file path for video recordings with appropriate naming convention
    /// </summary>
    /// <param name="recordingNum">The segment/recording number</param>
    /// <returns>Full file path for the video file</returns>
    public string InstantiateVideoFilePath(int recordingNum)
    {
        Debug.Log(videoFolder.FullName);
        Debug.Log(ParticipantID);
        Debug.Log(DemoNum);
        DirectoryInfo videoFile = 
            new DirectoryInfo(Path.Combine(videoFolder.FullName, ParticipantID + "_" + DemoNum + "_" + "segment" + recordingNum.ToString()));
        if (jsonToLLM.activateSystemRecording)
        {
            videoFile = 
                new DirectoryInfo(Path.Combine(videoFolder.FullName, "transcript" + transcriptNum + "_" + DemoNum + "_" + "segment" + recordingNum.ToString()));
        }

        return videoFile.FullName;
    }
    
    /// <summary>
    /// Generate file path for JSON segment files with appropriate naming convention
    /// </summary>
    /// <param name="recordingNum">The segment/recording number</param>
    /// <returns>Full file path for the JSON segment file</returns>
    public string InstantiateJSONSegmentFilePath(int recordingNum)
    {
        DirectoryInfo jsonSegmentFile = 
            new DirectoryInfo(Path.Combine(jsonSegmentFolder.FullName, ParticipantID + "_" + DemoNum + "_" + "segment" + recordingNum.ToString()));
        if (jsonToLLM.activateSystemRecording)
        {
            jsonSegmentFile = 
                new DirectoryInfo(Path.Combine(jsonSegmentFolder.FullName, "transcript" + transcriptNum + "_" + DemoNum + "_" + "segment" + recordingNum.ToString()));
        }

        return jsonSegmentFile.FullName;
    }

    /// <summary>
    /// UI callback for saving current demonstration
    /// Adds demo to usable list and triggers scenario end sequence
    /// </summary>
    public void SaveDemonstrationButton()
    {
        AddAndSaveDemo();
        
        // when finished prompting the user, unpause then restart the scenario
        StartCoroutine(UnpauseAndEndScenario());
    }

    /// <summary>
    /// UI callback for not saving current demonstration
    /// Triggers scenario end sequence without saving
    /// </summary>
    public void DoNotSaveDemonstrationButton()
    {
        // when finished prompting the user, unpause then restart the scenario
        StartCoroutine(UnpauseAndEndScenario());
    }
    
    /// <summary>
    /// Coroutine to handle the sequence of operations when ending a scenario
    /// Waits for transcription completion, cleans up UI, and resets the system
    /// </summary>
    private IEnumerator UnpauseAndEndScenario()
    {
        JSONToLLM jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        programSynthesisManager.RPC_CanvasSetActive(false);
        
        yield return new WaitForSeconds(0.5f);
        
        // Clear ground highlights and reset system state
        GroundSelection groundSelection = GameObject.FindGameObjectWithTag("Ground")
            .GetComponent<GroundSelection>();
        groundSelection.ClearGroundHighlights();
        
        // Reset timeline and scenario state
        programSynthesisManager.timelineManager.Unpause();
        programSynthesisManager.exitScenario.EndScenario();
        programSynthesisManager.canClick = true;
        programSynthesisManager.restarting = false;
        programSynthesisManager.timelineManager.Reset();
        programSynthesisManager.ResetJsonData();
        
        // Reset human interface
        HumanInterface humanInterface = GameObject.FindGameObjectWithTag("human").GetComponent<HumanInterface>();
        humanInterface.ResetHuman();
    }

    /// <summary>
    /// Reset recording-related counters and timers
    /// </summary>
    public void ResetRecordingNum()
    {
        JSONToLLM jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        jsonToLLM.time = 0;
    }
}