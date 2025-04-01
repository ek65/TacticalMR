using System.Collections;
using System.Collections.Generic;
using Fusion;
using Oculus.Interaction;
using UnityEngine;

public class ObjectsList : NetworkBehaviour
{
    //in-game objects created from Scenic
    public GameObject ballObject;
    public GameObject goalObject;
    // public List<ulong> bluePlayers;
    // public List<ulong> orangePlayers;
    public List<GameObject> defensePlayers;
    public List<GameObject> offensePlayers;

    public List<GameObject> scenicPlayers;
    public List<GameObject> humanPlayers;
    public List<GameObject> scenicObjects;

    public GameObject viewerPlayer;
    // public GameObject AIAgent; 
    
    public Dictionary<string, GameObject> modelList;

    void Awake()
    {
        // bluePlayers = new List<ulong>();
        // orangePlayers = new List<ulong>();
        defensePlayers = new List<GameObject>();
        offensePlayers = new List<GameObject>();
        scenicPlayers = new List<GameObject>();
        humanPlayers = new List<GameObject>();
        modelList = new Dictionary<string, GameObject>();
        scenicObjects = new List<GameObject>();
        InitModelDict();
    }
    // public void addToBlue(ulong id)
    // {
    //     bluePlayers.Add(id);
    // }
    //
    // public void addToOrange(ulong id)
    // {
    //     orangePlayers.Add(id);
    // }
    //
    // public void removeFromBlue(ulong id)
    // {
    //     bluePlayers.Remove(id);
    // }
    //
    // public void removeFromOrange(ulong id)
    // {
    //     orangePlayers.Remove(id);
    // }
    private void InitModelDict()
    {
        List<GameObject> models = new List<GameObject>();
        models.AddRange(Resources.LoadAll<GameObject>("Prefabs/Balls"));
        models.AddRange(Resources.LoadAll<GameObject>("Prefabs/Characters"));
        models.AddRange(Resources.LoadAll<GameObject>("Prefabs/Goal"));
        // models.AddRange(Resources.LoadAll<GameObject>("Prefabs/Misc Models"));
        // models.AddRange(Resources.LoadAll<GameObject>("Prefabs/Env Models"));
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
    public void Reset()
    {
        RPC_ResetRay();
        NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
        foreach (GameObject obj in scenicObjects)
        {
            runner.Despawn(obj.GetComponent<NetworkObject>());
        }
        foreach (GameObject player in scenicPlayers)
        {
            runner.Despawn(player.GetComponent<NetworkObject>());
        }
        foreach (GameObject player in offensePlayers)
        {
            runner.Despawn(player.GetComponent<NetworkObject>());
        }
        foreach (GameObject player in defensePlayers)
        {
            runner.Despawn(player.GetComponent<NetworkObject>());
        }
        if (ballObject)
        {
            runner.Despawn(ballObject.GetComponent<NetworkObject>());
        }

        if (goalObject)
        {
            runner.Despawn(goalObject.GetComponent<NetworkObject>());
        }
        
        // foreach (GameObject human in humanPlayers)
        // {
        //     Destroy(human);
        // }
        // RemoveAllHumans();
        // scenicObjects = new List<GameObject>();
        // scenicPlayers = new List<GameObject>();
        // offensePlayers = new List<GameObject>();
        // defensePlayers = new List<GameObject>();
        // ballObject = null;
        // goalObject = null;
        RPC_ResetLists();
        TimelineManager tlManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        tlManager.Reset();

        BallOwnership ballOwnership = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<BallOwnership>();
        ballOwnership.heldByHuman = false;
        ballOwnership.heldByScenic = false;
        ballOwnership.ballOwner = null;
        
        if (GameObject.FindGameObjectWithTag("human") != null)
        {
            HumanInterface humanInterface = GameObject.FindGameObjectWithTag("human").GetComponent<HumanInterface>();
            humanInterface.ResetHuman();
        }
        //call reset function on the ready boolean for human and index
        // humanPlayers[0].GetComponentInChildren<HumanInterface>().ResetValues();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetRay()
    {
        // Coroutine to disable right ray for a few seconds
        StartCoroutine(ResetRay());
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetLists()
    {
        foreach (GameObject obj in scenicObjects)
        {
            Destroy(obj);
        }
        foreach (GameObject player in scenicPlayers)
        {
            Destroy(player);
        }
        foreach (GameObject player in offensePlayers)
        {
            Destroy(player);
        }
        foreach (GameObject player in defensePlayers)
        {
            Destroy(player);
        }

        if (ballObject)
        {
            Destroy(ballObject);
        }

        if (goalObject)
        {
            Destroy(goalObject);
        }
        
        scenicObjects = new List<GameObject>();
        scenicPlayers = new List<GameObject>();
        offensePlayers = new List<GameObject>();
        defensePlayers = new List<GameObject>();
        ballObject = null;
        goalObject = null;
    }

    IEnumerator ResetRay()
    {
        RayInteractor rayInteractor = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>();
        rayInteractor.enabled = false;
        yield return new WaitForSeconds(1f);
        rayInteractor.enabled = true;
    }
    public void RemoveAllHumans()
    {
        humanPlayers = new List<GameObject>();
    }
}
