using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq; // For .OrderBy(...)
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.VisualScripting.Antlr3.Runtime;
using Whisper.Samples;

public class JSONToLLM : MonoBehaviour
{
    public KeyboardInput keyboard;
    public ObjectsList objectsList;
    private string filename;
    public string jsonString;
    public TimelineManager timelineManager;
    
    // So we can start/stop the video in FixedUpdate
    public RecorderManager recorderManager;

    public float time;  // Time index for capturing scene data
    public bool isTranscriptionComplete = false;
    public bool isLogging = false;

    public int recordingNum = -1; 
    public bool voiceActivated = false;
    public bool videoIsRecording;
    public bool loggingStartedByUnpause = false;

    [Tooltip("Used to record system jsons/videos")]
    public bool activateSystemRecording = false; 

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
    public class Orientation
    {
        public float angle; 
        public Orientation(Transform transform)
        {
            angle = transform.rotation.eulerAngles.y;
        }
    }

    [System.Serializable]
    public class Velocity
    {
        public float x;
        public float y;
        public Velocity(Vector3 vector)
        {
            x = vector.x;
            y = vector.z;
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

    [System.Serializable]
    public class Player
    {
        public string id;
        public string type;
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        // public List<bool> ballPossession = new List<bool>();
        public string behavior;
        public List<Orientation> orientation = new List<Orientation>();
    }

    [System.Serializable]
    public class Ball
    {
        public string id;
        public string type = "Ball";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }

    [System.Serializable]
    public class Goal
    {
        public string id;
        public string type = "Goal";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }
    
    [System.Serializable]
    public class Object
    {
        public string id;
        public string type = "Object";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
    }

    [System.Serializable]
    public class RootSegment
    {
        public int timestep;
        public List<object> objects = new List<object>();
    }

    public RootSegment myRootSegment = new RootSegment();
    private List<RootSegment> segmentsList = new List<RootSegment>();

    // Key = timestamp float, Value = list of tokens (strings or placeholders)
    public Dictionary<float, List<object>> tokenDictionary = new Dictionary<float, List<object>>();

    private Scribe scribe;

    void Start()
    {
        keyboard = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        
        // The RecorderManager is presumably on a GameObject tagged "RecorderManager"
        recorderManager = GameObject.FindGameObjectWithTag("RecorderManager").GetComponent<RecorderManager>();

        scribe = FindObjectOfType<Scribe>();
        if (scribe != null)
        {
            scribe.OnTranscriptionComplete += HandleTranscriptionComplete;
        }
        else
        {
            Debug.LogWarning("JSONToLLM: No Scribe found—cannot subscribe to OnTranscriptionComplete.");
        }
    }

    private void OnDestroy()
    {
        if (scribe != null)
        {
            scribe.OnTranscriptionComplete -= HandleTranscriptionComplete;
        }
    }

    void Update()
    {
        // any per-frame logic here
    }

    // Called in FixedUpdate if isLogging = true
    public void PopulateSegment()
    {
        PopulateSceneObjects();
        myRootSegment.timestep = timelineManager.TimeIndex;
    }
    
  public void PopulateSceneObjects()
    {
        // if (objectsList.ballObject == null)
        // {
        //     return;
        // }
        
        // Process each corner in the scene
        // for (int i = 1; i <= 4; i++)
        // {
        //     GameObject corner = GameObject.Find("corner" + i);
        //     if (corner != null)
        //     {
        //         Corner cornerObject = (Corner)myRootSegment.objects.Find(obj => obj is Corner c && c.id == "corner" + i);
        //         if (cornerObject == null)
        //         {
        //             cornerObject = new Corner { id = "corner" + i , type = "Bound"};
        //             myRootSegment.objects.Add(cornerObject);
        //         }
        //
        //         cornerObject.position.Add(new Position(corner.transform.position));
        //         cornerObject.velocity.Add(new Velocity(Vector3.zero));
        //         cornerObject.orientation.Add(new Orientation(corner.transform));
        //     }
        // }
        
        // Process each player in the scene
        // foreach (GameObject currPlayer in objectsList.defensePlayers)
        // {
        //     Player player = (Player)myRootSegment.objects.Find(obj => obj is Player p && p.id == currPlayer.name);
        //     if (player == null)
        //     {
        //         player = new Player
        //         {
        //             id = currPlayer.name,
        //             behavior = currPlayer.GetComponent<PlayerInterface>().behavior,
        //             type = "Teammate"
        //         };
        //         myRootSegment.objects.Add(player);
        //     }
        //
        //     player.position.Add(new Position(currPlayer.transform.position));
        //     player.velocity.Add(new Velocity(currPlayer.GetComponent<PlayerInterface>().currVelocity));
        //     player.orientation.Add(new Orientation(currPlayer.transform));
        //     // player.ballPossession.Add(currPlayer.GetComponent<PlayerInterface>().ballPossession);
        // }
        
        foreach (GameObject currPlayer in objectsList.scenicPlayers)
        {
            Player player = (Player)myRootSegment.objects.Find(obj => obj is Player p && p.id == currPlayer.name);
            if (player == null)
            {
                player = new Player
                {
                    id = currPlayer.name,
                    behavior = currPlayer.GetComponent<PlayerInterface>().behavior,
                    type = "Worker"
                };
                myRootSegment.objects.Add(player);
            }

            player.position.Add(new Position(currPlayer.transform.position));
            player.velocity.Add(new Velocity(currPlayer.GetComponent<PlayerInterface>().currVelocity));
            player.orientation.Add(new Orientation(currPlayer.transform));
            // player.ballPossession.Add(currPlayer.GetComponent<PlayerInterface>().ballPossession);
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
            // coach.ballPossession.Add(humanPlayer.GetComponent<HumanInterface>().ballPossession);
        }

        // process misc objects in the scene
        // foreach (GameObject obj in objectsList.miscObjects)
        // {
        //     Object miscObject = (Object)myRootSegment.objects.Find(o => o is Object m && m.id == obj.name);
        //     if (miscObject == null)
        //     {
        //         miscObject = new Object { id = obj.name, type = "Misc" };
        //         myRootSegment.objects.Add(miscObject);
        //     }
        //
        //     miscObject.position.Add(new Position(obj.transform.position));
        //     miscObject.velocity.Add(new Velocity(Vector3.zero)); 
        //     miscObject.orientation.Add(new Orientation(obj.transform));
        // }
        
        // Process the ball in the scene
        // GameObject ball = objectsList.ballObject;
        // Ball ballObject = (Ball)myRootSegment.objects.Find(obj => obj is Ball);
        // if (ballObject == null)
        // {
        //     ballObject = new Ball { id = "ball" };
        //     myRootSegment.objects.Add(ballObject);
        // }
        //
        // ballObject.position.Add(new Position(ball.transform.position));
        // ballObject.orientation.Add(new Orientation(ball.transform));
        // Rigidbody ballRB = ball.GetComponent<Rigidbody>();
        // ballObject.velocity.Add(new Velocity(ballRB.velocity));

        // Process the goal in the scene
        // GameObject goal = objectsList.goalObject;
        // Goal goalObject = (Goal)myRootSegment.objects.Find(obj => obj is Goal);
        // if (goalObject == null)
        // {
        //     goalObject = new Goal { id = "goal" };
        //     myRootSegment.objects.Add(goalObject);
        // }

        // if goal still null, skip this
        // if (goal == null)
        // {
        //     return;
        // }
        // Vector3 zeroVector = Vector3.zero;
        // goalObject.velocity.Add(new Velocity(zeroVector));
        // goalObject.position.Add(new Position(goal.transform.position));
        // goalObject.orientation.Add(new Orientation(goal.transform));
        //
        // Transform leftPost = goal.transform.Find("goal_leftpost");
        // Transform rightPost = goal.transform.Find("goal_rightpost");

        // Goal leftGoalPost = (Goal)myRootSegment.objects.Find(obj => obj is Goal g && g.id == "goal_leftpost");
        // if (leftGoalPost == null)
        // {
        //     leftGoalPost = new Goal { id = "goal_leftpost", type = "Goal" };
        //     myRootSegment.objects.Add(leftGoalPost);
        // }
        //
        // leftGoalPost.position.Add(new Position(leftPost.position));
        // leftGoalPost.velocity.Add(new Velocity(Vector3.zero));
        // leftGoalPost.orientation.Add(new Orientation(leftPost));
        //
        // Goal rightGoalPost = (Goal)myRootSegment.objects.Find(obj => obj is Goal g && g.id == "goal_rightpost");
        // if (rightGoalPost == null)
        // {
        //     rightGoalPost = new Goal { id =  "goal_rightpost", type = "Goal" };
        //     myRootSegment.objects.Add(rightGoalPost);
        // }
        //
        // rightGoalPost.position.Add(new Position(rightPost.position));
        // rightGoalPost.velocity.Add(new Velocity(Vector3.zero));
        // rightGoalPost.orientation.Add(new Orientation(rightPost));
       
        
    }
   
    // Called when ElevenLabs transcription is done
    private void HandleTranscriptionComplete(Scribe.ElevenLabsResponse response)
    {
        Debug.Log("JSONToLLM: Received transcription. Merging placeholders...");

        tokenDictionary.Clear();
        if (response.words != null)
        {
            foreach (var w in response.words)
            {
                float timeKey = w.start;
                if (!tokenDictionary.ContainsKey(timeKey))
                    tokenDictionary[timeKey] = new List<object>();
                tokenDictionary[timeKey].Add(w.text);
            }
        }

        if (keyboard != null)
        {
            // Insert bracket placeholders
            foreach (var pair in keyboard.annotationTimes)
            {
                float annotationTime = pair.Value;
                int annotationIndex = pair.Key;

                if (!tokenDictionary.ContainsKey(annotationTime))
                    tokenDictionary[annotationTime] = new List<object>();

                tokenDictionary[annotationTime].Add($"[{annotationIndex}]");
            }
        }

        isTranscriptionComplete = true;

        Debug.Log("Merged words + placeholders. Token dictionary:");
        foreach (var kvp in tokenDictionary.OrderBy(k => k.Key))
        {
            Debug.Log($"Time={kvp.Key:F2} => {string.Join(", ", kvp.Value)}");
        }
    }

    public void WriteFile()
    {
        CreateJSONString();
    }

    public void CreateJSONString()
    {
        recordingNum++;
        JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        
        var sortedTokens = tokenDictionary
            .OrderBy(kvp => kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        keyboard.explanation = BuildSentenceFromTokens();
    
        object finalScene;
      
            finalScene = new
            {
                scene = new
                {
                    id = jsonDirectory.drillID,
                    language = keyboard.explanation,
                    step = 0.02,
                    objects = myRootSegment.objects,
                    annotations = keyboard.GetAnnotationsAsJson(),
                    tokens = sortedTokens,
                    clickTimes = keyboard.annotationTimes
                }
            };

        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        jsonString = JsonConvert.SerializeObject(finalScene, settings);
        filename = jsonDirectory.InstantiateJSONSegmentFilePath(recordingNum) + ".json";
        File.WriteAllText(filename, jsonString);

        Debug.Log($"Segment written to {filename}");
    }

    
    private string BuildSentenceFromTokens()
    {
        // Sort the tokenDictionary by timestamp (key) ascending
        var sortedTokens = tokenDictionary.OrderBy(kvp => kvp.Key);
    
        StringBuilder sb = new StringBuilder();

        // Loop through each sorted token list and append each token
        foreach (var kvp in sortedTokens)
        {
            foreach (var tokenObj in kvp.Value)
            {
                sb.Append(tokenObj.ToString());
            }
        }
    
        // Optionally, trim any extra whitespace
        return sb.ToString().Trim();
    }


    public void ResetSegmentData()
    {
        myRootSegment = new RootSegment();
        time = 0;
        tokenDictionary.Clear();
        keyboard.explanation = "";
        Debug.Log("Segment data has been reset.");
    }

    void FixedUpdate()
    {
      
        if (activateSystemRecording && keyboard.segmentStarted) // if system recording and segment started, we want to start logging
        {
            isLogging = true;
            Debug.Log("started logging");
        }
        

        if (isLogging)
        {
            // Debug.Log("JSON LOGGING");
            time += 0.02f;
            PopulateSegment();
            if (!recorderManager.RecorderController.IsRecording())
            {
                recorderManager.StartRecording();
                videoIsRecording = recorderManager.RecorderController.IsRecording();
            }
        }
        else if (!isLogging)
        {
            if (recorderManager.RecorderController.IsRecording())
            {
                recorderManager.StopRecording();
                videoIsRecording = recorderManager.RecorderController.IsRecording();
            }
        }
    }

}
