using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Whisper.Samples;

// This script is collects all the necessary input data for ScenicSynth
public class JSONToLLM : MonoBehaviour
{
    public KeyboardInput keyboard;
    public ObjectsList objectsList;
    private string filename;
    public string jsonString;
    public int segments;
    public TimelineManager timelineManager;
    public StreamingSampleMic streamingSampleMic;
    public float time;
    public Dictionary<float, string> tokenDictionary = new Dictionary<float, string>();


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
        public List<Velocity> velocity = new List<Velocity>();
    }

    [System.Serializable]
    public class Goal
    {
        public string id;
        public string type = "Goal";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
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
        keyboard = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        streamingSampleMic = GameObject.FindGameObjectWithTag("stream").GetComponent<StreamingSampleMic>();
    }

    void Update()
    {
    }

    public void PopulateSceneObjects()
    {
        foreach (GameObject currPlayer in objectsList.scenicPlayers) // find all the objects in the scenic scene
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
            Vector3 coachVelocity = keyboard.movement;
            coach.velocity.Add(new Velocity(coachVelocity));
            // TODO: add ball posession to unity human interface
            coach.ballPossession.Add(humanPlayer.GetComponent<HumanInterface>().ballPossession);
        }

        GameObject ball = objectsList.ballObject;
        Ball ballObject = (Ball)myRootSegment.objects.Find(obj => obj is Ball);
        if (ballObject == null)
        {
            ballObject = new Ball { id = "ball" };
            myRootSegment.objects.Add(ballObject);
        }
        ballObject.position.Add(new Position(ball.transform.position));
        Rigidbody ballRB = ball.GetComponent<Rigidbody>();
        ballObject.velocity.Add(new Velocity(ballRB.velocity));

        GameObject goal = objectsList.goalObject;
        Goal goalObject = (Goal)myRootSegment.objects.Find(obj => obj is Goal);
        if (goalObject == null)
        {
            goalObject = new Goal { id = "goal" };
            myRootSegment.objects.Add(goalObject);
        }
        Vector3 zeroVector = Vector3.zero;
        goalObject.velocity.Add(new Velocity(zeroVector));
        goalObject.position.Add(new Position(goal.transform.position));
        // goalObject.rotation.Add(goal.transform.rotation.eulerAngles);
    }

    public void PopulateSegment()
    {
        PopulateSceneObjects();
        myRootSegment.timestep = timelineManager.TimeIndex;
    }
    public void ConvertTokensToSentence()
    {
        var sortedTokens = tokenDictionary.OrderBy(kv => kv.Key);
        string sentence = string.Join(" ", sortedTokens.Select(kv => kv.Value));
        keyboard.explanation = sentence;
        Debug.Log("Constructed Sentence: " + sentence);
    }
    public void ProcessTokensAndClickTimes()
    {
        var cleanedTokens = tokenDictionary.Where(kv => !kv.Value.Contains("[") && !kv.Value.Contains("]") && !kv.Value.Contains("BLANK"))
            .ToDictionary(kv => kv.Key, kv => kv.Value);        foreach (var clickTime in keyboard.annotationTimes)
        {
            var closestTime = FindClosestTime(cleanedTokens, clickTime.Value);
            cleanedTokens[closestTime] += $" [{clickTime.Key}]";
        }
        tokenDictionary = cleanedTokens;
        foreach (var token in tokenDictionary)
        {
            Debug.Log($"{token.Key}: {token.Value}");
        }
    }

    float FindClosestTime(Dictionary<float, string> tokensDict, float clickTime)
    {
        return tokensDict.Keys.OrderBy(t => Math.Abs(t - clickTime)).First();
    }

    public void CreateJSONString()
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        
        ProcessTokensAndClickTimes();
        ConvertTokensToSentence();
        jsonString = JsonConvert.SerializeObject(new
        {
            scene = new
            {
                id = "typical_1v1",
                language = keyboard.explanation,
                step = 0.02,
                objects = myRootSegment.objects,
                annotations = keyboard.GetAnnotationsAsJson(),
                tokens = tokenDictionary,
                clickTimes = keyboard.annotationTimes
            }
        }, settings);
    }
    

    public void WriteFile()
    {
        CreateJSONString();
        File.WriteAllText(filename, jsonString);
        Debug.Log($"Segment written to {filename}");
    }
    

    private void FixedUpdate()
    {
        time += 0.02f;
        // Debug.Log(time);
        PopulateSegment();
    }
}
