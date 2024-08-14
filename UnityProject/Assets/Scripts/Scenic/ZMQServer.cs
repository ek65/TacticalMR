using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Newtonsoft.Json;
using NetMQ;

public class ZMQServer : MonoBehaviour
{
    [SerializeField] private string ip;

    [SerializeField] private string port = "5555";

    private ScenicParser parser;
    // Start is called before the first frame update
    private ZMQRequester zmqRequester;

    public int lastTick;
    private ObjectsList objectList;

    private bool destroyed;

    private JSONStatusMaker sender;

    private TimelineManager tlManager;

    void Start()
    {
        if (ip == null || port == null)
        {
            throw new System.Exception();
        }

        bool isServer = true;
        zmqRequester = new ZMQRequester(ip, port, isServer);
        zmqRequester.Start();
        destroyed = false;

        lastTick = -1;

        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        parser = new ScenicParser();

        sender = this.gameObject.GetComponent<JSONStatusMaker>();

        tlManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();

    }
    void Update()
    {
        // sends to scenic
        if (tlManager.Paused)
        {
            // zmqRequester.SetReady(false);
            zmqRequester.SetSendData(null);
        }
        else
        {
            string newSendData = sender.getUnityData();
            zmqRequester.SetSendData(newSendData);
        }
        
        // zmqRequester.SetReady(true);

        // gets json from scenic
        string newData = zmqRequester.GetData();
        if (newData == null || newData.Equals("Null") || newData.Equals(""))
        {
            return;
        }
        // Debug.Log(newData);
        try
        {
            ScenicParser.ScenicJson jsonResult = parser.ParseData(newData);
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
        catch (NullReferenceException e)
        {
            Debug.LogError("json failed " + e);
        }
    }

    private void OnDestroy()
    {
        zmqRequester.server.Close();
        zmqRequester.server.Dispose();
        zmqRequester.Stop();
        //Following command crashes my editor for some reason
        // NetMQConfig.Cleanup(false); 
    }

    private void OnApplicationQuit()
    {
        zmqRequester.server.Close();
        zmqRequester.server.Dispose();
        zmqRequester.Stop();
        // NetMQConfig.Cleanup(); 
    }

    private List<ScenicMovementData> ParseMovementData(ScenicParser.ScenicJson data)
    {
        Debug.Log("Parse Movement Data!");
        List<ScenicMovementData> moveData = parser.ScenicMovementParser(data);
        //init = true;
        return moveData;
    }

    private int GetTickFromData(ScenicParser.ScenicJson data)
    {
        int tick = -1;
        tick = data.TimestepNumber;
        return tick;
    }

    private void ApplyMovement(List<ScenicMovementData> movementData)
    {
        // We want to ensure players spawned. We use this check.
        int numPlayersCheck = 0;
        int[] listOfScenicPlayerIndices = new int[movementData.Count];
        int[] listOfScenicObjectIndices = new int[movementData.Count];
        int currScenicPlayerListIdx = 0;
        int currScenicObjectListIdx = 0;
        int currMovementDataIndex = 0;
        int aiAgentIndex = 0;
        TimelineManager tlManager = FindObjectOfType<TimelineManager>();
        foreach (ScenicMovementData s in movementData)
        {
            if (s.model.modelType == "Player")
            {
                listOfScenicPlayerIndices[currScenicPlayerListIdx] = currMovementDataIndex;
                currScenicPlayerListIdx += 1;
                numPlayersCheck++;
            }
            // else if (s.model.modelType == "aiAgent")
            // {
            //     aiAgentIndex = currMovementDataIndex;
            // }
            else if(s.model.modelType == "Human" || s.model.modelType == "Coach")
            {
                //broadcast the pause boolean which may be set by scenic AI agent at any given moment. TimelineManager would handle it
                // tlManager.NotifyPauseStatus(s.pause);
                HumanInterface human = objectList.humanPlayers[0].GetComponentInChildren<HumanInterface>();
                human.ApplyMovement(movementData[aiAgentIndex]);
            }
            // else if (s.model.modelType != "player.human")
            // {
            //     listOfScenicObjectIndices[currScenicObjectListIdx] = currMovementDataIndex;
            //     currScenicObjectListIdx += 1;
            // }
            currMovementDataIndex += 1;
        }
        if (numPlayersCheck != objectList.scenicPlayers.Count)
        {
            Debug.LogError("Scenic Players Mismatched? MovementData Received = " + movementData.Count + " Scenic Players = " + objectList.scenicPlayers.Count);
        }

        for (int i = 0; i < numPlayersCheck; i++)
        {
            //Note: Because we apply controls at a set index, this *should* retain order...
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
        
        // if (objectList.AIAgent != null)
        // {
        //     AIInterface ai = objectList.AIAgent.GetComponentInChildren<AIInterface>();
        //     ai.ApplyMovement(movementData[aiAgentIndex]);
        // }
        
        /**
        for (int i = 0; i < objectList.scenicPlayers.Count; i ++)
        {   
            if (movementData[i].model.modelType == "player.scenic")
            {
                Debug.LogError(i);
                PlayerInterface p = objectList.scenicPlayers[i].GetComponentInChildren<PlayerInterface>();
                p.ApplyMovement(movementData[i]);
            }
        }*/
        for (int i = 0; i < objectList.scenicObjects.Count; i++)
        {
            PlayerInterface p = objectList.scenicObjects[i].GetComponentInChildren<PlayerInterface>();
            if (p != null)
            {
                int currObjectIdx = listOfScenicObjectIndices[i];
                p.ApplyMovement(movementData[currObjectIdx]);
            }
        }
        //checking for human players to apply to
        // foreach (ScenicMovementData s in movementData){
        //     if (s.model.modelType == "player.human")
        //     {
        //         this.enableThrustFromData = s.thBoActive;
        //         this.enableBrakeFromData = s.hud.brakeActive;
        //         string[] hudMessages;
        //         hudMessages = s.hud.message.ToArray();
        //         foreach(string str in hudMessages)
        //         {
        //             Debug.LogWarning(str);
        //         }
        //         try {
        //             HumanInterface p = objectList.humanPlayers[0].GetComponentInChildren<HumanInterface>();
        //             p.SetDataServerRpc(hudMessages);
        //             p.SetDataClientRpc(hudMessages);
        //             if (s.doLineDraw)
        //             {
        //                 p.SetLineDestClientRpc(s.lineDestination.ToArray(), true);
        //                 p.SetLineDestServerRpc(s.lineDestination.ToArray(), true);
        //             }
        //             //p.ApplyMovementClientRpc();
        //             
        //         } catch {
        //             Debug.LogError("Human not spawned in yet");
        //         }
        //     }
        // }
        //Debug.Log(objectList.scenicObjects.Count);
        //Make a new function that finds the human player SERVER SIDE. This means that you get it from the objects list and save it.
        //The reason for this is because HumanInterface has issues saving the data when received server side. 
    }


    //[ServerRpc]
    public void ResetTickServerRpc()
    {
        //Debug.LogError("RESTTING TICK");
        // skipSelected = false;
        destroyed = true;
        lastTick = -1;
    }
}














