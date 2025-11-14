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

/// <summary>
/// Manages JSON data collection, transcription processing, and file output for demonstration recordings.
/// Handles both laptop mode (single player) and multiplayer networking with client-server synchronization.
/// Coordinates video recording, audio transcription, and annotation data into structured JSON format.
/// </summary>
public class JSONToLLM : NetworkBehaviour
{
    [Header("Input and Scene References")]
    public KeyboardInput keyboard;
    public ObjectsList objectsList;
    public TimelineManager timelineManager;
    public RecorderManager recorderManager;
    
    [Header("Component References")]
    private GameManager gameManager;
    private ProgramSynthesisManager programSynthesisManager;
    private AnnotationManager annotationManager;
    private Scribe scribe;
    
    [Header("File Output")]
    private string filename;
    public string jsonString;
    
    [Header("Timing and State")]
    public float time;  // Time index for capturing scene data
    public int recordingNum = -1; 
    public bool isLogging = false;
    public bool voiceActivated = false;
    public bool videoIsRecording;
    public bool loggingStartedByUnpause = false;
    
    [Header("Network Synchronization")]
    public NetworkBool isTranscriptionComplete = false;
    public bool laptopModeTranscriptionComplete = false;
    public int totalChunksSent = 0;
    public int totalChunksReceived = 0;
    private bool clientHasReceivedAllData = false;
    public bool clientVideoSaveComplete = false;
    
    [Header("Recording Mode")]
    [Tooltip("Used to record system jsons/videos")]
    public bool activateSystemRecording = false; 

    #region Data Structures for JSON Serialization
    
    /// <summary>
    /// Represents a 2D position in the game world
    /// </summary>
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

    /// <summary>
    /// Represents object orientation as an angle
    /// </summary>
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

    /// <summary>
    /// Represents 2D velocity vector
    /// </summary>
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

    /// <summary>
    /// Represents corner/boundary objects in the scene
    /// </summary>
    [System.Serializable]
    public class Corner
    {
        public string id;
        public string type = "Bound";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }

    /// <summary>
    /// Represents player objects with behavior and possession data
    /// </summary>
    [System.Serializable]
    public class Player
    {
        public string id;
        public string type;
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<bool> ballPossession = new List<bool>();
        public List<bool> handRaised = new List<bool>();
        public string behavior;
        public List<Orientation> orientation = new List<Orientation>();
    }

    /// <summary>
    /// Represents the ball object in the scene
    /// </summary>
    [System.Serializable]
    public class Ball
    {
        public string id;
        public string type = "Ball";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }

    /// <summary>
    /// Represents goal objects in the scene
    /// </summary>
    [System.Serializable]
    public class Goal
    {
        public string id;
        public string type = "Goal";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }

    /// <summary>
    /// Root container for a single segment's data
    /// </summary>
    [System.Serializable]
    public class RootSegment
    {
        public int timestep;
        public List<object> objects = new List<object>();
    }
    
    /// <summary>
    /// Object data for a specific timestep in rewindable format
    /// </summary>
    [System.Serializable]
    public class ObjectData
    {
        public string objectName; // e.g., "coach", "opponent"
        public Position pos;    
        public Orientation rot;      
        public string action;     // Action (e.g., "moveTo")
        public string behavior;   // Behavior (e.g., "ActionID1")
    }
    
    /// <summary>
    /// Timestep data for rewindable timeline
    /// </summary>
    [System.Serializable]
    public class TimeStep
    {
        public int timestep;              // Timestep number
        public List<ObjectData> objects;  // List of objects in this timestep
    }

    /// <summary>
    /// Represents a moment when time was stopped with narration
    /// </summary>
    [System.Serializable]
    public class StoppedTime
    {
        public int timestopped;  // Timestep when stopped
        public string narration; // Narration text
    }
    
    /// <summary>
    /// Alternative root segment format with rewindable timeline support
    /// </summary>
    [System.Serializable]
    public class RootSegment2
    {
        public List<TimeStep> Timeseries;      // List of timesteps
        public List<StoppedTime> StoppedTimes; // List of stopped times
    }

    #endregion

    [Header("Data Storage")]
    public RootSegment myRootSegment = new RootSegment();
    public RootSegment2 rewindableRootSegment = new RootSegment2();
    private List<RootSegment> segmentsList = new List<RootSegment>();

    // Key = timestamp float, Value = list of tokens (strings or placeholders)
    public Dictionary<float, List<object>> tokenDictionary = new Dictionary<float, List<object>>();

    /// <summary>
    /// Initialize component references and subscribe to transcription events
    /// </summary>
    void Start()
    {
        keyboard = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        
#if UNITY_EDITOR
        recorderManager = GameObject.FindGameObjectWithTag("RecorderManager").GetComponent<RecorderManager>();
#endif
        programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();
        annotationManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<AnnotationManager>();

        // Subscribe to transcription completion events
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

    /// <summary>
    /// Clean up event subscriptions
    /// </summary>
    private void OnDestroy()
    {
        if (scribe != null)
        {
            scribe.OnTranscriptionComplete -= HandleTranscriptionComplete;
        }
    }

    void Update()
    {
        // Per-frame logic can be added here if needed
    }

    /// <summary>
    /// Called in FixedUpdate when isLogging = true to capture scene data
    /// </summary>
    public void PopulateSegment()
    {
        PopulateSceneObjects();
        myRootSegment.timestep = timelineManager.TimeIndex;
    }
    
    /// <summary>
    /// Add a stopped time entry with narration to the rewindable segment
    /// </summary>
    public void AddStoppedTime()
    {
        programSynthesisManager.explanation = BuildSentenceFromTokens();
        
        StoppedTime stoppedTime = new StoppedTime
        {
            timestopped = timelineManager.RewindTimeIndex,
            narration = programSynthesisManager.explanation
        };
        rewindableRootSegment.StoppedTimes.Add(stoppedTime);
    }

    /// <summary>
    /// Get the correct name for an object, prioritizing PlayerInterface networked name
    /// </summary>
    private string GetObjectName(GameObject obj)
    {
        if (obj == null) return "";
        
        // Try to get networked name from PlayerInterface first
        var playerInterface = obj.GetComponent<PlayerInterface>();
        if (playerInterface != null && !string.IsNullOrEmpty(playerInterface.ObjName.ToString()))
        {
            return playerInterface.ObjName.ToString();
        }
        
        // Try GoalInterface
        var goalInterface = obj.GetComponent<GoalInterface>();
        if (goalInterface != null && !string.IsNullOrEmpty(goalInterface.ObjName.ToString()))
        {
            return goalInterface.ObjName.ToString();
        }
        
        // Try BallInterface
        var ballInterface = obj.GetComponent<BallInterface>();
        if (ballInterface != null && !string.IsNullOrEmpty(ballInterface.ObjName.ToString()))
        {
            return ballInterface.ObjName.ToString();
        }
        
        // Fall back to GameObject.name
        return obj.name;
    }

    /// <summary>
    /// Populate the current segment with scene object data
    /// Collects position, velocity, and state data for all tracked objects
    /// </summary>
    public void PopulateSceneObjects()
    {
        if (objectsList == null)
        {
            return;
        }

        if (ScenarioTypeManager.Instance.currentScenario == ScenarioTypeManager.ScenarioType.FactoryScenarioCreation)
        {
            foreach (GameObject player in objectsList.scenicPlayers)
            {
                string objectName = GetObjectName(player);
                AddOrUpdatePlayer(player, objectName);
            }
            foreach (GameObject obj in objectsList.scenicObjects)
            {
                string objectName = GetObjectName(obj);
                AddOrUpdatePlayer(obj, objectName);
            }
        }
        
        if (ScenarioTypeManager.Instance.currentScenario == ScenarioTypeManager.ScenarioType.Soccer)
        {
            // Process defense players
            foreach (GameObject currPlayer in objectsList.defensePlayers)
            {
                AddOrUpdatePlayer(currPlayer, "Teammate");
            }
        
            // Process offense players
            foreach (GameObject currPlayer in objectsList.offensePlayers)
            {
                AddOrUpdatePlayer(currPlayer, "Opponent");
            }
        
            // Process coach/human players
            foreach (GameObject humanPlayer in objectsList.humanPlayers)
            {
                AddOrUpdatePlayer(humanPlayer, "Coach", "expert");
            }

            // Process ball object
            var ballData = (Ball)myRootSegment.objects.Find(obj => obj is Ball);
            if (ballData == null)
            {
                ballData = new Ball { id = "ball" };
                myRootSegment.objects.Add(ballData);
            }

            // Process goal object
            Goal goalData2 = (Goal)myRootSegment.objects.Find(obj => obj is Goal g && g.id == "goal");
            if (goalData2 == null)
            {
                goalData2 = new Goal { id = "goal" };
                myRootSegment.objects.Add(goalData2);
            }
        }
    }
    
    /// <summary>
    /// Add or update player data in the current segment
    /// </summary>
    /// <param name="playerGO">Player GameObject to process</param>
    /// <param name="type">Player type (Teammate, Opponent, Coach) or object name for FactoryScenarioCreation</param>
    /// <param name="behaviorOverride">Optional behavior override</param>
    private void AddOrUpdatePlayer(GameObject playerGO, string type, string behaviorOverride = null)
    {
        // Get the correct object name (networked name if available)
        string objectName = GetObjectName(playerGO);
        
        var existingPlayer = (Player)myRootSegment.objects
            .Find(obj => obj is Player p && p.id == objectName);

        if (existingPlayer == null)
        {
            existingPlayer = new Player
            {
                id = objectName,
                behavior = (behaviorOverride != null) 
                    ? behaviorOverride
                    : playerGO.GetComponent<PlayerInterface>().behavior.Value,
                type = type
            };
            myRootSegment.objects.Add(existingPlayer);
        }
    }
    
    #region Transcription and Networking

    /// <summary>
    /// Handle completion of ElevenLabs transcription
    /// Merges transcription words with annotation placeholders based on timing
    /// </summary>
    private void HandleTranscriptionComplete(Scribe.ElevenLabsResponse response)
    {
        // Laptop mode: Simple approach without networking
        if (gameManager.laptopMode)
        {
            Debug.Log("JSONToLLM (Laptop Mode): Received transcription. Merging placeholders...");

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
                foreach (var pair in annotationManager.annotationTimes)
                {
                    float annotationTime = pair.Value;
                    int annotationIndex = pair.Key;

                    if (!tokenDictionary.ContainsKey(annotationTime))
                        tokenDictionary[annotationTime] = new List<object>();

                    tokenDictionary[annotationTime].Add($"[{annotationIndex}]");
                }
            }

            laptopModeTranscriptionComplete = true;
            isTranscriptionComplete = true;

            Debug.Log("Merged words + placeholders. Token dictionary:");
            foreach (var kvp in tokenDictionary.OrderBy(k => k.Key))
            {
                Debug.Log($"Time={kvp.Key:F2} => {string.Join(", ", kvp.Value)}");
            }
            return;
        }

        // Multiplayer mode: Use server-client synchronization
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
                foreach (var pair in annotationManager.annotationTimes)
                {
                    float annotationTime = pair.Value;
                    int annotationIndex = pair.Key;

                    if (!tokenDictionary.ContainsKey(annotationTime))
                        tokenDictionary[annotationTime] = new List<object>();

                    tokenDictionary[annotationTime].Add($"[{annotationIndex}]");
                }
            }

            Debug.Log("Merged words + placeholders. Token dictionary:");
            foreach (var kvp in tokenDictionary.OrderBy(k => k.Key))
            {
                Debug.Log($"Time={kvp.Key:F2} => {string.Join(", ", kvp.Value)}");
            }
            
            SyncDictionaryToClients();
        }
    }
    
    /// <summary>
    /// RPC for clients to notify server that video save is complete
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_NotifyVideoSaveComplete()
    {
        if (Runner.IsServer)
        {
            clientVideoSaveComplete = true;
            Debug.Log("SERVER: Received notification that client has saved video");
        }
    }
    
    /// <summary>
    /// Check if client has completed video saving
    /// </summary>
    public bool HasClientSavedVideo()
    {
        return gameManager.laptopMode ? true : clientVideoSaveComplete;
    }

    /// <summary>
    /// Reset client video save status for next recording
    /// </summary>
    public void ResetClientVideoSaveStatus()
    {
        clientVideoSaveComplete = false;
    }

    /// <summary>
    /// RPC for clients to notify server that all data chunks have been received
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_NotifyChunksReceived()
    {
        if (Runner.IsServer)
        {
            clientHasReceivedAllData = true;
            Debug.Log("SERVER: Received notification that client has all chunks");
        }
    }
    
    /// <summary>
    /// Check if client has received all data chunks
    /// </summary>
    public bool HasClientReceivedAllData()
    {
        return gameManager.laptopMode ? true : clientHasReceivedAllData;
    }

    /// <summary>
    /// Reset client receive status for next recording session
    /// </summary>
    public void ResetClientReceiveStatus()
    {
        clientHasReceivedAllData = false;
        clientVideoSaveComplete = false;
    }
    
    /// <summary>
    /// Synchronize token dictionary from server to all clients
    /// Breaks data into chunks to handle network message size limits
    /// </summary>
    private void SyncDictionaryToClients()
    {
        RPC_ClearDictionary();
        
        // Convert dictionary to network-serializable format
        List<KeyValueData> allData = new List<KeyValueData>();
        
        foreach (var kvp in tokenDictionary)
        {
            foreach (var token in kvp.Value)
            {
                KeyValueData data = new KeyValueData();
                data.TimeKey = kvp.Key;
                
                if (token.ToString().StartsWith("[") && token.ToString().EndsWith("]"))
                {
                    // Handle annotation placeholders
                    string indexStr = token.ToString().Trim('[', ']');
                    if (int.TryParse(indexStr, out int index))
                    {
                        data.IsPlaceholder = 1;
                        data.PlaceholderIndex = index;
                    }
                }
                else
                {
                    // Handle text tokens
                    data.IsPlaceholder = 0;
                    data.TextToken = token.ToString();
                }
                
                allData.Add(data);
            }
        }
        
        // Chunk the data for network transmission
        const int CHUNK_SIZE = 20;
        totalChunksSent = Mathf.CeilToInt((float)allData.Count / CHUNK_SIZE);
        
        RPC_SetExpectedChunkCount(totalChunksSent);
        
        for (int i = 0; i < allData.Count; i += CHUNK_SIZE)
        {
            int currentChunkSize = Mathf.Min(CHUNK_SIZE, allData.Count - i);
            KeyValueData[] chunk = new KeyValueData[currentChunkSize];
            
            for (int j = 0; j < currentChunkSize; j++)
            {
                chunk[j] = allData[i + j];
            }
            
            RPC_ReceiveDictionaryChunk(chunk);
        }
        
        // Sync annotations after dictionary
        annotationManager.SyncAnnotationsToClients();
        
        isTranscriptionComplete = true;
        Debug.Log("SERVER: All chunks sent, marked transcription as complete");
    }
    
    /// <summary>
    /// RPC to set expected chunk count on clients
    /// </summary>
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
    
    /// <summary>
    /// Check if all data chunks have been received
    /// </summary>
    public bool AreAllChunksReceived()
    {
        if (gameManager.laptopMode)
        {
            return laptopModeTranscriptionComplete;
        }
        if (Runner.IsServer)
        {
            return isTranscriptionComplete;
        }
        
        return totalChunksReceived >= totalChunksSent && totalChunksSent > 0;
    }
    
    /// <summary>
    /// Network serializable struct for token dictionary data
    /// </summary>
    public struct KeyValueData : INetworkStruct
    {
        public float TimeKey;
        public byte IsPlaceholder; // 0 = text, 1 = placeholder
        
        [Networked, Capacity(64)]
        public NetworkString<_64> TextToken { get; set; }
        
        public int PlaceholderIndex;
    }
    
    /// <summary>
    /// RPC to clear client token dictionaries
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClearDictionary()
    {
        if (Runner.IsClient)
        {
            tokenDictionary.Clear();
        }
    }
    
    /// <summary>
    /// RPC to receive dictionary data chunks on clients
    /// </summary>
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
                    tokenDictionary[timeKey].Add($"[{data.PlaceholderIndex}]");
                }
                else
                {
                    tokenDictionary[timeKey].Add(data.TextToken.ToString());
                }
            }
            
            totalChunksReceived++;
            Debug.Log($"Received chunk {totalChunksReceived}/{totalChunksSent}");
            
            // Check if all chunks received
            if (totalChunksReceived >= totalChunksSent && totalChunksSent > 0)
            {
                Debug.Log("CLIENT: All dictionary chunks received, notifying server");
                RPC_NotifyChunksReceived();
                isTranscriptionComplete = true;
            }
        }
    }
    
    /// <summary>
    /// RPC to signal dictionary synchronization completion
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FinishDictionarySync()
    {
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

    #endregion

    #region File Operations

    /// <summary>
    /// Write the collected data to JSON file
    /// Ensures all chunks are received before writing in multiplayer mode
    /// </summary>
    public void WriteFile()
    {
        if (!gameManager.laptopMode && !AreAllChunksReceived())
        {
            Debug.LogError($"Warning: Writing file before all chunks received! ({totalChunksReceived}/{totalChunksSent})");
        }
    
        Debug.Log($"Creating JSON with tokenDictionary containing {tokenDictionary.Count} entries");
        CreateJSONString();
    }

    /// <summary>
    /// Create and serialize the complete JSON structure for the demonstration
    /// Combines scene data, transcription tokens, annotations, and metadata
    /// </summary>
    public void CreateJSONString()
    {
        recordingNum++;
        JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        
        var sortedTokens = tokenDictionary
            .OrderBy(kvp => kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        programSynthesisManager.explanation = BuildSentenceFromTokens();
    
        object finalScene = new
        {
            scene = new
            {
                id = jsonDirectory.drillID,
                language = programSynthesisManager.explanation,
                step = 0.02,
                objects = myRootSegment.objects,
                annotations = annotationManager.GetAnnotationsAsJson(),
                tokens = sortedTokens,
                clickTimes = annotationManager.annotationTimes
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

    /// <summary>
    /// Build a complete sentence from the token dictionary
    /// Concatenates all tokens in chronological order with proper spacing
    /// </summary>
    private string BuildSentenceFromTokens()
    {
        var sortedTokens = tokenDictionary.OrderBy(kvp => kvp.Key);
    
        StringBuilder sb = new StringBuilder();
        bool lastWasSpace = false;
        
        foreach (var kvp in sortedTokens)
        {
            foreach (var tokenObj in kvp.Value)
            {
                string token = tokenObj.ToString();
                
                // Check if this is an annotation marker like [0], [1], etc.
                bool isAnnotationMarker = token.StartsWith("[") && token.EndsWith("]") && 
                                         token.Length > 2 && token.Length <= 5;
                
                // Add space before token if:
                // - StringBuilder is not empty
                // - Last character wasn't already a space
                // - This token doesn't start with punctuation or is an annotation marker
                if (sb.Length > 0 && !lastWasSpace && !token.StartsWith(" "))
                {
                    // Don't add space before certain punctuation
                    if (!token.StartsWith(",") && !token.StartsWith(".") && 
                        !token.StartsWith("!") && !token.StartsWith("?") &&
                        !token.StartsWith(";") && !token.StartsWith(":"))
                    {
                        sb.Append(" ");
                    }
                }
                
                sb.Append(token);
                lastWasSpace = token.EndsWith(" ");
            }
        }
    
        return sb.ToString().Trim();
    }

    #endregion

    #region State Management

    /// <summary>
    /// Prepare system for a new recording session
    /// Resets all state and synchronization data
    /// </summary>
    public void PrepareForNewRecording()
    {
        ResetSegmentData();
        if (!gameManager.laptopMode)
        {
            ResetClientReceiveStatus();
        }
        Debug.Log("Prepared for new recording session");
    }
    
    /// <summary>
    /// Reset all segment data and networking state
    /// Clears collected data and prepares for new recording
    /// </summary>
    public void ResetSegmentData()
    {
        // Reset segment data
        myRootSegment = new RootSegment();
        time = 0;
        tokenDictionary.Clear();
        programSynthesisManager.explanation = "";
    
        // Reset network synchronization state
        totalChunksSent = 0;
        totalChunksReceived = 0;
        isTranscriptionComplete = false;
        laptopModeTranscriptionComplete = false;
        clientHasReceivedAllData = false;
    
        // Reset recording state
        videoIsRecording = false;
        isLogging = false;
    
        // Sync reset to clients in multiplayer mode
        if (!gameManager.laptopMode && Runner != null && Runner.IsServer)
        {
            RPC_ResetNetworkSync();
        }
    
        Debug.Log("Segment data has been reset completely.");
    }
    
    /// <summary>
    /// RPC to reset network synchronization state on all clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetNetworkSync()
    {
        totalChunksSent = 0;
        totalChunksReceived = 0;
        isTranscriptionComplete = false;
    
        if (Runner.IsClient)
        {
            myRootSegment = new RootSegment();
            time = 0;
            tokenDictionary.Clear();
            Debug.Log("CLIENT: Network sync state reset complete");
        }
    }

    #endregion

    /// <summary>
    /// Main update loop for data collection and video recording
    /// Handles automatic logging and video recording based on system state
    /// </summary>
    void FixedUpdate()
    {
        // Start logging for system recording when segment begins
        if (activateSystemRecording && programSynthesisManager.segmentStarted)
        {
            isLogging = true;
            Debug.Log("started logging");
        }
        
        if (isLogging)
        {
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
    }
}