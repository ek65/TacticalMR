using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

public class ScenicObectAddEventArg : EventArgs { public GameObject gameObject { get; set; } }
public class InstantiateScenicObject
{
    ObjectsList objectList;
    public delegate void PublishScenicAddObjectEvent(ScenicObectAddEventArg arg);
    // //timeline manager will subscribe to this event
    public static event PublishScenicAddObjectEvent Publish;

    public InstantiateScenicObject(Vector3 pos, Quaternion rot, string modelType, Color color, string name)
    {
        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        Debug.Log(modelType);
        //Debug.Log(objectList.modelList);
        AddScenicObject(pos, rot, modelType, color, name);
        
    }

    private void AddScenicObject(Vector3 pos, Quaternion rot, string modelType, Color color, string name)
    {
        GameObject addedGameObject = null;
        if (modelType == "Ball")
        {
            // addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["soccer_ball"], pos, Quaternion.identity);
            NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
            NetworkObject temp = runner.Spawn(objectList.modelList["soccer_ball"], pos, Quaternion.identity);
            addedGameObject = temp.gameObject;
            
            BallInterface bI = addedGameObject.GetComponent<BallInterface>();
            
            bI.RPC_InstantiateValues();
            bI.SetObjectName("Ball");
            
            // addedGameObject.name = "Ball";
            // disc.GetComponent<NetworkObject>().Spawn();
            // objectList.ballObject = addedGameObject;
            // objectList.scenicObjects.Add(addedGameObject);
            
        }
        else if (modelType == "goal")
        {
            // addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["goal"], pos, rot);
            NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
            NetworkObject temp = runner.Spawn(objectList.modelList["goal"], pos, rot);
            addedGameObject = temp.gameObject;
            
            GoalInterface gI = addedGameObject.GetComponent<GoalInterface>();
            
            gI.RPC_InstantiateValues();
            gI.SetObjectName("Goal");
            
            // assuming always 1 goal for now
            // addedGameObject.name = "Goal";
            // objectList.goalObject = addedGameObject;
            // objectList.scenicObjects.Add(addedGameObject);
        }
        else if (modelType == "line")
        {
            NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
            NetworkObject temp = runner.Spawn(objectList.modelList["line"], pos, rot);
            addedGameObject = temp.gameObject;
            
            LineInterface lI = addedGameObject.GetComponent<LineInterface>();
            
            lI.RPC_InstantiateValues();
            lI.SetObjectName("Goal");
        }
        // else if (tag == "aiAgent")
        // {
        //     addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["Convai NPC Daniel He"], pos, Quaternion.Euler(-90,0,0));
        //     addedGameObject.transform.parent = GameObject.Find("AI Interface").transform;
        //     objectList.AIAgent = addedGameObject;
        // }
        else if (modelType == "Player" || modelType == "Robot" )
        {
            if (modelType == "Player")
            {
                // addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["player.scenic"], pos, rot);
                NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
                NetworkObject temp = runner.Spawn(objectList.modelList["player.scenic"], pos, rot);
                addedGameObject = temp.gameObject;
                
                PlayerInterface pI = addedGameObject.GetComponent<PlayerInterface>();
                
                if (color == new Color(0f, 0f, 255f, 1f)) // defense team
                {
                    pI.RPC_InstantiateValues(isAlly: true);
                    // pI.ally = true;
                    // objectList.defensePlayers.Add(addedGameObject);
                }
                else if (color == new Color(255f, 0f, 0f, 1f)) // offense team
                {
                    pI.RPC_InstantiateValues(isAlly: false);
                    // pI.enemy = true;
                    // objectList.offensePlayers.Add(addedGameObject);
                }
                
                pI.SetObjectName(name);
                
                addedGameObject.name = name;
                // objectList.scenicPlayers.Add(addedGameObject);
            } else if (modelType == "Robot")
            {
                // addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["player.scenic2"], pos, rot);
                NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
                NetworkObject temp = runner.Spawn(objectList.modelList["player.scenic2"], pos, rot);
                addedGameObject = temp.gameObject;
                
                // addedGameObject.name = name;
                PlayerInterface pI = addedGameObject.GetComponent<PlayerInterface>();
                pI.SetObjectName(name);
                
                // objects are added to objectlist in their respective spawn/interface scripts
                // objectList.scenicPlayers.Add(addedGameObject);

            }

            //objectList.orangePlayers.Add(scenicPlayer.GetComponent<NetworkObject>().NetworkInstanceId);
            Debug.Log("Added Scenic Player");
        }
        else if (modelType == "Human" || modelType == "Coach")
        {
            if (objectList.humanPlayers.Count == 0)
            {
                // Debug.LogError("in here: " + objectList.humanPlayers.Count);
                if (modelType == "Human")
                {
                    // Change to "player.human VR" for VR human, otherwise "player.human"
                    if (GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().laptopMode)
                    {
                        addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["player.human"], pos, rot);
                    }
                    else
                    {
                        addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["player.human VR"], pos, rot);
                    }
                } else if (modelType == "Coach")
                {
                    // addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["player.coach"], pos, rot);
                    NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
                    NetworkObject temp = runner.Spawn(objectList.modelList["player.coach"], pos, rot);
                    addedGameObject = temp.gameObject;
                }
                else if (modelType == "RobotCoach")
                {
                    addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["player.robot"], pos, rot);
                }
                //scenicPlayer.GetComponent<NetworkObject>().Spawn();
                HumanInterface hI = addedGameObject.GetComponent<HumanInterface>();
                hI.RPC_InstantiateValues(isAlly: true);
                hI.SetObjectName("Coach");
                addedGameObject.name = "Coach";
                
                // objectList.humanPlayers.Add(addedGameObject);
                // addedGameObject.GetComponentInChildren<HumanInterface>().ally = true;
                //objectList.orangePlayers.Add(scenicPlayer.GetComponent<NetworkObject>().NetworkInstanceId);
                Debug.Log("Added Human Player");
            }
            else
            {
                if (GameObject.FindGameObjectWithTag("human") != null)
                {
                    addedGameObject = objectList.humanPlayers[0];
                    HumanInterface hI = addedGameObject.GetComponent<HumanInterface>();
                    // Hardcoding "Coach" name here because the name string seems to be empty
                    // TODO: look into fixing this so we don't have to hardcode the string
                    addedGameObject.name = "Coach";
                    // Debug.LogError("name0: " + name);
                    hI.SetObjectName("Coach");
                    
                    Fade f = objectList.humanPlayers[0].GetComponent<Fade>();
                    ExitScenario e = objectList.humanPlayers[0].GetComponent<ExitScenario>();
                    e.endScenario = false;
                    f.StartFadeAndMove(pos, rot);
                }
            }
            
        }
        if (Publish != null)
        {
            Publish(new ScenicObectAddEventArg() { gameObject = addedGameObject });
        }

    }
}
