using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateScenicObject : MonoBehaviour
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
        }
    }
}
