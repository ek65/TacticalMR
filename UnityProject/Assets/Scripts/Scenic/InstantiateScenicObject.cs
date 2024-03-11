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

    public InstantiateScenicObject(Vector3 pos, Quaternion rot, string tag)
    {
        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        Debug.Log(tag);
        //Debug.Log(objectList.modelList);
        AddScenicObject(pos, rot, tag);
        
    }

    private void AddScenicObject(Vector3 pos, Quaternion rot, string tag)
    {
        GameObject addedGameObject = null;
        if (tag == "Ball")
        {
            addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["soccer_ball"], pos, Quaternion.identity);

            // disc.GetComponent<NetworkObject>().Spawn();
            objectList.ballObject = addedGameObject;
            objectList.scenicObjects.Add(addedGameObject);
            
        }
        else if (tag == "goal")
        {
            addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["goal"], pos, rot);
            objectList.scenicObjects.Add(addedGameObject);
        }
        else if (tag == "Player")
        {
            addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["player.scenic"], pos, rot);
            //scenicPlayer.GetComponent<NetworkObject>().Spawn();
            objectList.scenicPlayers.Add(addedGameObject);
            //objectList.orangePlayers.Add(scenicPlayer.GetComponent<NetworkObject>().NetworkInstanceId);
            Debug.Log("Added Scenic Player");
        }
        else if (tag == "Human")
        {
            if (objectList.humanPlayers.Count == 0)
            {
                addedGameObject = MonoBehaviour.Instantiate(objectList.modelList["player.human"], pos, rot);
                //scenicPlayer.GetComponent<NetworkObject>().Spawn();
                objectList.humanPlayers.Add(addedGameObject);
                //objectList.orangePlayers.Add(scenicPlayer.GetComponent<NetworkObject>().NetworkInstanceId);
                Debug.Log("Added Human Player");
            }
            else
            {
                try
                {
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
