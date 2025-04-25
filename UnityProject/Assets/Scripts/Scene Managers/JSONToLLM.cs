using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq; // For .OrderBy(...)
using System.Text;
using Fusion;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.VisualScripting.Antlr3.Runtime;
using Whisper.Samples;

public class JSONToLLM : NetworkBehaviour
{
    public KeyboardInput keyboard;
    public ObjectsList objectsList;
    private string filename;
    public string jsonString;
    public TimelineManager timelineManager;
    
    // So we can start/stop the video in FixedUpdate
    public RecorderManager recorderManager;

    public float time;  // Time index for capturing scene data
    public NetworkBool isTranscriptionComplete = false;
    public bool isLogging = false;

    public int recordingNum = -1; 
    public bool voiceActivated = false;
    public bool videoIsRecording;
    public bool loggingStartedByUnpause = false;

    [Tooltip("Used to record system jsons/videos")]
    public bool activateSystemRecording = false; 

    [System.Serializable]
    public class Position
    {
        public float x;
        public float y;
        public Position(Vector3 vector)
        {
            x = vector.x;
            y = vector.z;
        }
    }

    [System.Serializable]
    public class Orientation
    {
        public float angle; 
        public Orientation(Transform transform)
        {
            angle = transform.rotation.eulerAngles.y;
        }
        public Orientation(Quaternion quaternion)
        {
            angle = quaternion.eulerAngles.y;
        }
    }

    [System.Serializable]
    public class Velocity
    {
        public float x;
        public float y;
        public Velocity(Vector3 vector)
        {
            x = vector.x;
            y = vector.z;
        }
    }

    [System.Serializable]
    public class Corner
    {
        public string id;
        public string type = "Bound";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }

    [System.Serializable]
    public class Player
    {
        public string id;
        public string type;
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<bool> ballPossession = new List<bool>();
        public string behavior;
        public List<Orientation> orientation = new List<Orientation>();
    }

    [System.Serializable]
    public class Ball
    {
        public string id;
        public string type = "Ball";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }

    [System.Serializable]
    public class Goal
    {
        public string id;
        public string type = "Goal";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }

    [System.Serializable]
    public class RootSegment
    {
        public int timestep;
        public List<object> objects = new List<object>();
    }
    
    [System.Serializable]
    public class ObjectData
    {
        public string objectName; // e.g., "coach", "opponent"
        public Position pos;    
        public Orientation rot;      
        public string action;     // Action (e.g., "moveTo")
        public string behavior;   // Behavior (e.g., "ActionID1")
    }
    
    [System.Serializable]
    public class TimeStep
    {
        public int timestep;              // Timestep number
        public List<ObjectData> objects;  // List of objects in this timestep
    }

    [System.Serializable]
    public class StoppedTime
    {
        public int timestopped;  // Timestep when stopped
        public string narration; // Narration text
    }
    
    [System.Serializable]
    public class RootSegment2
    {
        public List<TimeStep> Timeseries;      // List of timesteps
        public List<StoppedTime> StoppedTimes; // List of stopped times
    }


    public RootSegment myRootSegment = new RootSegment();
    public RootSegment2 rewindableRootSegment = new RootSegment2();
    private List<RootSegment> segmentsList = new List<RootSegment>();

    // Key = timestamp float, Value = list of tokens (strings or placeholders)
    public Dictionary<float, List<object>> tokenDictionary = new Dictionary<float, List<object>>();

    private Scribe scribe;

    void Start()
    {
        keyboard = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        
#if UNITY_EDITOR
        // The RecorderManager is presumably on a GameObject tagged "RecorderManager"
        recorderManager = GameObject.FindGameObjectWithTag("RecorderManager").GetComponent<RecorderManager>();
#endif

        scribe = FindObjectOfType<Scribe>();
        if (scribe != null)
        {
            scribe.OnTranscriptionComplete += HandleTranscriptionComplete;
        }
        else
        {
            Debug.LogError("JSONToLLM: No Scribe found—cannot subscribe to OnTranscriptionComplete.");
        }
    }

    private void OnDestroy()
    {
        if (scribe != null)
        {
            scribe.OnTranscriptionComplete -= HandleTranscriptionComplete;
        }
    }

    void Update()
    {
        // any per-frame logic here
    }

    // Called in FixedUpdate if isLogging = true
    public void PopulateSegment()
    {
        // Debug.LogError("in populate segment");
        PopulateSceneObjects();
        myRootSegment.timestep = timelineManager.TimeIndex;
    }
    
    public void PopulateSegment2()
    {
        rewindableRootSegment.Timeseries = new List<TimeStep>();

        // Populate Timeseries
        
        // Determine the maximum number of timesteps
        int maxTimesteps = timelineManager.Timeseries.Values
            .Select(ts => ts.positions.Count)
            .DefaultIfEmpty(0)
            .Max();
        
        // Iterate over each timestep
        for (int timeIndex = 0; timeIndex < maxTimesteps; timeIndex++)
        {
            TimeStep timeStep = new TimeStep
            {
                timestep = timeIndex,
                objects = new List<ObjectData>()
            };

            // Iterate over each GameObject in Timeseries
            foreach (var entry in timelineManager.Timeseries)
            {
                GameObject obj = entry.Key;
                RewindableTimeSeries timeSeries = entry.Value;

                if (timeIndex < timeSeries.positions.Count)
                {
                    Vector3 position = timeSeries.positions[timeIndex];
                    Quaternion rotation = timeSeries.rotations[timeIndex];

                    ObjectData objectData = new ObjectData
                    {
                        objectName = obj.name,
                        pos = new Position(position),
                        rot = new Orientation(rotation),
                        action = timeSeries.currAction,
                        behavior = timeSeries.currBehavior
                    };

                    timeStep.objects.Add(objectData);
                }
            }

            rewindableRootSegment.Timeseries.Add(timeStep);
        }
    }
    
    public void AddStoppedTime()
    {
        keyboard.explanation = BuildSentenceFromTokens();
        
        StoppedTime stoppedTime = new StoppedTime
        {
            timestopped = timelineManager.RewindTimeIndex,
            narration = keyboard.explanation
        };
        rewindableRootSegment.StoppedTimes.Add(stoppedTime);
    }

    public void PopulateSceneObjects()
    {
        // Debug.LogError("in populate scene objects");
        if (objectsList == null)
        {
            return;
        }
        
        // corners
        // for (int i = 1; i <= 4; i++)
        // {
        //     GameObject cornerGO = GameObject.Find("corner" + i);
        //     if (cornerGO != null)
        //     {
        //         var cornerData = (Corner)myRootSegment.objects
        //             .Find(obj => obj is Corner c && c.id == "corner" + i);
        //
        //         if (cornerData == null)
        //         {
        //             cornerData = new Corner { id = "corner" + i };
        //             myRootSegment.objects.Add(cornerData);
        //         }
        //
        //         cornerData.position.Add(new Position(cornerGO.transform.position));
        //         cornerData.velocity.Add(new Velocity(Vector3.zero));
        //         cornerData.orientation.Add(new Orientation(cornerGO.transform));
        //     }
        // }
        
        // defense players
        foreach (GameObject currPlayer in objectsList.defensePlayers)
        {
            AddOrUpdatePlayer(currPlayer, "Teammate");
        }
        // offense players
        foreach (GameObject currPlayer in objectsList.offensePlayers)
        {
            AddOrUpdatePlayer(currPlayer, "Opponent");
        }
        // coach/human players
        foreach (GameObject humanPlayer in objectsList.humanPlayers)
        {
            AddOrUpdatePlayer(humanPlayer, "Coach", "expert");
        }

        // ball
        var ballData = (Ball)myRootSegment.objects.Find(obj => obj is Ball);
        if (ballData == null)
        {
            ballData = new Ball { id = "ball" };
            myRootSegment.objects.Add(ballData);
        }
        
        if (objectsList.ballObject != null)
        {
            var ballGO = objectsList.ballObject;

            ballData.position.Add(new Position(ballGO.transform.position));
            ballData.orientation.Add(new Orientation(ballGO.transform));
            Rigidbody ballRB = ballGO.GetComponent<Rigidbody>();
            ballData.velocity.Add(new Velocity(ballRB.velocity));
        }

        // goal
        Goal goalData2 = (Goal)myRootSegment.objects.Find(obj => obj is Goal g && g.id == "goal");
        if (goalData2 == null)
        {
            goalData2 = new Goal { id = "goal" };
            myRootSegment.objects.Add(goalData2);
        }

        if (objectsList.goalObject != null)
        {
            var goalGO = objectsList.goalObject;
            
            Vector3 zeroVector = Vector3.zero;
            goalData2.velocity.Add(new Velocity(zeroVector));
            goalData2.position.Add(new Position(goalGO.transform.position));
            goalData2.orientation.Add(new Orientation(goalGO.transform));
            
            Transform leftPost = goalGO.transform.Find("goal_leftpost");
            Transform rightPost = goalGO.transform.Find("goal_rightpost");
            
            Goal leftGoalPost = (Goal)myRootSegment.objects
                .Find(obj => obj is Goal g && g.id == "goal_leftpost");
            if (leftGoalPost == null)
            {
                leftGoalPost = new Goal { id = "goal_leftpost", type = "Goal" };
                myRootSegment.objects.Add(leftGoalPost);
            }
            leftGoalPost.position.Add(new Position(leftPost.position));
            leftGoalPost.velocity.Add(new Velocity(Vector3.zero));
            leftGoalPost.orientation.Add(new Orientation(leftPost));

            Goal rightGoalPost = (Goal)myRootSegment.objects
                .Find(obj => obj is Goal g && g.id == "goal_rightpost");
            if (rightGoalPost == null)
            {
                rightGoalPost = new Goal { id = "goal_rightpost", type = "Goal" };
                myRootSegment.objects.Add(rightGoalPost);
            }
            rightGoalPost.position.Add(new Position(rightPost.position));
            rightGoalPost.velocity.Add(new Velocity(Vector3.zero));
            rightGoalPost.orientation.Add(new Orientation(rightPost));
        }
    }
    
    private void AddOrUpdatePlayer(GameObject playerGO, string type, string behaviorOverride = null)
    {
        var existingPlayer = (Player)myRootSegment.objects
            .Find(obj => obj is Player p && p.id == playerGO.name);

        if (existingPlayer == null)
        {
            existingPlayer = new Player
            {
                id = playerGO.name,
                behavior = (behaviorOverride != null) 
                    ? behaviorOverride
                    : playerGO.GetComponent<PlayerInterface>().behavior.Value,
                type = type
            };
            myRootSegment.objects.Add(existingPlayer);
        }
        
        Transform _transform = playerGO.transform;
        if (type == "Coach" && playerGO.GetComponent<HumanInterface>().isVR)
        {
            _transform = playerGO.GetComponent<HumanInterface>().vrTransform;
        }

        existingPlayer.position.Add(new Position(_transform.position));
        
        Vector3 velocity = (type == "Coach")
            ? keyboard.movement
            : playerGO.GetComponent<PlayerInterface>().currVelocity;

        if (type == "Coach" && playerGO.GetComponent<HumanInterface>().isVR)
        {
            velocity = playerGO.GetComponent<HumanInterface>().velocity;
        }
        
        existingPlayer.velocity.Add(new Velocity(velocity));
        existingPlayer.orientation.Add(new Orientation(_transform));

        bool hasBall = false;
        if (type == "Coach")
        {
            hasBall = playerGO.GetComponent<HumanInterface>().ballPossession;
        }
        else
        {
            hasBall = playerGO.GetComponent<PlayerInterface>().ballPossession;
        }
        existingPlayer.ballPossession.Add(hasBall);
    }
    
    
    // Called when ElevenLabs transcription is done
    private void HandleTranscriptionComplete(Scribe.ElevenLabsResponse response)
    {
        if (Runner.IsServer)
        {
            Debug.Log("JSONToLLM: Received transcription. Merging placeholders...");

            tokenDictionary.Clear();
            if (response.words != null)
            {
                foreach (var w in response.words)
                {
                    float timeKey = w.start;
                    if (!tokenDictionary.ContainsKey(timeKey))
                        tokenDictionary[timeKey] = new List<object>();
                    tokenDictionary[timeKey].Add(w.text);
                }
            }

            if (keyboard != null)
            {
                // Insert bracket placeholders
                foreach (var pair in keyboard.annotationTimes)
                {
                    float annotationTime = pair.Value;
                    int annotationIndex = pair.Key;

                    if (!tokenDictionary.ContainsKey(annotationTime))
                        tokenDictionary[annotationTime] = new List<object>();

                    tokenDictionary[annotationTime].Add($"[{annotationIndex}]");
                }
            }

            // isTranscriptionComplete = true;

            Debug.Log("Merged words + placeholders. Token dictionary:");
            foreach (var kvp in tokenDictionary.OrderBy(k => k.Key))
            {
                Debug.Log($"Time={kvp.Key:F2} => {string.Join(", ", kvp.Value)}");
            }
            
            SyncDictionaryToClients();
        }
    }
    
    // Add a new property to track how many chunks were sent by the server
    public int totalChunksSent = 0;
    // Add a property to track how many chunks were received by the client
    public int totalChunksReceived = 0;
    
    private bool clientHasReceivedAllData = false;
    public bool clientVideoSaveComplete = false;
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_NotifyVideoSaveComplete()
    {
        // Only the server will execute this code
        if (Runner.IsServer)
        {
            clientVideoSaveComplete = true;
            Debug.Log("SERVER: Received notification that client has saved video");
        }
    }
    
    public bool HasClientSavedVideo()
    {
        return clientVideoSaveComplete;
    }

    public void ResetClientVideoSaveStatus()
    {
        clientVideoSaveComplete = false;
    }


    // Add this method for clients to notify the server
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_NotifyChunksReceived()
    {
        // Only the server will execute this code
        if (Runner.IsServer)
        {
            clientHasReceivedAllData = true;
            Debug.Log("SERVER: Received notification that client has all chunks");
        }
    }
    
    // Add this method on the server to check client status
    public bool HasClientReceivedAllData()
    {
        return clientHasReceivedAllData;
    }

    // Add this to reset state between recordings
    public void ResetClientReceiveStatus()
    {
        clientHasReceivedAllData = false;
        clientVideoSaveComplete = false;
    }
    
    private void SyncDictionaryToClients()
    {
        // First, clear the clients' dictionaries
        RPC_ClearDictionary();
        
        // Convert dictionary to a format we can send
        List<KeyValueData> allData = new List<KeyValueData>();
        
        foreach (var kvp in tokenDictionary)
        {
            foreach (var token in kvp.Value)
            {
                KeyValueData data = new KeyValueData();
                data.TimeKey = kvp.Key;
                
                if (token.ToString().StartsWith("[") && token.ToString().EndsWith("]"))
                {
                    // It's a placeholder
                    string indexStr = token.ToString().Trim('[', ']');
                    if (int.TryParse(indexStr, out int index))
                    {
                        data.IsPlaceholder = 1;
                        data.PlaceholderIndex = index;
                    }
                }
                else
                {
                    // It's a text token
                    data.IsPlaceholder = 0;
                    data.TextToken = token.ToString();
                }
                
                allData.Add(data);
            }
        }
        
        // Now actually chunk the data and send it
        const int CHUNK_SIZE = 20; // Adjust based on token size
        totalChunksSent = Mathf.CeilToInt((float)allData.Count / CHUNK_SIZE);
        
        // Send the total number of chunks to expect
        RPC_SetExpectedChunkCount(totalChunksSent);
        
        for (int i = 0; i < allData.Count; i += CHUNK_SIZE)
        {
            // Get the current chunk
            int currentChunkSize = Mathf.Min(CHUNK_SIZE, allData.Count - i);
            KeyValueData[] chunk = new KeyValueData[currentChunkSize];
            
            for (int j = 0; j < currentChunkSize; j++)
            {
                chunk[j] = allData[i + j];
            }
            
            // Send this chunk
            RPC_ReceiveDictionaryChunk(chunk);
        }
        
        // Now sync the annotations
        keyboard.SyncAnnotationsToClients();
        
        // Mark transcription as complete only after both dictionary and annotations are synced
        isTranscriptionComplete = true;
        Debug.Log("SERVER: All chunks sent, marked transcription as complete");
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetExpectedChunkCount(int count)
    {
        if (Runner.IsClient)
        {
            totalChunksReceived = 0;
            totalChunksSent = count;
            Debug.Log($"Client expecting {count} dictionary chunks");
        }
    }
    
    public bool AreAllChunksReceived()
    {
        if (Runner.IsServer)
        {
            return isTranscriptionComplete; // Server is done once it sends all chunks
        }
        
        return totalChunksReceived >= totalChunksSent && totalChunksSent > 0;
    }
    
    // Struct to hold key-value data that Fusion can serialize
    public struct KeyValueData : INetworkStruct
    {
        public float TimeKey;
        public byte IsPlaceholder; // 0 = text, 1 = placeholder
        
        // For text tokens
        [Networked, Capacity(64)]
        public NetworkString<_64> TextToken { get; set; }
        
        // For placeholder tokens
        public int PlaceholderIndex;
    }
    
    // RPC to clear the dictionary
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClearDictionary()
    {
        if (Runner.IsClient)
        {
            tokenDictionary.Clear();
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReceiveDictionaryChunk(KeyValueData[] chunk)
    {
        if (Runner.IsClient)
        {
            foreach (var data in chunk)
            {
                float timeKey = data.TimeKey;
            
                if (!tokenDictionary.ContainsKey(timeKey))
                {
                    tokenDictionary[timeKey] = new List<object>();
                }
            
                if (data.IsPlaceholder == 1)
                {
                    // It's a placeholder
                    tokenDictionary[timeKey].Add($"[{data.PlaceholderIndex}]");
                }
                else
                {
                    // It's a text token
                    tokenDictionary[timeKey].Add(data.TextToken.ToString());
                }
            }
            
            totalChunksReceived++;
            Debug.Log($"Received chunk {totalChunksReceived}/{totalChunksSent}");
            
            // Check if all chunks have been received
            if (totalChunksReceived >= totalChunksSent && totalChunksSent > 0)
            {
                Debug.Log("CLIENT: All dictionary chunks received, notifying server");
                RPC_NotifyChunksReceived(); // Notify the server
                isTranscriptionComplete = true;
            }
        }
        
    }
    
    // RPC to signal dictionary sync is complete
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FinishDictionarySync()
    {
        // Log completion if we're a client
        if (Runner.IsClient)
        {
            Debug.Log("Token dictionary sync complete. Entries:");
            foreach (var kvp in tokenDictionary.OrderBy(k => k.Key))
            {
                Debug.Log($"Time={kvp.Key:F2} => {string.Join(", ", kvp.Value)}");
            }
        }
        
        isTranscriptionComplete = true;
    }

    public void WriteFile()
    {
        if (!AreAllChunksReceived())
        {
            Debug.LogError($"Warning: Writing file before all chunks received! ({totalChunksReceived}/{totalChunksSent})");
        }
    
        Debug.Log($"Creating JSON with tokenDictionary containing {tokenDictionary.Count} entries");
        CreateJSONString();
    }
    
    public void WriteFile2()
    {
        CreateJSONString2();
    }

    public void CreateJSONString()
    {
        recordingNum++;
        JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        
        var sortedTokens = tokenDictionary
            .OrderBy(kvp => kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        keyboard.explanation = BuildSentenceFromTokens();
    
        object finalScene;
      
            finalScene = new
            {
                scene = new
                {
                    id = jsonDirectory.drillID,
                    language = keyboard.explanation,
                    step = 0.02,
                    objects = myRootSegment.objects,
                    annotations = keyboard.GetAnnotationsAsJson(),
                    tokens = sortedTokens,
                    clickTimes = keyboard.annotationTimes
                }
            };

        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        jsonString = JsonConvert.SerializeObject(finalScene, settings);
        filename = jsonDirectory.InstantiateJSONSegmentFilePath(recordingNum) + ".json";
        File.WriteAllText(filename, jsonString);

        Debug.Log($"Segment written to {filename}");
    }
    
    public void CreateJSONString2()
    {
        PopulateSegment2();
        AddStoppedTime();
        
        JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        object finalScene;
      
        finalScene = new
        {
            scene = new
            {
                TimeSeries = rewindableRootSegment.Timeseries,
                StoppedTimes = rewindableRootSegment.StoppedTimes
            }
        };
        
        jsonString = JsonConvert.SerializeObject(finalScene, settings);
        filename = jsonDirectory.InstantiateJSONEditPath() + ".json";
        File.WriteAllText(filename, jsonString);

        Debug.Log($"Edited recording written to {filename}");
    }

    
    private string BuildSentenceFromTokens()
    {
        var sortedTokens = tokenDictionary.OrderBy(kvp => kvp.Key);
    
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in sortedTokens)
        {
            foreach (var tokenObj in kvp.Value)
            {
                sb.Append(tokenObj.ToString());
            }
        }
    
        // Optionally, trim any extra whitespace
        return sb.ToString().Trim();
    }
    
    public void PrepareForNewRecording()
    {
        // Reset any state needed for a new recording
        ResetSegmentData();
        ResetClientReceiveStatus(); // Make sure this gets called
        Debug.Log("Prepared for new recording session");
    }
    
    public void ResetSegmentData()
    {
        // Reset segment data
        myRootSegment = new RootSegment();
        time = 0;
        tokenDictionary.Clear();
        keyboard.explanation = "";
    
        // Reset network synchronization state
        totalChunksSent = 0;
        totalChunksReceived = 0;
        isTranscriptionComplete = false;
        clientHasReceivedAllData = false; // Make sure this gets reset!
    
        // Reset recording state
        videoIsRecording = false;
        isLogging = false;
    
        // Sync reset to clients
        RPC_ResetNetworkSync();
    
        Debug.Log("Segment data has been reset completely.");
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetNetworkSync()
    {
        // For both server and client
        totalChunksSent = 0;
        totalChunksReceived = 0;
        isTranscriptionComplete = false;
    
        // Client-specific reset
        if (Runner.IsClient)
        {
            myRootSegment = new RootSegment();
            time = 0;
            tokenDictionary.Clear();
        
            // Notify server that reset is complete
            Debug.Log("CLIENT: Network sync state reset complete");
        }
    }

    void FixedUpdate()
    {
      
        if (activateSystemRecording && keyboard.segmentStarted) // if system recording and segment started, we want to start logging
        {
            isLogging = true;
            Debug.Log("started logging");
        }
        
        if (isLogging)
        {
            // Debug.Log("JSON LOGGING");
            time += 0.02f;
#if UNITY_EDITOR
            PopulateSegment();
            if (!recorderManager.RecorderController.IsRecording())
            {
                recorderManager.StartRecording();
                videoIsRecording = recorderManager.RecorderController.IsRecording();
            }
#endif
        }
        else if (!isLogging)
        {
#if UNITY_EDITOR
            // being handled in keyboardinput.FileCoroutine() now
            // if (recorderManager.RecorderController.IsRecording())
            // {
            //     recorderManager.StopRecording();
            //     videoIsRecording = recorderManager.RecorderController.IsRecording();
            // }
#endif
        }
    }

}
