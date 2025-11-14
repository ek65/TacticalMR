using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages event annotations for AI analysis and program synthesis.
/// Tracks player actions, interactions, and user inputs to create comprehensive annotations
/// that can be used for machine learning and behavioral analysis.
/// Handles network synchronization of annotations across multiplayer sessions.
/// </summary>
public class AnnotationManager : NetworkBehaviour
{
    #region Component References
    [Header("Scene References")]
    private JSONToLLM jsonToLLM;
    private GameManager gameManager;
    private ProgramSynthesisManager programSynthesisManager;
    #endregion

    #region Annotation Storage
    [Header("Annotations")]
    /// <summary>
    /// Main annotation dictionary mapping annotation IDs to annotation data
    /// </summary>
    public Dictionary<int, object> annotation = new Dictionary<int, object>();
    
    /// <summary>
    /// Human-readable descriptions for each annotation
    /// </summary>
    public Dictionary<int, string> annotationDescriptions = new Dictionary<int, string>();
    
    /// <summary>
    /// Maps GameObjects to their annotation keys for quick lookup
    /// </summary>
    public Dictionary<GameObject, int> objectToKey = new Dictionary<GameObject, int>();
    
    /// <summary>
    /// Stores the timestamp for each annotation relative to segment start
    /// </summary>
    public Dictionary<int, float> annotationTimes = new Dictionary<int, float>();
    
    /// <summary>
    /// Sequential ID counter for annotation ordering
    /// </summary>
    public int clickOrder = 0;
    
    /// <summary>
    /// Whether annotations have been synchronized across clients
    /// </summary>
    public bool annotationsReady = false;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        InitializeComponents();
        Debug.Log("AnnotationManager script initialized");
    }

    void Update()
    {
        
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes component references
    /// </summary>
    private void InitializeComponents()
    {
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        programSynthesisManager = this.gameObject.GetComponent<ProgramSynthesisManager>();
    }
    #endregion

    #region Network Synchronization Structures
    /// <summary>
    /// Network-serializable structure for transmitting annotation data across clients
    /// </summary>
    public struct AnnotationData : INetworkStruct
    {
        public int AnnotationID;
        public byte AnnotationType; // 0 = GameObject, 1 = Vector3, 2 = Dictionary
    
        // For Vector3 type annotations
        public float PositionX;
        public float PositionY;
        public float PositionZ;
    
        // For annotation description
        [Networked, Capacity(128)]
        public NetworkString<_128> Description { get; set; }
    
        // For annotation time
        public float AnnotationTime;
    
        // For GameObject and Dictionary types - Object name/type
        [Networked, Capacity(64)]
        public NetworkString<_64> ObjectName { get; set; }
    
        // For Dictionary type additional data
        [Networked, Capacity(64)]
        public NetworkString<_64> DictType { get; set; }
    
        [Networked, Capacity(64)]
        public NetworkString<_64> DictFrom { get; set; }
    
        [Networked, Capacity(64)]
        public NetworkString<_64> DictTo { get; set; }
    
        // For dictionary point coordinates
        public float PointX;
        public float PointY;
    }
    #endregion

    #region Network Synchronization
    /// <summary>
    /// Synchronizes all annotations from server to clients in chunks to handle network limitations
    /// </summary>
    public void SyncAnnotationsToClients()
    {
        if (gameManager.laptopMode || !Runner.IsServer) return;
        
        RPC_ClearAnnotations();
        
        List<AnnotationData> allAnnotations = new List<AnnotationData>();
        
        // Convert all annotations to network-friendly format
        foreach (var entry in annotation)
        {
            int id = entry.Key;
            object value = entry.Value;
            string description = annotationDescriptions.ContainsKey(id) ? annotationDescriptions[id] : "";
            float time = annotationTimes.ContainsKey(id) ? annotationTimes[id] : 0f;
            
            AnnotationData data = CreateAnnotationData(id, value, description, time);
            allAnnotations.Add(data);
        }
        
        // Send annotations in manageable chunks
        SendAnnotationChunks(allAnnotations);
        
        RPC_FinishAnnotationSync();
        
        Debug.Log($"SERVER: Sent {allAnnotations.Count} annotations to clients");
    }

    /// <summary>
    /// Creates network annotation data from local annotation entry
    /// </summary>
    private AnnotationData CreateAnnotationData(int id, object value, string description, float time)
    {
        AnnotationData data = new AnnotationData();
        data.AnnotationID = id;
        data.Description = description;
        data.AnnotationTime = time;
        
        if (value is GameObject go)
        {
            data.AnnotationType = 0; // GameObject
            data.ObjectName = go.name;
        }
        else if (value is Vector3 vector)
        {
            data.AnnotationType = 1; // Vector3
            data.PositionX = vector.x;
            data.PositionY = vector.y;
            data.PositionZ = vector.z;
        }
        else if (value is Dictionary<string, object> dictValue)
        {
            data.AnnotationType = 2; // Dictionary
            PopulateDictionaryData(ref data, dictValue);
        }
        
        return data;
    }

    /// <summary>
    /// Populates dictionary-specific fields in annotation data
    /// </summary>
    private void PopulateDictionaryData(ref AnnotationData data, Dictionary<string, object> dictValue)
    {
        if (dictValue.ContainsKey("type"))
            data.DictType = dictValue["type"].ToString();
            
        if (dictValue.ContainsKey("player") || dictValue.ContainsKey("from"))
            data.DictFrom = dictValue.ContainsKey("player") ? dictValue["player"].ToString() : dictValue["from"].ToString();
            
        if (dictValue.ContainsKey("to"))
        {
            if (dictValue["to"] is Dictionary<string, float> pointDict)
            {
                data.PointX = pointDict.ContainsKey("x") ? pointDict["x"] : 0f;
                data.PointY = pointDict.ContainsKey("y") ? pointDict["y"] : 0f;
            }
            else
            {
                data.DictTo = dictValue["to"].ToString();
            }
        }
    }

    /// <summary>
    /// Sends annotation data in chunks to avoid network packet size limits
    /// </summary>
    private void SendAnnotationChunks(List<AnnotationData> allAnnotations)
    {
        const int CHUNK_SIZE = 5; // Adjust based on complexity
        
        for (int i = 0; i < allAnnotations.Count; i += CHUNK_SIZE)
        {
            int currentChunkSize = Mathf.Min(CHUNK_SIZE, allAnnotations.Count - i);
            AnnotationData[] chunk = new AnnotationData[currentChunkSize];
            
            for (int j = 0; j < currentChunkSize; j++)
            {
                chunk[j] = allAnnotations[i + j];
            }
            
            RPC_ReceiveAnnotationChunk(chunk);
        }
    }

    /// <summary>
    /// Network RPC to clear annotations on all clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClearAnnotations()
    {
        if (Runner.IsClient)
        {
            annotation.Clear();
            annotationDescriptions.Clear();
            annotationTimes.Clear();
            annotationsReady = false;
            Debug.Log("CLIENT: Cleared all annotation dictionaries");
        }
    }
    
    /// <summary>
    /// Network RPC to receive annotation chunk data
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReceiveAnnotationChunk(AnnotationData[] chunk)
    {
        if (Runner.IsClient)
        {
            foreach (var data in chunk)
            {
                ProcessReceivedAnnotation(data);
            }
            
            Debug.Log($"CLIENT: Received {chunk.Length} annotations");
        }
    }

    /// <summary>
    /// Processes received annotation data and reconstructs local annotation dictionaries
    /// </summary>
    private void ProcessReceivedAnnotation(AnnotationData data)
    {
        int id = data.AnnotationID;
        
        annotationTimes[id] = data.AnnotationTime;
        annotationDescriptions[id] = data.Description.ToString();
        
        if (data.AnnotationType == 0) // GameObject
        {
            ProcessGameObjectAnnotation(id, data);
        }
        else if (data.AnnotationType == 1) // Vector3
        {
            annotation[id] = new Vector3(data.PositionX, data.PositionY, data.PositionZ);
        }
        else if (data.AnnotationType == 2) // Dictionary
        {
            ProcessDictionaryAnnotation(id, data);
        }
    }

    /// <summary>
    /// Processes GameObject reference annotations
    /// </summary>
    private void ProcessGameObjectAnnotation(int id, AnnotationData data)
    {
        string objectName = data.ObjectName.ToString();
        GameObject foundObj = GameObject.Find(objectName);
        
        if (foundObj != null)
        {
            annotation[id] = foundObj;
        }
        else
        {
            Debug.LogError($"CLIENT: Could not find GameObject named {objectName}");
            annotation[id] = new Dictionary<string, object> { { "type", "Reference" }, { "obj", objectName } };
        }
    }

    /// <summary>
    /// Processes dictionary-based annotations
    /// </summary>
    private void ProcessDictionaryAnnotation(int id, AnnotationData data)
    {
        Dictionary<string, object> dict = new Dictionary<string, object>();
        
        if (!string.IsNullOrEmpty(data.DictType.ToString()))
            dict["type"] = data.DictType.ToString();
            
        if (!string.IsNullOrEmpty(data.DictFrom.ToString()))
        {
            // Handle player or from fields based on annotation type
            if (IsPlayerBasedAnnotation(data.DictType.ToString()))
            {
                dict["player"] = data.DictFrom.ToString();
            }
            else
            {
                dict["from"] = data.DictFrom.ToString();
            }
        }
        
        if (!string.IsNullOrEmpty(data.DictTo.ToString()))
        {
            dict["to"] = data.DictTo.ToString();
        }
        else if (data.PointX != 0 || data.PointY != 0)
        {
            dict["to"] = new Dictionary<string, float> { { "x", data.PointX }, { "y", data.PointY } };
        }
        
        annotation[id] = dict;
    }

    /// <summary>
    /// Determines if annotation type uses "player" field instead of "from"
    /// </summary>
    private bool IsPlayerBasedAnnotation(string type)
    {
        return type == "ReceiveBall" || type == "PickUp" || type == "PutDown" || type == "Packaging";
    }
    
    /// <summary>
    /// Network RPC to signal annotation sync completion
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FinishAnnotationSync()
    {
        if (Runner.IsClient)
        {
            annotationsReady = true;
            Debug.Log($"CLIENT: All annotations synced. Total annotations: {annotation.Count}");
        }
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Generates description text for object reference annotations
    /// </summary>
    public string GetDescriptionAnnotation(GameObject go)
    {
        return $"(Coach is pointing at {go.name})";
    }
    
    /// <summary>
    /// Checks if annotations have been properly synchronized across clients
    /// </summary>
    public bool AreAnnotationsSynced()
    {
        return gameManager.laptopMode ? true : annotationsReady;
    }
    #endregion

    #region JSON Export
    /// <summary>
    /// Converts annotations to JSON-serializable format for export to LLM systems
    /// </summary>
    public List<Dictionary<string, object>> GetAnnotationsAsJson()
    {
        var annotationsList = new List<Dictionary<string, object>>();

        foreach (var entry in annotation)
        {
            int id = entry.Key;
            object value = entry.Value;

            if (value is GameObject go)
            {
                annotationsList.Add(new Dictionary<string, object>
                {
                    { "id", id.ToString() },
                    { "type", "Reference" },
                    { "obj", go.name }
                });
            }
            else if (value is Vector3 vector)
            {
                annotationsList.Add(new Dictionary<string, object>
                {
                    { "id", id.ToString() },
                    { "type", "Point" },
                    { "point", new { x = vector.x, y = vector.z } }
                });
            }
            else if (value is Dictionary<string, object> dictValue)
            {
                var annotationDict = new Dictionary<string, object>
                {
                    ["id"] = id.ToString()
                };

                // Merge user-defined fields from logged actions
                foreach (var kvp in dictValue)
                {
                    annotationDict[kvp.Key] = kvp.Value;
                }

                annotationsList.Add(annotationDict);
            }
        }

        return annotationsList;
    }
    #endregion

    #region Annotation Creation Methods
    /// <summary>
    /// Creates annotation for pause action during gameplay
    /// </summary>
    public void CreatePauseActionAnnotation(float segmentStartTime)
    {
        Dictionary<string, object> pauseAction = new Dictionary<string, object>
        {
            { "type", "PauseAction" }
        };
            
        annotation.Add(clickOrder, pauseAction);
        annotationDescriptions.Add(clickOrder, "Coach paused the game");
            
        float pauseTime = Time.time - segmentStartTime;
        annotationTimes.Add(clickOrder, pauseTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(clickOrder, pauseTime);
            
        Debug.Log($"Added PauseAction annotation at {pauseTime:F2}s, key {clickOrder}");
        clickOrder++;
    }

    /// <summary>
    /// Creates annotation for object click/selection events
    /// Uses jsonToLLM.time as the global time reference for consistency with tokens
    /// </summary>
    public void CreateObjectClickAnnotation(GameObject clickedObject, float segmentStartTime)
    {
        annotation.Add(clickOrder, clickedObject);
        annotationDescriptions.Add(clickOrder, GetDescriptionAnnotation(clickedObject));
        objectToKey[clickedObject] = clickOrder;

        // Use jsonToLLM.time as the global time reference
        // This ensures annotations and tokens are on the same timeline
        float annotationTime = jsonToLLM.time;
        annotationTimes.Add(clickOrder, annotationTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(clickOrder, annotationTime);

        Debug.Log($"Added {clickedObject.name} to annotations at {annotationTime:F2}s (jsonToLLM.time), key {clickOrder}");
        clickOrder++;
    }
    
    /// <summary>
    /// Inserts an annotation marker into the token dictionary at the exact annotation timestamp
    /// This ensures [0], [1], [2] markers appear at the same time as stored in clickTimes
    /// </summary>
    private void InsertAnnotationMarkerIntoTokens(int annotationId, float annotationTime)
    {
        if (jsonToLLM == null || jsonToLLM.tokenDictionary == null)
        {
            Debug.LogWarning("Cannot insert annotation marker - jsonToLLM or tokenDictionary is null");
            return;
        }

        string marker = $"[{annotationId}]";
        
        // Always insert at the exact annotation time to match clickTimes
        if (!jsonToLLM.tokenDictionary.ContainsKey(annotationTime))
        {
            jsonToLLM.tokenDictionary[annotationTime] = new List<object>();
        }
        jsonToLLM.tokenDictionary[annotationTime].Add(marker);
        
        Debug.Log($"Inserted annotation marker {marker} at time {annotationTime:F3}s");
    }

    /// <summary>
    /// Creates annotation for position click events
    /// Uses jsonToLLM.time as the global time reference for consistency with tokens
    /// </summary>
    public void CreatePositionClickAnnotation(Vector3 clickedPosition, float segmentStartTime)
    {
        annotation.Add(clickOrder, clickedPosition);
        annotationDescriptions.Add(clickOrder, $"(Position at {clickedPosition})");

        // Use jsonToLLM.time as the global time reference
        // This ensures annotations and tokens are on the same timeline
        float annotationTime = jsonToLLM.time;
        annotationTimes.Add(clickOrder, annotationTime);
        
        // Insert annotation marker into token stream at the exact same time
        InsertAnnotationMarkerIntoTokens(clickOrder, annotationTime);

        Debug.Log($"Added position {clickedPosition} to annotations at {annotationTime:F2}s, key {clickOrder}");
        clickOrder++;
    }

    /// <summary>
    /// Creates annotation for trigger pass events (coach instructing player to pass)
    /// </summary>
    public void CreateTriggerPassAnnotation(GameObject teammate)
    {
        int eventID = clickOrder;
        float eventTime = jsonToLLM.time;
    
        annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "TriggerPass" },
            { "from", teammate.name }
        });
    
        annotationDescriptions.Add(eventID, $"(Coach told {teammate.name} to pass the ball)");
        annotationTimes.Add(eventID, eventTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(eventID, eventTime);
        
        Debug.Log($"Added trigger pass to annotations at {eventTime:F2}s, key {eventID}");
        clickOrder++;
    }

    /// <summary>
    /// Creates annotation for ball interception events
    /// </summary>
    public void CreateInterceptAnnotation(GameObject player)
    {
        int interceptID = clickOrder;
        float interceptTime = jsonToLLM.time;
        
        annotation.Add(clickOrder, new Dictionary<string, string>
        {
            { "type", "Intercept" },
            { "player", player.name }
        });

        annotationTimes.Add(interceptID, interceptTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(interceptID, interceptTime);
        
        Debug.Log($"Intercept action recorded with ID {interceptID} at time: {interceptTime}");
        clickOrder++; 
    }

    /// <summary>
    /// Creates annotation for player-to-player pass events
    /// </summary>
    public void CreatePassAnnotation(GameObject player, GameObject closestPlayerInDirection)
    {
        int passID = clickOrder;
        float passTime = jsonToLLM.time;
    
        annotation.Add(passID, new Dictionary<string, object>
        {
            { "type", "Pass" },
            { "from", player.name },
            { "to", closestPlayerInDirection.name }
        });

        annotationDescriptions.Add(passID, $"({player.name} passed to {closestPlayerInDirection.name})");
        annotationTimes.Add(passID, passTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(passID, passTime);
        
        Debug.Log($"Pass action recorded with ID {passID}, from: {player.name} to: {closestPlayerInDirection.name} at time: {passTime}");
        clickOrder++;
    }

    /// <summary>
    /// Creates annotation for goal shooting events
    /// </summary>
    public void CreateShootGoalAnnotation(GameObject player, GameObject goalObj)
    {
        int shootID = clickOrder;
        float shootTime = jsonToLLM.time;
    
        annotation.Add(shootID, new Dictionary<string, object>
        {
            { "type", "Shoot Goal" },
            { "from", player.name },
            { "to", goalObj.name }
        });

        annotationDescriptions.Add(shootID, $"({player.name} shot towards Goal)");
        annotationTimes.Add(shootID, shootTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(shootID, shootTime);
        
        Debug.Log($"Shoot goal action recorded with ID {shootID}, from: {player.name} at time: {shootTime}");
        clickOrder++; 
    }

    /// <summary>
    /// Creates annotation for through pass events (passing to open space)
    /// </summary>
    public void CreateThroughPassAnnotation(GameObject player, Vector3 pos)
    {
        int passID = clickOrder;
        float passTime = jsonToLLM.time;

        var pointDict = new Dictionary<string, float>
        {
            { "x", pos.x },
            { "y", pos.z }
        };
        
        annotation.Add(passID, new Dictionary<string, object>
        {
            { "type", "Through Pass" },
            { "from", player.name},
            { "to", pointDict }
        });
        
        annotationDescriptions.Add(passID, $"({player.name} passed to position: {pointDict})");
        annotationTimes.Add(passID, passTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(passID, passTime);
        
        Debug.Log($"Through Pass action recorded with ID {passID} at time: {passTime}");
        clickOrder++; 
    }

    /// <summary>
    /// Creates annotation for ball reception events
    /// </summary>
    public void CreateReceivePassAnnotation(GameObject player)
    {
        int receiveBallID = clickOrder;
        float receiveBallTime = jsonToLLM.time;
        
        annotation.Add(receiveBallID, new Dictionary<string, object>
        {
            { "type", "ReceiveBall" },
            { "player", player.gameObject.name }
        });
        
        annotationDescriptions.Add(receiveBallID, $"({player.gameObject.name} received the ball)");
        annotationTimes.Add(receiveBallID, receiveBallTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(receiveBallID, receiveBallTime);
        
        Debug.Log($"ReceiveBall action recorded with ID {receiveBallID} at time: {receiveBallTime}");
        clickOrder++;
    }

    /// <summary>
    /// Creates annotation for finite state machine node interactions
    /// </summary>
    public void CreateFsmNodeAnnotation(int selectedStateId, TMP_Text descriptionText)
    {
        Dictionary<string, object> nodeAnnotation = new Dictionary<string, object>
        {
            { "type", "node annotation" },
            { "stateId", selectedStateId },
            { "description", descriptionText.text }
        };
        
        float annotationTime = Time.time - programSynthesisManager.segmentStartTime;
        annotationTimes.Add(clickOrder, annotationTime);
        annotation.Add(clickOrder, nodeAnnotation);
        annotationDescriptions.Add(clickOrder, $"Node annotation: State {selectedStateId}");
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(clickOrder, annotationTime);
        
        Debug.Log($"Added node annotation for state {selectedStateId}, key {clickOrder}");
        clickOrder++;
    }

    /// <summary>
    /// Creates annotation for finite state machine edge/transition interactions
    /// </summary>
    public void CreateFsmEdgeAnnotation(int selectedTransitionId, TMP_Text descriptionText)
    {
        Dictionary<string, object> edgeAnnotation = new Dictionary<string, object>
        {
            { "type", "edge annotation" },
            { "transitionId", selectedTransitionId },
            { "description", descriptionText.text }
        };
        
        float annotationTime = Time.time - programSynthesisManager.segmentStartTime;
        annotationTimes.Add(clickOrder, annotationTime);
        annotation.Add(clickOrder, edgeAnnotation);
        annotationDescriptions.Add(clickOrder, $"Edge annotation: Transition {selectedTransitionId}");
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(clickOrder, annotationTime);
        
        Debug.Log($"Added edge annotation for transition {selectedTransitionId}, key {clickOrder}");
        clickOrder++;
    }
    
    public void CreatePickUpAnnotation(GameObject closestObject)
    {
        int eventID = clickOrder;
        float eventTime = jsonToLLM.time;
            
        var pointDict = new Dictionary<string, float>
        {
            { "x", closestObject.transform.position.x },
            { "y", closestObject.transform.position.y }
        };
        
        annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "Pick Up" },
            { "player", this.name },
            { "object", closestObject.name },
            { "at", pointDict }
        });

        annotationDescriptions.Add(eventID, $"({this.name} picked up {closestObject.name})");
        annotationTimes.Add(eventID, eventTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(eventID, eventTime);
        
        Debug.Log($"Pick Up action recorded with ID {eventID}, from player: {this.name} for object: {closestObject.name} at time: {eventTime}");
        clickOrder++; 
    }
    
    public void CreatePutDownAnnotation(GameObject o)
    {
        int eventID = clickOrder;
        float eventTime = jsonToLLM.time;
        
        var pointDict = new Dictionary<string, float>
        {
            { "x", o.transform.position.x },
            { "y", o.transform.position.y }
        };

        annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "Put Down" },
            { "player", this.name },
            { "object", o.name },
            { "at", pointDict }
        });

        annotationDescriptions.Add(eventID, $"({this.name} put down {o.name})");
        annotationTimes.Add(eventID, eventTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(eventID, eventTime);
        
        Debug.Log(
            $"Put Down action recorded with ID {eventID}, from player: {this.name} for object: {o.name} at time: {eventTime}");
        clickOrder++;
    }
    
    public void CreateReceivedItemAnnotation(GameObject o, GameObject receivedPlayer)
    {
        int eventID = clickOrder;
        float eventTime = jsonToLLM.time;

        annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "Received Item" },
            { "player", receivedPlayer.name },
            { "object", o.name },
            { "from", this.name }
        });
        annotationDescriptions.Add(eventID, $"({receivedPlayer.name} received {o.name}) from {this.name}");
        annotationTimes.Add(eventID, eventTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(eventID, eventTime);
        
        Debug.Log(
            $"Received Item action recorded with ID {eventID}, for player: {receivedPlayer.name} for object: {o.name} at time: {eventTime}");
        clickOrder++;
    }
    
    public void CreatePackagingAnnotation(GameObject o)
    {
        int eventID = clickOrder;
        float eventTime = jsonToLLM.time;

        var pointDict = new Dictionary<string, float>
        {
            { "x", o.transform.position.x },
            { "y", o.transform.position.y }
        };
        
        annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "Packaging" },
            { "player", this.name },
            { "object", o.name },
            { "at", pointDict }
        });

        annotationDescriptions.Add(eventID, $"({this.name} packaged {o.name})");
        annotationTimes.Add(eventID, eventTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(eventID, eventTime);
        
        Debug.Log(
            $"Packaging action recorded with ID {eventID}, from player: {this.name} for object: {o.name} at time: {eventTime}");
        clickOrder++;
    }
    
    public void CreateRaiseHandAnnotation()
    {
        int eventID = clickOrder;
        float eventTime = jsonToLLM.time;

        annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "Raise Hand" },
            { "player", this.name }
        });

        annotationDescriptions.Add(eventID, $"({this.name} raised hand)");
        annotationTimes.Add(eventID, eventTime);
        
        // Insert annotation marker into token stream
        InsertAnnotationMarkerIntoTokens(eventID, eventTime);
        
        Debug.Log(
            $"Raise Hand action recorded with ID {eventID}, from player: {this.name} at time: {eventTime}");
        clickOrder++;
    }
    
    #endregion
}