using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.VisualScripting.Antlr3.Runtime;
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
    public Dictionary<int, List<object>> tokenDictionary = new Dictionary<int, List<object>>();

    private string sentence;
    private Dictionary<float, string> manualTokenDictionary = new Dictionary<float, string>();

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
    

    public void ProcessTokens()
    {
        // Log the tokens for debugging
        foreach (var token in tokenDictionary)
        {
            Debug.Log($"JSONTOLLM KEY saved at time: {token.Key:F2} seconds, VALUE: {token.Value}");
        }
        
        
        // Convert the manually created dictionary to a JSON string
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        string tokenJsonString = JsonConvert.SerializeObject(tokenDictionary, settings);
        // Log the manually created JSON string for verification
        Debug.Log("Created Token JSON String: " + tokenJsonString);
    }


    private string BuildSentenceFromTokens(Dictionary<int, List<object>> dict)
    {
        foreach (var clickTime in keyboard.annotationTimes)
        {
            int annotationKey = clickTime.Key;
            float clickTimestamp = clickTime.Value;

            // Find the closest time in tokenDictionary (comparing the float timestamp part of List<object>)
            int closestKey = tokenDictionary
                .Where(pair => pair.Value.Count > 1)
                .OrderBy(pair => Mathf.Abs((float)pair.Value[1] - clickTimestamp))
                .FirstOrDefault().Key;

            // Append the annotation key as a string to the corresponding text in the closest token list
            if (tokenDictionary.ContainsKey(closestKey))
            {
                tokenDictionary[closestKey][0] = $"{tokenDictionary[closestKey][0]} [{annotationKey}]";
                Debug.Log(
                    $"Appended annotation key [{annotationKey}] to token at order {closestKey} with time {((float)tokenDictionary[closestKey][1]):F2} seconds");
            }
        }

        StringBuilder sentenceBuilder = new StringBuilder();
        bool lastWasPunctuation = false;
        
        
        foreach (var tokenEntry in tokenDictionary)
        {
            string text = tokenEntry.Value[0] as string;        
            
            // Skip any placeholders like BLANK_AUDIO if needed
            List<string> unwantedTokens = new List<string> { "BL", "ANK", "AUD", "IO", "_", "urn", "@", "sc", "hem", "as.com", "uk" };

            if (unwantedTokens.Any(unwanted => text.Contains(unwanted)))
            {
                continue;
            }

            // Trim any leading/trailing spaces from the token
            text = text.Trim();

            // Check if the token is punctuation
            if (text == "," || text == "." || text == "!" || text == "?")
            {
                sentenceBuilder.Append(text);
                lastWasPunctuation = true;
            }
            else
            {
                if (sentenceBuilder.Length > 0 && !lastWasPunctuation && !text.StartsWith("'"))
                {
                    sentenceBuilder.Append(" ");
                }

                sentenceBuilder.Append(text);
                lastWasPunctuation = false;
            }
        }
        
        return sentenceBuilder.ToString();
    }

    public void ResetSegmentData()
    {
        myRootSegment = new RootSegment();
        tokenDictionary.Clear();  // Clear token dictionary
        keyboard.explanation = "";
        Debug.Log("EXPLANATION AFTER RESET:");
        Debug.Log(keyboard.explanation);
        Debug.Log("AFTER RESET TOKENS");
        ProcessTokens();
        Debug.Log("Segment data has been reset.");
    }

    public void CreateJSONString()
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
            ,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        keyboard.explanation = BuildSentenceFromTokens(tokenDictionary);
        Debug.Log("Constructed Sentence: " + keyboard.explanation);
        
        // Construct the final JSON string
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

        // Write the JSON string to the file
        File.WriteAllText(filename, jsonString);
        Debug.Log($"Segment written to {filename}");
    }
    
    

public void WriteFile()
    {
        CreateJSONString();
    }
    

    private void FixedUpdate()
    {
        time += 0.02f;
        // Debug.Log(time);
        PopulateSegment();
    }
}
