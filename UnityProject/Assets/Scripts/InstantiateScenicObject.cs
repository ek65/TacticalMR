using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateScenicObject 
{
    ObjectsList objectList;

    public InstantiateScenicObject(Vector3 pos, Quaternion rot, string tag)
    {
        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        Debug.Log(tag);
        //Debug.Log(objectList.modelList);
        AddScenicObject(pos, rot, tag);
        
    }

    private void AddScenicObject(Vector3 pos, Quaternion rot, string tag)
    {
        if (tag == "Ball")
        {
            GameObject ball = MonoBehaviour.Instantiate(objectList.modelList["soccer_ball"], pos, Quaternion.identity);

            // disc.GetComponent<NetworkObject>().Spawn();
            objectList.ballObject = ball;
            objectList.scenicObjects.Add(ball);
        } else if (tag == "Player")
        {
            GameObject scenicPlayer = MonoBehaviour.Instantiate(objectList.modelList["player.scenic"], pos, rot);
            //scenicPlayer.GetComponent<NetworkObject>().Spawn();
            objectList.scenicPlayers.Add(scenicPlayer);
            //objectList.orangePlayers.Add(scenicPlayer.GetComponent<NetworkObject>().NetworkInstanceId);
            Debug.Log("Added Scenic Player");
        }
        else if (tag == "Human")
        {
            if (objectList.humanPlayers.Count == 0)
            {
                GameObject humanPlayer = MonoBehaviour.Instantiate(objectList.modelList["player.human"], pos, rot);
                //scenicPlayer.GetComponent<NetworkObject>().Spawn();
                objectList.humanPlayers.Add(humanPlayer);
                //objectList.orangePlayers.Add(scenicPlayer.GetComponent<NetworkObject>().NetworkInstanceId);
                Debug.Log("Added Human Player");
            }
            else
            {
                try
                {
                    Fade f = objectList.humanPlayers[0].GetComponent<Fade>();
                    f.StartFadeAndMove(pos);
                }
                catch
                {
                    Debug.LogError("Human not spawned in yet");
                }
            }
            
            
        }
    }
}
