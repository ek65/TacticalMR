using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq; // For .OrderBy(...)
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Whisper.Samples;

public class JSONToLLM : MonoBehaviour
{
    public KeyboardInput keyboard;
    public ObjectsList objectsList;
    private string filename;
    public string jsonString;
    public TimelineManager timelineManager;
    private RecorderManager recorderManager;

    public float time;  // Current timeline time for logging
    public bool isTranscriptionComplete = false;
    public bool isLogging = false;

    public int recordingNum = -1; 
    public bool voiceActivated = false;
    public bool videoIsRecording;
    public bool loggingStartedByUnpause = false;

    [Tooltip("Used to record system jsons/videos")]
    public bool activateSystemRecording = false;

    // ---------------- TRAJECTORY DATA CLASSES ----------------
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
        public List<bool> ballPossession = new List<bool>();
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
    public class RootSegment
    {
        public int timestep;
        public List<object> objects = new List<object>();
    }
    
    public RootSegment myRootSegment = new RootSegment();
    private List<RootSegment> segmentsList = new List<RootSegment>();
    
    public Dictionary<float, List<object>> tokenDictionary = new Dictionary<float, List<object>>();
    
    private Scribe scribe;

    void Start()
    {
        // Find references
        keyboard = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
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
        // If needed, do real-time logic here
    }

    // This is called from FixedUpdate if isLogging=true, to capture each frame’s data
    public void PopulateSegment()
    {
        PopulateSceneObjects();
        myRootSegment.timestep = timelineManager.TimeIndex;
    }
    
    public void PopulateSceneObjects()
    {
        if (objectsList == null || objectsList.ballObject == null) return;

        // 1) corners
        for (int i = 1; i <= 4; i++)
        {
            GameObject cornerGO = GameObject.Find("corner" + i);
            if (cornerGO != null)
            {
                Corner cornerData = (Corner)myRootSegment.objects
                    .Find(obj => obj is Corner c && c.id == "corner" + i);

                if (cornerData == null)
                {
                    cornerData = new Corner { id = "corner" + i };
                    myRootSegment.objects.Add(cornerData);
                }
                cornerData.position.Add(new Position(cornerGO.transform.position));
                cornerData.velocity.Add(new Velocity(Vector3.zero));
                cornerData.orientation.Add(new Orientation(cornerGO.transform));
            }
        }

        // 2) defense players
        foreach (GameObject currPlayer in objectsList.defensePlayers)
        {
            AddOrUpdatePlayer(currPlayer, "Teammate");
        }

        // 3) offense players
        foreach (GameObject currPlayer in objectsList.offensePlayers)
        {
            AddOrUpdatePlayer(currPlayer, "Opponent");
        }

        // 4) coach/human players
        foreach (GameObject humanPlayer in objectsList.humanPlayers)
        {
            AddOrUpdatePlayer(humanPlayer, "Coach", "expert");
        }

        // 5) ball
        GameObject ballGO = objectsList.ballObject;
        Ball ballData = (Ball)myRootSegment.objects.Find(obj => obj is Ball);
        if (ballData == null)
        {
            ballData = new Ball { id = "ball" };
            myRootSegment.objects.Add(ballData);
        }
        ballData.position.Add(new Position(ballGO.transform.position));
        ballData.orientation.Add(new Orientation(ballGO.transform));
        Rigidbody ballRB = ballGO.GetComponent<Rigidbody>();
        ballData.velocity.Add(new Velocity(ballRB.velocity));

        // 6) goal
        GameObject goalGO = objectsList.goalObject;
        if (goalGO != null)
        {
            Goal goalData = (Goal)myRootSegment.objects
                .Find(obj => obj is Goal g && g.id == "goal");
            if (goalData == null)
            {
                goalData = new Goal { id = "goal" };
                myRootSegment.objects.Add(goalData);
            }
            goalData.position.Add(new Position(goalGO.transform.position));
            goalData.velocity.Add(new Velocity(Vector3.zero));
            goalData.orientation.Add(new Orientation(goalGO.transform));

            // left post
            Transform leftPost = goalGO.transform.Find("goal_leftpost");
            if (leftPost != null)
            {
                Goal leftGoalPost = (Goal)myRootSegment.objects
                    .Find(obj => obj is Goal g && g.id == "goal_leftpost");
                if (leftGoalPost == null)
                {
                    leftGoalPost = new Goal { id = "goal_leftpost", type = "Goal" };
                    myRootSegment.objects.Add(leftGoalPost);
                }
                leftGoalPost.position.Add(new Position(leftPost.position));
                leftGoalPost.velocity.Add(new Velocity(Vector3.zero));
                leftGoalPost.orientation.Add(new Orientation(leftPost));
            }

            // right post
            Transform rightPost = goalGO.transform.Find("goal_rightpost");
            if (rightPost != null)
            {
                Goal rightGoalPost = (Goal)myRootSegment.objects
                    .Find(obj => obj is Goal g && g.id == "goal_rightpost");
                if (rightGoalPost == null)
                {
                    rightGoalPost = new Goal { id = "goal_rightpost", type = "Goal" };
                    myRootSegment.objects.Add(rightGoalPost);
                }
                rightGoalPost.position.Add(new Position(rightPost.position));
                rightGoalPost.velocity.Add(new Velocity(Vector3.zero));
                rightGoalPost.orientation.Add(new Orientation(rightPost));
            }
        }
    }

    private void AddOrUpdatePlayer(GameObject playerGO, string type, string behaviorOverride = null)
    {
        Player existingPlayer = (Player)myRootSegment.objects
            .Find(obj => obj is Player p && p.id == playerGO.name);

        if (existingPlayer == null)
        {
            existingPlayer = new Player
            {
                id = playerGO.name,
                behavior = (behaviorOverride != null) ? behaviorOverride : playerGO.GetComponent<PlayerInterface>().behavior,
                type = type
            };
            myRootSegment.objects.Add(existingPlayer);
        }
        
        existingPlayer.position.Add(new Position(playerGO.transform.position));
        Vector3 velocity = (type == "Coach")
            ? keyboard.movement
            : playerGO.GetComponent<PlayerInterface>().currVelocity;

        existingPlayer.velocity.Add(new Velocity(velocity));
        existingPlayer.orientation.Add(new Orientation(playerGO.transform));

        // ballPossession
        bool hasBall = false;
        if (type == "Coach")
        {
            hasBall = playerGO.GetComponent<HumanInterface>().ballPossession;
        }
        else
        {
            hasBall = playerGO.GetComponent<PlayerInterface>().ballPossession;
        }
        existingPlayer.ballPossession.Add(hasBall);
    }
    
    private void HandleTranscriptionComplete(Scribe.ElevenLabsResponse response)
    {
        Debug.Log("JSONToLLM: Received transcription from Scribe. Merging placeholders...");

        tokenDictionary.Clear();
        if (response.words != null)
        {
            foreach (var w in response.words)
            {
                float timeKey = w.start;
                if (!tokenDictionary.ContainsKey(timeKey))
                {
                    tokenDictionary[timeKey] = new List<object>();
                }
                tokenDictionary[timeKey].Add(w.text);
            }
        }
        
        if (keyboard != null)
        {
            foreach (var pair in keyboard.annotationTimes)
            {
                float annotationTime = pair.Value;
                int annotationIndex = pair.Key;

                if (!tokenDictionary.ContainsKey(annotationTime))
                {
                    tokenDictionary[annotationTime] = new List<object>();
                }

                tokenDictionary[annotationTime].Add($"[{annotationIndex}]");
            }
        }

        isTranscriptionComplete = true;

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

        // Sort tokens if needed
        var sortedTokens = tokenDictionary
            .OrderBy(kvp => kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // We'll store our anonymous JSON structure in a plain `object`.
        object finalScene;

        if (activateSystemRecording)
        {
            finalScene = new
            {
                scene = new
                {
                    id = jsonDirectory.drillID,
                    step = 0.02,
                    objects = myRootSegment.objects
                }
            };
        }
        else
        {
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
        }

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

    public void ResetSegmentData()
    {
        myRootSegment = new RootSegment();
        time = 0;
        tokenDictionary.Clear();
        keyboard.explanation = "";
        Debug.Log("Segment data has been reset.");
    }

    // For debugging tokens in console
    public void ProcessTokens()
    {
        foreach (var kvp in tokenDictionary.OrderBy(k => k.Key))
        {
            Debug.Log($"Time {kvp.Key:F2} => {string.Join(", ", kvp.Value)}");
        }
        isTranscriptionComplete = true;
    }

    private void FixedUpdate()
    {
   
        if (isLogging)
        {
            time += 0.02f;
            PopulateSegment();
        }
    }
}
