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
    public StreamingSampleMic streamingSampleMic;
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
    private RecorderManager recorderManager;
    public bool loggingStartedByUnpause = false;

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
        public string type = "Corner";
        public List<Position> position = new List<Position>();
        public List<Velocity> velocity = new List<Velocity>();
        public List<Orientation> orientation = new List<Orientation>();
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
        streamingSampleMic = GameObject.FindGameObjectWithTag("stream").GetComponent<StreamingSampleMic>();
        recorderManager = GameObject.FindGameObjectWithTag("RecorderManager").GetComponent<RecorderManager>();
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
                    cornerObject = new Corner { id = "corner" + i };
                    myRootSegment.objects.Add(cornerObject);
                }

                cornerObject.position.Add(new Position(corner.transform.position));
                cornerObject.velocity.Add(new Velocity(Vector3.zero));
                cornerObject.orientation.Add(new Orientation(corner.transform));
            }
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
                    behavior = "expert"
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
        
        Transform leftPost = goal.transform.Find("goalpost_left");
        Transform rightPost = goal.transform.Find("goalpost_right");

        Goal leftGoalPost = (Goal)myRootSegment.objects.Find(obj => obj is Goal g && g.id == "goalpost_left");
        if (leftGoalPost == null)
        {
            leftGoalPost = new Goal { id = "goalpost_left", type = "Goalpost" };
            myRootSegment.objects.Add(leftGoalPost);
        }

        leftGoalPost.position.Add(new Position(leftPost.position));
        leftGoalPost.velocity.Add(new Velocity(Vector3.zero));
        leftGoalPost.orientation.Add(new Orientation(leftPost));

        Goal rightGoalPost = (Goal)myRootSegment.objects.Find(obj => obj is Goal g && g.id == "goalpost_right");
        if (rightGoalPost == null)
        {
            rightGoalPost = new Goal { id = "goalpost_right", type = "Goalpost" };
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
        RemoveSpecificSequences();
        isTranscriptionComplete = false;
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

            // List<string> unwantedTokens = new List<string>
            //     { "BL", "ANK", "AUD", "IO", "_", "@", ".com" };
            //
            // if (unwantedTokens.Any(unwanted => text.Contains(unwanted)))
            // {
            //     continue;
            // }

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

    public void RemoveSpecificSequences()
    {
        
        //  sequences to be removed from token dictionary
        List<List<string>> sequencesToRemove = new List<List<string>>
        {
            new List<string> { "[", "BL", "ANK", "_", "AUD", "IO", "]" },
            new List<string> { "Thank", "you", "." },
            new List<string> { "Thank", "you", "for", "watching", "." }
        };

        foreach (var sequence in sequencesToRemove)
        {
            var keysToRemove = new List<int>();
            foreach (var entry in tokenDictionary)
            {
                int startIndex = entry.Key;
                bool isMatch = true;

                for (int i = 0; i < sequence.Count; i++)
                {
                    int currentIndex = startIndex + i;

                    if (!tokenDictionary.ContainsKey(currentIndex) ||
                        tokenDictionary[currentIndex][0].ToString() != sequence[i])
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    for (int i = 0; i < sequence.Count; i++)
                    {
                        keysToRemove.Add(startIndex + i);
                    }

                    break; 
                }
            }

            // Remove the keys associated with the sequence
            foreach (int key in keysToRemove)
            {
                tokenDictionary.Remove(key);
            }
        }

        Debug.Log("Specified sequences removed from token dictionary.");
    }


// Reset the data for the current segment, clearing stored tokens and annotations
    public void ResetSegmentData()
    {
        myRootSegment = new RootSegment();
        isTranscriptionComplete = false;
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
                id = jsonDirectory.drillID,
                language = keyboard.explanation,
                step = 0.02,
                objects = myRootSegment.objects,
                annotations = keyboard.GetAnnotationsAsJson(),
                tokens = tokenDictionary,
                clickTimes = keyboard.annotationTimes
            }
        }, settings);

        // DirectoryInfo jsonOutputFolder = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "SampleJsons"));
        filename = jsonDirectory.InstantiateJSONSegmentFilePath(recordingNum) + ".json";
        
        File.WriteAllText(filename, jsonString);
        isAdjusted = false;
        Debug.Log($"Segment written to {filename}");
        
    }

    
    // adjust token times based on whether or not the first token was NOT said within the 0-1 seconds.
    public void AdjustTokenTimes()
    {
        if (tokenDictionary.Count == 0)
        {
            Debug.LogWarning("Token dictionary is empty. No adjustments made.");
            return;
        }
        
        int firstKey = tokenDictionary.Keys.First();
        float firstTimestamp = (float)tokenDictionary[firstKey][1];
        
        if (firstTimestamp < 0 || firstTimestamp > 1)
        {
            float targetTimestamp = 0.1f;
            
            float adjustment = targetTimestamp - firstTimestamp;
            
            Dictionary<int, List<object>> adjustedTokenDictionary = new Dictionary<int, List<object>>();

            foreach (var entry in tokenDictionary)
            {
                int key = entry.Key;
                List<object> value = entry.Value;
                float adjustedTime = (float)value[1] + adjustment;
                adjustedTokenDictionary[key] = new List<object> { value[0], adjustedTime };
            }

            tokenDictionary = adjustedTokenDictionary;
            Debug.Log($"Token times adjusted to align the first token between 0-1 second with adjustment of {adjustment:F2} seconds.");
        }
        else
        {
            Debug.Log("First token already within the 0-1 second range. No adjustment needed.");
        }

        isAdjusted = true;
    }
    

    // Write the JSON data to a file
    public void WriteFile()
    {
        AdjustTokenTimes();
        if (isAdjusted)
        {
            CreateJSONString();
        }
    }

    // Update the scene data on a fixed time interval
    private void FixedUpdate()
    {
        if (streamingSampleMic.isSpeechDetected)
        {
            voiceActivated = true;
            keyboard.activationConditionMet = true; 
            Debug.Log("voice check activated");
        }

        // check both flags before starting logging (voice activity or unpause)
        if (keyboard.segmentStarted && keyboard.activationConditionMet && !isLogging)
        {
            isLogging = true;
            Debug.Log("Started logging");
        }

        if (isLogging)
        {
            Debug.Log("JSON LOGGING");
            time += 0.02f;
            PopulateSegment();

            if (!recorderManager.RecorderController.IsRecording())
            {
                recorderManager.StartRecording();
                videoIsRecording = recorderManager.RecorderController.IsRecording();
            }
        }
        else if (!isLogging && !voiceActivated)
        {
            if (recorderManager.RecorderController.IsRecording())
            {
                recorderManager.StopRecording();
                videoIsRecording = recorderManager.RecorderController.IsRecording();
            }
        }

        var color = streamingSampleMic.isSpeechDetected ? Color.green : Color.red;
        streamingSampleMic.microphoneRecord.vadIndicatorImage.color = color;
    }
}