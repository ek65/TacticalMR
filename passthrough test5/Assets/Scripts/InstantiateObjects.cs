using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateObjects : MonoBehaviour
{
    ObjectsList objectList;

    public InstantiateObjects(Vector3 pos, Quaternion rot, string tag)
    {
        objectList = GameObject.FindGameObjectWithTag("controllerTestTag").GetComponent<ObjectsList>();
        AddScenicPlayer(pos, rot, tag);
    }

    private void AddScenicPlayer(Vector3 pos, Quaternion rot, string tag)
    {
        if (tag == "Disc")
        {
            GameObject disc = MonoBehaviour.Instantiate(objectList.modelList["Frisbee"], pos, Quaternion.identity);

            // disc.GetComponent<NetworkObject>().Spawn();
            objectList.DiscObject = disc;
            objectList.scenicObjects.Add(disc);
        }
    }
}
