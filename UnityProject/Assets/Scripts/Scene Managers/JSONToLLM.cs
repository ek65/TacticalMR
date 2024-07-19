using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class JSONToLLM : MonoBehaviour
{
    public KeyboardInput keyboard;
    public ObjectsList objectsList;
    private string filename;
    private string jsonString;
    public int segments;
    public TimelineManager timelineManager;

    [System.Serializable]
    public class Position
    {
        public float x;
        public float y;

        public Position(Vector3 vector)
        {
            x = vector.x;
            y = vector.z;
        }
    }

    [System.Serializable]
    public class Velocity
    {
        public float x;
        public float y;

        public Velocity(Vector3 vector)
        {
            float magnitude = vector.magnitude;
            x = magnitude * vector.x;
            y = magnitude * vector.z;
        }
    }

    [System.Serializable]
    public class Player
    {
        public string id;
        public string type = "Player";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<bool> ballPossession = new List<bool>();
        public string behavior;
    }

    [System.Serializable]
    public class Ball
    {
        public string id;
        public string type = "Ball";
        public List<Position> position = new List<Position>();
    }

    [System.Serializable]
    public class Goal
    {
        public string id;
        public string type = "Goal";
        public List<Position> position = new List<Position>();
        // public List<Vector3> rotation = new List<Vector3>();
    }

    [System.Serializable]
    public class RootSegment
    {
        public int timestep;
        public List<object> objects = new List<object>();
    }

    public RootSegment myRootSegment = new RootSegment();
    private List<RootSegment> segmentsList = new List<RootSegment>();

    void Start()
    {
        filename = Application.dataPath + "/sanjit.json";
        keyboard = GameObject.FindGameObjectWithTag("player").GetComponent<KeyboardInput>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
    }

    void Update()
    {
    }

    public void PopulateSceneObjects()
    {
        foreach (GameObject currPlayer in objectsList.scenicPlayers)
        {
            Player player = (Player)myRootSegment.objects.Find(obj => obj is Player p && p.id == currPlayer.name);
            if (player == null)
            {
                player = new Player
                {
                    id = currPlayer.name,
                    behavior = currPlayer.GetComponent<PlayerInterface>().behavior
                };
                myRootSegment.objects.Add(player);
            }
            player.position.Add(new Position(currPlayer.transform.position));
            player.velocity.Add(new Velocity(currPlayer.GetComponent<PlayerInterface>().currVelocity));
            player.ballPossession.Add(currPlayer.GetComponent<PlayerInterface>().ballPossession);
        }

        foreach (GameObject humanPlayer in objectsList.humanPlayers)
        {
            Player coach = (Player)myRootSegment.objects.Find(obj => obj is Player p && p.id == humanPlayer.name);
            if (coach == null)
            {
                coach = new Player
                {
                    id = humanPlayer.name,
                    behavior = "expert"
                };
                myRootSegment.objects.Add(coach);
            }
            coach.position.Add(new Position(humanPlayer.transform.position));
            Vector3 coachVelocity = humanPlayer.GetComponent<KeyboardInput>().movement;
            coach.velocity.Add(new Velocity(coachVelocity));
            // TODO: add ball posession to unity human interface
            coach.ballPossession.Add(humanPlayer.GetComponent<PlayerInterface>().ballPossession);
        }

        GameObject ball = objectsList.ballObject;
        Ball ballObject = (Ball)myRootSegment.objects.Find(obj => obj is Ball b && b.id == ball.name);
        if (ballObject == null)
        {
            ballObject = new Ball { id = ball.name };
            myRootSegment.objects.Add(ballObject);
        }
        ballObject.position.Add(new Position(ball.transform.position));

        GameObject goal = objectsList.goalObject;
        Goal goalObject = (Goal)myRootSegment.objects.Find(obj => obj is Goal g && g.id == goal.name);
        if (goalObject == null)
        {
            goalObject = new Goal { id = goal.name };
            myRootSegment.objects.Add(goalObject);
        }
        goalObject.position.Add(new Position(goal.transform.position));
        // goalObject.rotation.Add(goal.transform.rotation.eulerAngles);
    }

    public void PopulateSegment()
    {
        PopulateSceneObjects();
        myRootSegment.timestep = timelineManager.TimeIndex;
    }

    public void CreateJSONString()
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        jsonString = JsonConvert.SerializeObject(new { scene = new { id = "typical_1v1", step = 0.1, objects = myRootSegment.objects }, annotations = keyboard.annotation }, settings);
    }

    public void WriteFile()
    {
        CreateJSONString();
        File.WriteAllText(filename, jsonString);
        Debug.Log($"Segment written to {filename}");
    }

    public void AppendToObjects()
    {
        PopulateSegment();
    }

    private void FixedUpdate()
    {
        keyboard.editJSON();
    }
}
