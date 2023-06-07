using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectsList : MonoBehaviour
{
    //This function keeps track of the ever-increasing amount of gameobjects within the scene.
    //prefabs to spawn or inst
    public GameObject AIPrefab;
    public GameObject PlayerPrefab;
    public GameObject DiscPrefab;
    public GameObject ButtonsPrefab;

    //spawn locations
    public Vector3 orangeSpawnLocation;
    public Vector3 blueSpawnLocation;
    public Vector3 playerSpawnLocation;

    //in-game objects
    public GameObject StartingBarrierObject;
    public GameObject DiscObject;
    public List<ulong> bluePlayers;
    public List<ulong> orangePlayers;

    public List<GameObject> scenicPlayers;
    public List<GameObject> humanPlayers;
    public List<GameObject> scenicObjects;
    public Dictionary<string, GameObject> modelList;

    void Start()
    {
        bluePlayers = new List<ulong>();
        orangePlayers = new List<ulong>();
        scenicPlayers = new List<GameObject>();
        humanPlayers = new List<GameObject>();
        modelList = new Dictionary<string, GameObject>();
        scenicObjects = new List<GameObject>();
        InitModelDict();
    }
    public void addToBlue(ulong id)
    {
        bluePlayers.Add(id);
    }

    public void addToOrange(ulong id)
    {
        orangePlayers.Add(id);
    }

    public void removeFromBlue(ulong id)
    {
        bluePlayers.Remove(id);
    }

    public void removeFromOrange(ulong id)
    {
        orangePlayers.Remove(id);
    }
    private void InitModelDict()
    {
        List<GameObject> models = new List<GameObject>();
        models.AddRange(Resources.LoadAll<GameObject>("Prefabs/XR Rigs"));
        models.AddRange(Resources.LoadAll<GameObject>("Prefabs/Misc Models"));
        models.AddRange(Resources.LoadAll<GameObject>("Prefabs/Discs"));
        models.AddRange(Resources.LoadAll<GameObject>("Prefabs/Env Models"));
        foreach (GameObject obj in models)
        {
            modelList.Add(obj.name, obj);
        }
    }
    public void RemoveScenicObj(GameObject obj)
    {
        bool inScenicPlayers = scenicPlayers.Remove(obj);
        bool inScenicObjects = false;
        if (!inScenicPlayers)
        {
            inScenicObjects = scenicObjects.Remove(obj);
        }
        if (!inScenicObjects && !inScenicPlayers)
        {
            Debug.LogWarning("Could not remove scenic obj: " + obj.ToString());
        }
    }
    /*public void Reset()
    {
        foreach (GameObject obj in scenicObjects)
        {
            Destroy(obj);
        }
        foreach (GameObject player in scenicPlayers)
        {
            Destroy(player);
        }
        scenicObjects = new List<GameObject>();
        scenicPlayers = new List<GameObject>();
        Destroy(DiscObject);
        DiscObject = null;
        //call reset function on the ready boolean for human and index
        humanPlayers[0].GetComponentInChildren<HumanInterface>().ResetValues();
    }*/
    public void RemoveAllHumans()
    {
        humanPlayers = new List<GameObject>();
    }
}
