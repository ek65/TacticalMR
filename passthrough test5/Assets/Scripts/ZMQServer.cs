using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Newtonsoft.Json;
public class ZMQServer : MonoBehaviour
{
    [SerializeField] private string ip;

    [SerializeField] private string port = "5555";

    public GameObject obj;
    // Start is called before the first frame update
    private ZMQTest zmq;
        
    void Start()
    {
        if (ip == null || port == null)
        {
            throw new System.Exception();
        }

        bool isServer = true;
        zmq = new ZMQTest(ip, port, isServer);
        zmq.Start();
        
    }
    void Update()
    {
        string data = zmq.data;
        if (data == null)
        {
            return;
        }
        Debug.Log(data);
        try
        {
            var jsonResult = JsonConvert.DeserializeObject(data).ToString();
            Data d = JsonConvert.DeserializeObject<Data>(jsonResult);
            MovementData mvData = HandleMovementData(d); // convert data types to Unity readable types
            ApplyMovement(mvData); // apply json movement from mvData to obj
        }
        catch (NullReferenceException e)
        {
            Debug.LogError("json failed" + e);
        }

    }

    public void ApplyMovement(MovementData mvData)
    {
        if (mvData.tag == "player")
        {
            // spawn obj at mvData.position if not instantiated yet
            if (obj.active == false)
            {
                Debug.Log("spawn position: " + mvData.position);
                obj.transform.position = mvData.position;
                obj.SetActive(true);
            }

            var player = obj.GetComponent<MoveToSoccerBallAndTurn>();
            if (mvData.doMove)
            {
                player.MoveToBallThenLook(player.goal.position);
            } else if (mvData.doKick)
            {
                player.KickBall(mvData.kickPosition);
            }
            
        }
    }

    private class Data
    {
        [JsonProperty("tag")] public string tag { get; set; }
        [JsonProperty("position")] public List<float> position { get; set; }
        [JsonProperty("doMove")] public bool doMove { get; set; }
        [JsonProperty("doKick")] public bool doKick { get; set; }
        [JsonProperty("kickPosition")] public List<float> kickPosition { get; set; }
    }



    private MovementData HandleMovementData(Data data)
    {
        MovementData mvData = new MovementData();
        mvData.tag = data.tag;
        // if obj not instantiated yet, this is the spawn position
        if (obj.active == false)
        {
            mvData.position = ListToVector(data.position);
        }
        mvData.doMove = data.doMove;
        mvData.doKick = data.doKick;
        mvData.kickPosition = ListToVector(data.kickPosition);
        return mvData;
    }
    
    private Vector3 ListToVector(List<float> v)
    {
        /*
        v[0] = x
        v[1] = y
        v[2] = z
        NOTE: We swap y and z because scenic uses z as vertical axis
        ^ not doing this right now, may need to change it back to this later when scenic implemented
        */
        return new Vector3(v[0], v[1], v[2]);
    }
    
}
