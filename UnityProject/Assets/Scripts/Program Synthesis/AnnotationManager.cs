using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

public class AnnotationManager : NetworkBehaviour
{
    [Header("Scene References")]
    private JSONToLLM jsonToLLM;
    private GameManager gameManager;
    private ProgramSynthesisManager programSynthesisManager;

    [Header("Annotations")]
    public Dictionary<int, object> annotation = new Dictionary<int, object>();
    public Dictionary<int, string> annotationDescriptions = new Dictionary<int, string>();
    public Dictionary<GameObject, int> objectToKey = new Dictionary<GameObject, int>();
    public Dictionary<int, float> annotationTimes = new Dictionary<int, float>();
    public int clickOrder = 0; 
    
    void Start()
    {
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        programSynthesisManager = this.gameObject.GetComponent<ProgramSynthesisManager>();

        Debug.Log("KeyboardInput script initialized");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // Struct to hold Annotation data for RPCs
    public struct AnnotationData : INetworkStruct
    {
        public int AnnotationID;
        public byte AnnotationType; // 0 = GameObject, 1 = Vector3, 2 = Dictionary
    
        // For Vector3 type
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
    
    public void SyncAnnotationsToClients()
    {
        if (gameManager.laptopMode || !Runner.IsServer) return;
        
        // First, clear client annotations
        RPC_ClearAnnotations();
        
        List<AnnotationData> allAnnotations = new List<AnnotationData>();
        
        // Convert all annotations to network-friendly format
        foreach (var entry in annotation)
        {
            int id = entry.Key;
            object value = entry.Value;
            string description = annotationDescriptions.ContainsKey(id) ? annotationDescriptions[id] : "";
            float time = annotationTimes.ContainsKey(id) ? annotationTimes[id] : 0f;
            
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
                
                // Handle common dictionary fields
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
            
            allAnnotations.Add(data);
        }
        
        // Send annotations in chunks
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
        
        // Signal that all annotations have been sent
        RPC_FinishAnnotationSync();
        
        Debug.Log($"SERVER: Sent {allAnnotations.Count} annotations to clients");
    }
    
    public string GetDescriptionAnnotation(GameObject go)
    {
        return $"(Coach is pointing at {go.name})";
    }
    
    public bool annotationsReady = false;

    public bool AreAnnotationsSynced()
    {
        return gameManager.laptopMode ? true : annotationsReady;
    }
    
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
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReceiveAnnotationChunk(AnnotationData[] chunk)
    {
        if (Runner.IsClient)
        {
            foreach (var data in chunk)
            {
                int id = data.AnnotationID;
                
                // Add to annotationTimes
                annotationTimes[id] = data.AnnotationTime;
                
                // Add to annotationDescriptions
                annotationDescriptions[id] = data.Description.ToString();
                
                // Add to annotation based on type
                if (data.AnnotationType == 0) // GameObject
                {
                    // For GameObject references, we need to find the object by name
                    string objectName = data.ObjectName.ToString();
                    GameObject foundObj = GameObject.Find(objectName);
                    
                    if (foundObj != null)
                    {
                        annotation[id] = foundObj;
                    }
                    else
                    {
                        Debug.LogError($"CLIENT: Could not find GameObject named {objectName}");
                        // Create a placeholder dictionary to avoid null references
                        annotation[id] = new Dictionary<string, object> { { "type", "Reference" }, { "obj", objectName } };
                    }
                }
                else if (data.AnnotationType == 1) // Vector3
                {
                    annotation[id] = new Vector3(data.PositionX, data.PositionY, data.PositionZ);
                }
                else if (data.AnnotationType == 2) // Dictionary
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    
                    if (!string.IsNullOrEmpty(data.DictType.ToString()))
                        dict["type"] = data.DictType.ToString();
                        
                    if (!string.IsNullOrEmpty(data.DictFrom.ToString()))
                    {
                        // Handle player or from
                        if (data.DictType.ToString() == "ReceiveBall" || 
                            data.DictType.ToString() == "PickUp" || 
                            data.DictType.ToString() == "PutDown" || 
                            data.DictType.ToString() == "Packaging")
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
                        // Point coordinates
                        dict["to"] = new Dictionary<string, float> { { "x", data.PointX }, { "y", data.PointY } };
                    }
                    
                    annotation[id] = dict;
                }
            }
            
            Debug.Log($"CLIENT: Received {chunk.Length} annotations");
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FinishAnnotationSync()
    {
        if (Runner.IsClient)
        {
            annotationsReady = true;
            Debug.Log($"CLIENT: All annotations synced. Total annotations: {annotation.Count}");
        }
    }
    
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
            // NEW: handle dictionary-based annotations (e.g. "PickUp", "Pass", etc.)
            else if (value is Dictionary<string, object> dictValue)
            {
                // Start by creating a fresh dictionary for the JSON entry
                var annotationDict = new Dictionary<string, object>
                {
                    ["id"] = id.ToString()
                };

                // Merge user-defined fields from your log calls
                foreach (var kvp in dictValue)
                {
                    annotationDict[kvp.Key] = kvp.Value;
                }

                annotationsList.Add(annotationDict);
            }
        }

        return annotationsList;
    }
    
    public void CreatePauseActionAnnotation(float segmentStartTime)
    {
        // Create a dictionary with the PauseAction type
        Dictionary<string, object> pauseAction = new Dictionary<string, object>
        {
            { "type", "PauseAction" }
        };
            
        // Add to annotations with current click order
        annotation.Add(clickOrder, pauseAction);
        annotationDescriptions.Add(clickOrder, "Coach paused the game");
            
        // Record the time of pause relative to segment start
        float pauseTime = Time.time - segmentStartTime;
        annotationTimes.Add(clickOrder, pauseTime);
            
        Debug.Log($"Added PauseAction annotation at {pauseTime:F2}s, key {clickOrder}");
            
        // Increment click order for next annotation
        clickOrder++;
    }

    public void CreateObjectClickAnnotation(GameObject clickedObject, float segmentStartTime)
    {
        annotation.Add(clickOrder, clickedObject);
        annotationDescriptions.Add(clickOrder, GetDescriptionAnnotation(clickedObject));
        objectToKey[clickedObject] = clickOrder;

        float annotationRelativeTime = Time.time - segmentStartTime;
        annotationTimes.Add(clickOrder, annotationRelativeTime);

        Debug.Log($"Added {clickedObject.name} to annotations at {annotationRelativeTime:F2}s, key {clickOrder}");
        clickOrder++;
    }
    
    public void CreatePositionClickAnnotation(Vector3 clickedPosition, float segmentStartTime)
    {
        annotation.Add(clickOrder, clickedPosition);
        annotationDescriptions.Add(clickOrder, $"(Position at {clickedPosition})");

        float annotationRelativeTime = Time.time - segmentStartTime;
        annotationTimes.Add(clickOrder, annotationRelativeTime);

        Debug.Log($"Added position {clickedPosition} to annotations at {annotationRelativeTime:F2}s, key {clickOrder}");
        clickOrder++;
    }

    public void CreateTriggerPassAnnotation(GameObject teammate)
    {
        int eventID = clickOrder;
        float eventTime = jsonToLLM.time;
    
        annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "TriggerPass" },
            { "from", teammate.name }
        });
        Debug.Log(annotation);
    
        annotationDescriptions.Add(eventID, $"(Coach told {teammate.name} to pass the ball)");
        Debug.Log($"Added trigger pass to annotations at {eventTime:F2}s, key {eventTime}");
        annotationTimes.Add(eventID, eventTime);
        clickOrder++;
    }

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
        Debug.Log($"Intercept action recorded with ID {interceptID} at time: {interceptTime}");
        clickOrder++; 
    }

    public void CreatePassAnnotation(GameObject player, GameObject closestPlayerInDirection)
    {
        int passID = clickOrder;
        float passTime = jsonToLLM.time;
    
        GameObject targetPlayer = closestPlayerInDirection;
        annotation.Add(passID, new Dictionary<string, object>
        {
            { "type", "Pass" },
            { "from", player.name },
            { "to", targetPlayer.name }
        });

        annotationDescriptions.Add(passID, $"({player.name} passed to {targetPlayer.name})");
        
        annotationTimes.Add(passID, passTime);
        Debug.Log($"Pass action recorded with ID {passID}, from: {player.name} to: {targetPlayer.name} at time: {passTime}");
        clickOrder++;
    }

    public void CreateShootGoalAnnotation(GameObject player, GameObject goalObj)
    {
        int passID = clickOrder;
        float passTime = jsonToLLM.time;
    
        annotation.Add(passID, new Dictionary<string, object>
        {
            { "type", "Shoot Goal" },
            { "from", player.name },
            { "to", goalObj.name }
        });

        annotationDescriptions.Add(passID, $"({player.name} shot towards Goal)");
        
        annotationTimes.Add(passID, passTime);
        Debug.Log($"Shoot goal action recorded with ID {passID}, from: {player.name} at time: {passTime}");
        clickOrder++; 
    }

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
        Debug.Log($"Through Pass action recorded with ID {passID} at time: {passTime}");
        clickOrder++; 
    }

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
        Debug.Log($"ReceiveBall action recorded with ID {receiveBallID} at time: {receiveBallTime}");
        
        clickOrder++;
    }

    public void CreateFsmNodeAnnotation(int selectedStateId, TMP_Text descriptionText)
    {
        // Node annotation
        Dictionary<string, object> nodeAnnotation = new Dictionary<string, object>
        {
            { "type", "node annotation" },
            { "stateId", selectedStateId },
            { "description", descriptionText.text }
        };
        
        annotationTimes.Add(clickOrder, Time.time - programSynthesisManager.segmentStartTime);

        annotation.Add(clickOrder, nodeAnnotation);
        annotationDescriptions.Add(clickOrder, $"Node annotation: State {selectedStateId}");
        
        Debug.Log($"Added node annotation for state {selectedStateId}, key {clickOrder}");
        
        clickOrder++;
    }

    public void CreateFsmEdgeAnnotation(int selectedTransitionId, TMP_Text descriptionText)
    {
        Dictionary<string, object> edgeAnnotation = new Dictionary<string, object>
        {
            { "type", "edge annotation" },
            { "transitionId", selectedTransitionId },
            { "description", descriptionText.text }
        };
        
        annotationTimes.Add(clickOrder, Time.time - programSynthesisManager.segmentStartTime);
        
        annotation.Add(clickOrder, edgeAnnotation);
        annotationDescriptions.Add(clickOrder, $"Edge annotation: Transition {selectedTransitionId}");
        
        Debug.Log($"Added edge annotation for transition {selectedTransitionId}, key {clickOrder}");
        
        clickOrder++;
    }
}
