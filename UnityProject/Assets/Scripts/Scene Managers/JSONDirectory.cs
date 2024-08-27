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
    
    private DirectoryInfo drillFolder;
    private DirectoryInfo demoFolder;
    private DirectoryInfo videoFolder;
    private DirectoryInfo jsonSegmentFolder;
    private KeyboardInput keyboardInput;
    private bool initialDemo;

    public bool InitialDemo
    {
        get => initialDemo;
        set => initialDemo = value;
    }
    
    public enum Drills
    {
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

        string usableDemosFile = drillFolder.FullName + "/usable_demonstrations.json";
        
        File.WriteAllText(usableDemosFile, usableDemosString);
    }

    public void InstantiateInitialFolders()
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
        } else if (drillFolder.Exists)
        {
            drillFolder.Delete(true);
            drillFolder.Create();
        }
    }
    public void InstantiateDemoFolders()
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
        }
        
        demoFolder = 
            new DirectoryInfo(Path.Combine(drillFolder.FullName, "demonstration" + demoNum.ToString()));
        if (!demoFolder.Exists)
        {
            demoFolder.Create();
        } else if (demoFolder.Exists)
        {
            demoFolder.Delete(true);
            demoFolder.Create();
        }
        
        videoFolder = 
            new DirectoryInfo(Path.Combine(demoFolder.FullName, "videos"));
        if (!videoFolder.Exists)
        {
            videoFolder.Create();
        } else if (videoFolder.Exists)
        {
            videoFolder.Delete(true);
            videoFolder.Create();
        }
        
        jsonSegmentFolder = 
            new DirectoryInfo(Path.Combine(demoFolder.FullName, "json_segments"));
        if (!jsonSegmentFolder.Exists)
        {
            jsonSegmentFolder.Create();
        } else if (jsonSegmentFolder.Exists)
        {
            jsonSegmentFolder.Delete(true);
            jsonSegmentFolder.Create();
        }
        
    }
    
    public string InstantiateVideoFilePath(int recordingNum)
    {
        DirectoryInfo videoFile = 
            new DirectoryInfo(Path.Combine(videoFolder.FullName, ParticipantID + "_" + DemoNum + "_" + "segment" + recordingNum.ToString()));

        return videoFile.FullName;
    }
    
    public string InstantiateJSONSegmentFilePath(int recordingNum)
    {
        DirectoryInfo jsonSegmentFile = 
            new DirectoryInfo(Path.Combine(jsonSegmentFolder.FullName, ParticipantID + "_" + DemoNum + "_" + "segment" + recordingNum.ToString()));

        return jsonSegmentFile.FullName;
    }

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
        yield return null;
        keyboardInput.saveDemoCanvas.SetActive(false);
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
        jsonToLLM.recordingNum = 0;
        jsonToLLM.time = 0;
        
        RecorderManager recorderManager = GameObject.FindGameObjectWithTag("RecorderManager").GetComponent<RecorderManager>();
        recorderManager.recordingNum = 0;
    }
}
