using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.OVR.Scripts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

public class JSONDirectory : MonoBehaviour
{
    public int participantID;
    public string ParticipantID => "participant" + participantID.ToString();
    public Drills drillID;
    public int demoNum = -1;
    public string DemoNum => "demo" + demoNum.ToString();
    public int transcriptNum = -1; // only used for system recording
    
    private DirectoryInfo drillFolder;
    private DirectoryInfo demoFolder;
    private DirectoryInfo videoFolder;
    private DirectoryInfo jsonSegmentFolder;
    private KeyboardInput keyboardInput;
    private JSONToLLM jsonToLLM;
    private bool initialDemo = true;

    private DirectoryInfo systemTranscriptsFolder;

    public bool InitialDemo
    {
        get => initialDemo;
        set => initialDemo = value;
    }
    
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
    
    [System.Serializable]
    public class UsableDemos
    {
        public List<int> demoNums = new List<int>();
    }
    
    private UsableDemos usableDemos = new UsableDemos();

    private void Start()
    {
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
    }

    public void AddAndSaveDemo()
    {
        if (!usableDemos.demoNums.Contains(demoNum))
        {
            usableDemos.demoNums.Add(demoNum);
            WriteUsableDemosFile();
        }
    }
    
    public void IncrementDemoNum()
    {
        demoNum++;
    }
    
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

    public void InstantiateInitialFolders()
    {
        if (!jsonToLLM.activateSystemRecording)
        {
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
                // } else if (drillFolder.Exists)
                // {
                //     drillFolder.Delete(true);
                //     drillFolder.Create();
                // }
            }
        }
        else if (jsonToLLM.activateSystemRecording)
        {
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

    // UI element prompt
    public void SaveDemonstrationButton()
    {
        AddAndSaveDemo();
        
        // when finished prompting the user, unpause then restart the scenario
        StartCoroutine(UnpauseAndEndScenario());
    }

    public void DoNotSaveDemonstrationButton()
    {
        // when finished prompting the user, unpause then restart the scenario
        StartCoroutine(UnpauseAndEndScenario());
    }
    
    private IEnumerator UnpauseAndEndScenario()
    {
        JSONToLLM jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        keyboardInput.RPC_CanvasSetActive(false);
        while (!jsonToLLM.isTranscriptionComplete) // Wait until transcription is done
        {
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(0.5f);
        // adding this reset here in case, it also resets in ObjectsList.cs in Reset()
        keyboardInput.timelineManager.Unpause();
        keyboardInput.exitScenario.EndScenario();
        keyboardInput.canClick = true;
        keyboardInput.restarting = false;
        keyboardInput.timelineManager.Reset();
        HumanInterface humanInterface = GameObject.FindGameObjectWithTag("human").GetComponent<HumanInterface>();
        humanInterface.ResetHuman();
    }

    public void ResetRecordingNum()
    {
        JSONToLLM jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        jsonToLLM.recordingNum = -1;
        jsonToLLM.time = 0;
        
        // RecorderManager recorderManager = GameObject.FindGameObjectWithTag("RecorderManager").GetComponent<RecorderManager>();
        // recorderManager.recordingNum = -1;
    }
}
