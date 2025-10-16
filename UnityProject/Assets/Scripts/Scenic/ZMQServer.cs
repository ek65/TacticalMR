using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using NetMQ;

/// <summary>
/// Main server component that manages communication between Unity and the Scenic simulation system.
/// Handles JSON parsing, object synchronization, and movement data application.
/// Coordinates with ZMQRequester for network communication and manages simulation state.
/// </summary>
public class ZMQServer : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField] private string ip;
    [SerializeField] private string port = "5555";
    #endregion

    #region Private Fields
    private ScenicParser parser;
    private ZMQRequester zmqRequester;
    private ObjectsList objectList;
    private JSONStatusMaker sender;
    private TimelineManager tlManager;
    private bool destroyed;
    private bool firstApplyMovement = true;
    #endregion

    #region Public Properties
    /// <summary>
    /// Last processed tick number for synchronization
    /// </summary>
    public int lastTick;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        ValidateConfiguration();
        InitializeNetworking();
        InitializeComponents();
    }

    void Update()
    {
        HandleOutgoingData();
        HandleIncomingData();
    }

    private void OnDestroy()
    {
        CleanupNetworking();
    }

    private void OnApplicationQuit()
    {
        CleanupNetworking();
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Validates that required network configuration is set
    /// </summary>
    private void ValidateConfiguration()
    {
        if (ip == null || port == null)
        {
            throw new System.Exception("IP and Port must be configured for ZMQ communication");
        }
    }

    /// <summary>
    /// Initializes ZMQ networking as server
    /// </summary>
    private void InitializeNetworking()
    {
        bool isServer = true;
        zmqRequester = new ZMQRequester(ip, port, isServer);
        zmqRequester.Start();
        destroyed = false;
        lastTick = -1;
    }

    /// <summary>
    /// Initializes component references
    /// </summary>
    private void InitializeComponents()
    {
        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        parser = new ScenicParser();
        sender = this.gameObject.GetComponent<JSONStatusMaker>();
        tlManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
    }
    #endregion

    #region Data Handling
    /// <summary>
    /// Handles outgoing data transmission to Scenic
    /// </summary>
    private void HandleOutgoingData()
    {
        if (tlManager.Paused)
        {
            zmqRequester.SetSendData(null);
        }
        else
        {
            string newSendData = sender.getUnityData();
            zmqRequester.SetSendData(newSendData);
        }
    }

    /// <summary>
    /// Handles incoming data from Scenic and processes simulation updates
    /// </summary>
    private void HandleIncomingData()
    {
        string newData = zmqRequester.GetData();
        if (string.IsNullOrEmpty(newData) || newData.Equals("Null"))
        {
            return;
        }
        
        ProcessScenicData(newData);
    }

    /// <summary>
    /// Processes received JSON data from Scenic simulation
    /// </summary>
    /// <param name="jsonData">Raw JSON string from Scenic</param>
    private void ProcessScenicData(string jsonData)
    {
        ScenicParser.ScenicJson jsonResult = parser.ParseData(jsonData);
        int scenicTick = GetTickFromData(jsonResult);
        int newTick = -1;
        
        if (!destroyed || scenicTick == 0)
        {
            newTick = scenicTick;
            destroyed = false;
        }
        
        if (newTick == lastTick)
        {
            return;
        }
        
        if (newTick > lastTick + 10)
        {
            Debug.LogError("A scenic tick might have been skipped. Last Tick = " + lastTick.ToString() + " New Tick = " + newTick.ToString());
        }
        
        lastTick = newTick;
        List<ScenicMovementData> mvData = ParseMovementData(jsonResult);
        ApplyMovement(mvData);
    }
    #endregion

    #region Movement Processing
    /// <summary>
    /// Parses movement data from Scenic JSON
    /// </summary>
    /// <param name="data">Parsed Scenic JSON data</param>
    /// <returns>List of movement data for each object</returns>
    private List<ScenicMovementData> ParseMovementData(ScenicParser.ScenicJson data)
    {
        List<ScenicMovementData> moveData = parser.ScenicMovementParser(data);
        return moveData;
    }

    /// <summary>
    /// Extracts tick number from Scenic data for synchronization
    /// </summary>
    /// <param name="data">Scenic JSON data</param>
    /// <returns>Current simulation tick number</returns>
    private int GetTickFromData(ScenicParser.ScenicJson data)
    {
        return data.TimestepNumber;
    }

    /// <summary>
    /// Applies movement data to corresponding Unity objects.
    /// Handles player synchronization and ensures proper object mapping.
    /// </summary>
    /// <param name="movementData">List of movement data for all objects</param>
    private void ApplyMovement(List<ScenicMovementData> movementData)
    {
        // Validate player count synchronization
        int numPlayersCheck = 0;
        int[] listOfScenicPlayerIndices = new int[movementData.Count];
        int[] listOfScenicObjectIndices = new int[movementData.Count];
        int currScenicPlayerListIdx = 0;
        int currScenicObjectListIdx = 0;
        int currMovementDataIndex = 0;
        int aiAgentIndex = 0;
        
        TimelineManager tlManager = FindObjectOfType<TimelineManager>();
        
        // Map movement data to object types
        foreach (ScenicMovementData s in movementData)
        {
            if (s.model.modelType == "Player" || s.model.modelType == "Robot")
            {
                listOfScenicPlayerIndices[currScenicPlayerListIdx] = currMovementDataIndex;
                currScenicPlayerListIdx += 1;
                numPlayersCheck++;
            }
            else if(s.model.modelType == "Human" || s.model.modelType == "Coach")
            {
                if (objectList.humanPlayers.Count > 0)
                {
                    HumanInterface human = objectList.humanPlayers[0].GetComponentInChildren<HumanInterface>();
                    human.ApplyMovement(movementData[aiAgentIndex]);
                }
            }
            currMovementDataIndex += 1;
        }
        
        // Validate object synchronization
        if (numPlayersCheck != objectList.scenicPlayers.Count)
        {
            if (firstApplyMovement)
            {
                firstApplyMovement = false;
                return;
            }
            Debug.LogError("Scenic Players Mismatched? MovementData Received = " + movementData.Count + " Scenic Players = " + objectList.scenicPlayers.Count);
        }

        // Apply movement to scenic players
        for (int i = 0; i < numPlayersCheck; i++)
        {
            int currPlayerIdx = listOfScenicPlayerIndices[i];
            PlayerInterface p = objectList.scenicPlayers[i].GetComponentInChildren<PlayerInterface>();
            if (tlManager.Paused)
            {
                continue;
            }
            else
            {
                p.ApplyMovement(movementData[currPlayerIdx]);
            }
        }
        
        // Apply movement to other scenic objects
        for (int i = 0; i < objectList.scenicObjects.Count; i++)
        {
            PlayerInterface p = objectList.scenicObjects[i].GetComponentInChildren<PlayerInterface>();
            if (p != null)
            {
                int currObjectIdx = listOfScenicObjectIndices[i];
                p.ApplyMovement(movementData[currObjectIdx]);
            }
        }
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// Properly closes ZMQ networking resources
    /// </summary>
    private void CleanupNetworking()
    {
        if (zmqRequester?.server != null)
        {
            zmqRequester.server.Close();
            zmqRequester.server.Dispose();
        }
        zmqRequester?.Stop();
    }

    /// <summary>
    /// Resets tick counter for new simulation runs
    /// </summary>
    public void ResetTickServerRpc()
    {
        destroyed = true;
        lastTick = -1;
    }
    #endregion
}