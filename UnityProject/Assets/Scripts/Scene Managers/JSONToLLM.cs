using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.VisualScripting.Antlr3.Runtime;
using Whisper.Samples;

// This script collects all the necessary input data for ScenicSynth
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
    public bool isTranscriptionComplete = false;
    private string sentence;
    private Dictionary<float, string> manualTokenDictionary = new Dictionary<float, string>();
    public bool isLogging = false;

    // Class representing the position of an object
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

    // Class representing the velocity of an object
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

    // Class representing a player in the scene
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

    // Class representing a ball in the scene
    [System.Serializable]
    public class Ball
    {
        public string id;
        public string type = "Ball";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
    }

    // Class representing a goal in the scene
    [System.Serializable]
    public class Goal
    {
        public string id;
        public string type = "Goal";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
    }

    // Class representing a segment of the scene, containing objects and their states
    [System.Serializable]
    public class RootSegment
    {
        public int timestep;
        public List<object> objects = new List<object>();
    }

    public RootSegment myRootSegment = new RootSegment();
    private List<RootSegment> segmentsList = new List<RootSegment>();

    // Initialize necessary components and set the file path for the JSON output
    void Start()
    {
        filename = Application.dataPath + "/sanjit.json";
        keyboard = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        streamingSampleMic = GameObject.FindGameObjectWithTag("stream").GetComponent<StreamingSampleMic>();
    }

    void Update()
    {
        // Placeholder for update logic, currently not implemented
    }

    // Populate the scene objects (players, ball, and goal) with their current states
    public void PopulateSceneObjects()
    {
        if (objectsList.ballObject == null)
        {
            return;
        }

        // Process each player in the scene
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

        // Process the human players (coach)
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
            coach.ballPossession.Add(humanPlayer.GetComponent<HumanInterface>().ballPossession);
        }

        // Process the ball in the scene
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

        // Process the goal in the scene
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
    }

    // Populate the current segment with scene objects and their states
    public void PopulateSegment()
    {
        PopulateSceneObjects();
        myRootSegment.timestep = timelineManager.TimeIndex;
    }

    // Process tokens collected during the scene and log them
    public void ProcessTokens()
    {
        foreach (var token in tokenDictionary)
        {
            Debug.Log($"JSONTOLLM KEY saved at time: {token.Key:F2} seconds, VALUE: {token.Value}");
        }

        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        string tokenJsonString = JsonConvert.SerializeObject(tokenDictionary, settings);
        Debug.Log("Created Token JSON String: " + tokenJsonString);
        isTranscriptionComplete = true;
    }

    // Clean up the sentence by removing unwanted patterns
    private string CleanSentence(string sentence)
    {
        string pattern = @"\b[tT] ?h?e?d? ?r\.com\b|\/ ?s ?port\b";
        return Regex.Replace(sentence, pattern, string.Empty, RegexOptions.IgnoreCase).Trim();
    }

    // Build a sentence from the tokens, attaching annotations where applicable
    private string BuildSentenceFromTokens(Dictionary<int, List<object>> dict)
    {
        foreach (var clickTime in keyboard.annotationTimes)
        {
            int annotationKey = clickTime.Key;
            float clickTimestamp = clickTime.Value;

            int closestKey = tokenDictionary
                .Where(pair => pair.Value.Count > 1)
                .OrderBy(pair => Mathf.Abs((float)pair.Value[1] - clickTimestamp))
                .FirstOrDefault().Key;

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

            List<string> unwantedTokens = new List<string>
                { "BL", "ANK", "AUD", "IO", "_", "urn", "@", "sc", "hem", "as.com", "uk" };

            if (unwantedTokens.Any(unwanted => text.Contains(unwanted)))
            {
                continue;
            }

            text = text.Trim();

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

        sentence = sentenceBuilder.ToString();
        return CleanSentence(sentence);
    }

    // Reset the data for the current segment, clearing stored tokens and annotations
    public void ResetSegmentData()
    {
        myRootSegment = new RootSegment();
        tokenDictionary.Clear();
        keyboard.explanation = "";
        Debug.Log("EXPLANATION AFTER RESET:");
        Debug.Log(keyboard.explanation);
        Debug.Log("AFTER RESET TOKENS");
        ProcessTokens();
        Debug.Log("Segment data has been reset.");
    }

    // Create the final JSON string representing the scene and its annotations
    public void CreateJSONString()
    {
        // Build the sentence before creating JSON
        keyboard.explanation = BuildSentenceFromTokens(tokenDictionary);
        Debug.Log("Constructed Sentence: " + keyboard.explanation);

        // Proceed to serialize the scene data into JSON
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

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

        File.WriteAllText(filename, jsonString);
        Debug.Log($"Segment written to {filename}");
    }


    // Write the JSON data to a file
    public void WriteFile()
    {
        CreateJSONString();
    }

    // Update the scene data on a fixed time interval
    private void FixedUpdate()
    {
        if (isLogging)
        {
            time += 0.02f;
            PopulateSegment();
        }
    }
}
