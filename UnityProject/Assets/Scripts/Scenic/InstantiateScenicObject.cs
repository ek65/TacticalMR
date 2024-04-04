using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ScenicObectAddEventArg : EventArgs { public GameObject gameObject { get; set; } }
public class InstantiateScenicObject 
{
    ObjectsList objectList;
    public delegate void PublishScenicAddObjectEvent(ScenicObectAddEventArg arg);
    //timeline manager will subscribe to this event
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
            addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["soccer_ball"], pos, Quaternion.identity);
            addedGameObject.name = "Ball";
            // disc.GetComponent<NetworkObject>().Spawn();
            objectList.ballObject = addedGameObject;
            objectList.scenicObjects.Add(addedGameObject);
            
        }
        else if (modelType == "goal")
        {
            addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["goal"], pos, rot);
            // assuming always 1 goal for now
            addedGameObject.name = "Goal";
            objectList.goalObject = addedGameObject;
            objectList.scenicObjects.Add(addedGameObject);
        }
        // else if (tag == "aiAgent")
        // {
        //     addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["Convai NPC Daniel He"], pos, Quaternion.Euler(-90,0,0));
        //     addedGameObject.transform.parent = GameObject.Find("AI Interface").transform;
        //     objectList.AIAgent = addedGameObject;
        // }
        else if (modelType == "Player")
        {
            addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["player.scenic"], pos, rot);
            addedGameObject.name = name;
            //scenicPlayer.GetComponent<NetworkObject>().Spawn();
            objectList.scenicPlayers.Add(addedGameObject);
            if (color == new Color(0f, 0f, 255f, 1f)) // defense team
            {
                addedGameObject.GetComponentInChildren<PlayerInterface>().ally = true;
                objectList.defensePlayers.Add(addedGameObject);
            }
            else if (color == new Color(255f, 0f, 0f, 1f)) // offense team
            {
                addedGameObject.GetComponentInChildren<PlayerInterface>().enemy = true;
                objectList.offensePlayers.Add(addedGameObject);
            }
            
            //objectList.orangePlayers.Add(scenicPlayer.GetComponent<NetworkObject>().NetworkInstanceId);
            Debug.Log("Added Scenic Player");
        }
        else if (modelType == "Human")
        {
            if (objectList.humanPlayers.Count == 0)
            {
                addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["player.human"], pos, rot);
                //scenicPlayer.GetComponent<NetworkObject>().Spawn();
                addedGameObject.name = "Coach";
                objectList.humanPlayers.Add(addedGameObject);
                //objectList.orangePlayers.Add(scenicPlayer.GetComponent<NetworkObject>().NetworkInstanceId);
                Debug.Log("Added Human Player");
            }
            else
            {
                try
                {
                    addedGameObject = objectList.humanPlayers[0];
                    Fade f = objectList.humanPlayers[0].GetComponent<Fade>();
                    ExitScenario e = objectList.humanPlayers[0].GetComponent<ExitScenario>();
                    e.endScenario = false;
                    f.StartFadeAndMove(pos);
                }
                catch
                {
                    Debug.LogError("Human not spawned in yet");
                }
            }
            
        }
        if (Publish != null)
        {
            Publish(new ScenicObectAddEventArg() { gameObject = addedGameObject });
        }

    }
}
