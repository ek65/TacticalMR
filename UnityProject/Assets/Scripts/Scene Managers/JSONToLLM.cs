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
    public TimelineManager timelineManager;
    public float time;
    public Dictionary<int, List<object>> tokenDictionary = new Dictionary<int, List<object>>();
    public bool isTranscriptionComplete = false;
    private string sentence;
    private Dictionary<float, string> manualTokenDictionary = new Dictionary<float, string>();
    public bool isLogging = false;
    private bool isAdjusted = false;
    public int recordingNum = -1; // segmentNum
    public bool voiceActivated = false;
    public bool videoIsRecording;
    // private RecorderManager recorderManager;
    public bool loggingStartedByUnpause = false;
    [Tooltip("Used to record system jsons/videos")]
    // used in tandem with python script to run all scenic tests, records jsons and videos in ONE segment
    // if this bool is activated, mic should not be activated 
    public bool activateSystemRecording = false; 

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

    // Class representing the orientation of an object
    [System.Serializable]
    public class Orientation
    {
        public float angle; // Angle with respect to the origin

        public Orientation(Transform transform)
        {
            // Calculate the angle with respect to the origin of the x, y axis
            angle = transform.rotation.eulerAngles.y;
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
    [System.Serializable]
    public class Corner
    {
        public string id;
        public string type = "Bound";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }

    // Class representing a player in the scene
    [System.Serializable]
       public class Player
    {
        public string id;
        public string type;
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<bool> ballPossession = new List<bool>();
        public string behavior;
        public List<Orientation> orientation = new List<Orientation>();
    }

    // Class representing a ball in the scene
    [System.Serializable]
    public class Ball
    {
        public string id;
        public string type = "Ball";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }

    // Class representing a goal in the scene
    [System.Serializable]
    public class Goal
    {
        public string id;
        public string type = "Goal";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
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
        // filename = Application.dataPath + "/sanjit.json";
        keyboard = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        // recorderManager = GameObject.FindGameObjectWithTag("RecorderManager").GetComponent<RecorderManager>()
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
        
        // Process each corner in the scene
        for (int i = 1; i <= 4; i++)
        {
            GameObject corner = GameObject.Find("corner" + i);
            if (corner != null)
            {
                Corner cornerObject = (Corner)myRootSegment.objects.Find(obj => obj is Corner c && c.id == "corner" + i);
                if (cornerObject == null)
                {
                    cornerObject = new Corner { id = "corner" + i , type = "Bound"};
                    myRootSegment.objects.Add(cornerObject);
                }

                cornerObject.position.Add(new Position(corner.transform.position));
                cornerObject.velocity.Add(new Velocity(Vector3.zero));
                cornerObject.orientation.Add(new Orientation(corner.transform));
            }
        }
        
        // Process each player in the scene
        foreach (GameObject currPlayer in objectsList.defensePlayers)
        {
            Player player = (Player)myRootSegment.objects.Find(obj => obj is Player p && p.id == currPlayer.name);
            if (player == null)
            {
                player = new Player
                {
                    id = currPlayer.name,
                    behavior = currPlayer.GetComponent<PlayerInterface>().behavior,
                    type = "Teammate"
                };
                myRootSegment.objects.Add(player);
            }

            player.position.Add(new Position(currPlayer.transform.position));
            player.velocity.Add(new Velocity(currPlayer.GetComponent<PlayerInterface>().currVelocity));
            player.orientation.Add(new Orientation(currPlayer.transform));
            player.ballPossession.Add(currPlayer.GetComponent<PlayerInterface>().ballPossession);
        }
        
        foreach (GameObject currPlayer in objectsList.offensePlayers)
        {
            Player player = (Player)myRootSegment.objects.Find(obj => obj is Player p && p.id == currPlayer.name);
            if (player == null)
            {
                player = new Player
                {
                    id = currPlayer.name,
                    behavior = currPlayer.GetComponent<PlayerInterface>().behavior,
                    type = "Opponent"
                };
                myRootSegment.objects.Add(player);
            }

            player.position.Add(new Position(currPlayer.transform.position));
            player.velocity.Add(new Velocity(currPlayer.GetComponent<PlayerInterface>().currVelocity));
            player.orientation.Add(new Orientation(currPlayer.transform));
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
                    behavior = "expert",
                    type = "Coach" 
                };
                myRootSegment.objects.Add(coach);
            }

            coach.position.Add(new Position(humanPlayer.transform.position));
            Vector3 coachVelocity = keyboard.movement;
            coach.velocity.Add(new Velocity(coachVelocity));
            coach.orientation.Add(new Orientation(humanPlayer.transform));
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
        ballObject.orientation.Add(new Orientation(ball.transform));
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

        // if goal still null, skip this
        if (goal == null)
        {
            return;
        }
        Vector3 zeroVector = Vector3.zero;
        goalObject.velocity.Add(new Velocity(zeroVector));
        goalObject.position.Add(new Position(goal.transform.position));
        goalObject.orientation.Add(new Orientation(goal.transform));
        
        Transform leftPost = goal.transform.Find("goal_leftpost");
        Transform rightPost = goal.transform.Find("goal_rightpost");

        Goal leftGoalPost = (Goal)myRootSegment.objects.Find(obj => obj is Goal g && g.id == "goal_leftpost");
        if (leftGoalPost == null)
        {
            leftGoalPost = new Goal { id = "goal_leftpost", type = "Goal" };
            myRootSegment.objects.Add(leftGoalPost);
        }

        leftGoalPost.position.Add(new Position(leftPost.position));
        leftGoalPost.velocity.Add(new Velocity(Vector3.zero));
        leftGoalPost.orientation.Add(new Orientation(leftPost));

        Goal rightGoalPost = (Goal)myRootSegment.objects.Find(obj => obj is Goal g && g.id == "goal_rightpost");
        if (rightGoalPost == null)
        {
            rightGoalPost = new Goal { id =  "goal_rightpost", type = "Goal" };
            myRootSegment.objects.Add(rightGoalPost);
        }

        rightGoalPost.position.Add(new Position(rightPost.position));
        rightGoalPost.velocity.Add(new Velocity(Vector3.zero));
        rightGoalPost.orientation.Add(new Orientation(rightPost));
       
        
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
        isTranscriptionComplete = true;
        Debug.Log("Created Token JSON String: " + tokenJsonString);
    }
    


// Reset the data for the current segment, clearing stored tokens and annotations
    public void ResetSegmentData()
    {
        myRootSegment = new RootSegment();
        time = 0;
        tokenDictionary.Clear();
        keyboard.explanation = "";
        Debug.Log("EXPLANATION AFTER RESET:");
        Debug.Log(keyboard.explanation);
        Debug.Log("Segment data has been reset.");
    }

    // Create the final JSON string representing the scene and its annotations
    public void CreateJSONString()
    {
        recordingNum++;
        JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        
        // Build the sentence before creating JSON
        if (!activateSystemRecording)
        {
            
            Debug.Log("Constructed Sentence: ");
        } else if (activateSystemRecording)
        {
        }
        
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        if (!activateSystemRecording)
        {
            jsonString = JsonConvert.SerializeObject(new
            {
                scene = new
                {
                    id = jsonDirectory.drillID,
                    language = keyboard.explanation,
                    step = 0.02,
                    objects = myRootSegment.objects,
                    annotations = keyboard.GetAnnotationsAsJson(),
                    tokens = tokenDictionary,
                    clickTimes = keyboard.annotationTimes
                }
            }, settings);
        }
        else
        {
            jsonString = JsonConvert.SerializeObject(new
            {
                scene = new
                {
                    id = jsonDirectory.drillID,
                    step = 0.02,
                    objects = myRootSegment.objects,
                }
            }, settings);
        }

        // DirectoryInfo jsonOutputFolder = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "SampleJsons"));
        filename = jsonDirectory.InstantiateJSONSegmentFilePath(recordingNum) + ".json";
        
        File.WriteAllText(filename, jsonString);
        isAdjusted = false;
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
        if (!activateSystemRecording)
        {
        }
        else if (activateSystemRecording && keyboard.segmentStarted) // if system recording and segment started, we want to start logging
        {
            isLogging = true;
        }
        
        // check both flags before starting logging (voice activity or unpause)
        if (keyboard.segmentStarted && keyboard.activationConditionMet && !isLogging)
        {
            isLogging = true;
            Debug.Log("Started logging");
        }

        if (isLogging)
        {
            // Debug.Log("JSON LOGGING");
            time += 0.02f;
            PopulateSegment();
            // if (!recorderManager.RecorderController.IsRecording())
            // {
            //     recorderManager.StartRecording();
            //     videoIsRecording = recorderManager.RecorderController.IsRecording();
            // }
        }
        else if (!isLogging)
        {
            // if (recorderManager.RecorderController.IsRecording())
            // {
            //     recorderManager.StopRecording();
            //     videoIsRecording = recorderManager.RecorderController.IsRecording();
            // }
        }
    }
}