using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using OpenAI.Samples.Chat;
using OVRSimpleJSON;
using Newtonsoft.Json.Linq;

public class JSONToLLM : MonoBehaviour
{
    public ObjectsList objectsList;
    private string filename = "";
    public ChatBehaviour chatBehaviour;
    private string jsonString;
    public int segments;

    [System.Serializable]
    public class OffensePlayer
    {
        public string name;
        public float positionX;
        public float positionZ;
        public Vector3 rotation;
        public string behavior;
    }
    
    [System.Serializable]
    public class DefensePlayer
    {
        public string name;
        public float positionX;
        public float positionZ;
        public Vector3 rotation;
        public string behavior;
    }
    
    [System.Serializable]
    public class Ball
    {
        public string name;
        public float positionX;
        public float positionZ;
    }
    
    [System.Serializable]
    public class Goal
    {
        public string name;
        public float positionX;
        public float positionZ;
        public Vector3 rotation;
    }
    
    [System.Serializable]
    public class Coach
    {
        public string name;
        public float positionX;
        public float positionZ;
        public Vector3 rotation;
        public string explanation;
    }
    
    [System.Serializable]
    public class SceneObjects
    {
        public List<OffensePlayer> offsensePlayers;
        public List<DefensePlayer> defensePlayers;
        public Ball ball;
        public Goal goal;
        public Coach coach;
    }
    
    public SceneObjects mySceneObjects = new SceneObjects();
    
    void Start()
    {
        filename = Application.dataPath + "/output.txt";
    }

    void Update()
    {
    }

    public void PopulateSceneObjects()
    {
        mySceneObjects.offsensePlayers = new List<OffensePlayer>();
        mySceneObjects.defensePlayers = new List<DefensePlayer>();
        
        foreach (GameObject offense in objectsList.offensePlayers)
        {
            OffensePlayer offensePlayer = new OffensePlayer();
            offensePlayer.name = offense.name;
            offensePlayer.positionX = offense.transform.position.x;
            offensePlayer.positionZ = offense.transform.position.z;
            offensePlayer.rotation = offense.transform.rotation.eulerAngles;
            offensePlayer.behavior = offense.GetComponent<PlayerInterface>().behavior;
            mySceneObjects.offsensePlayers.Add(offensePlayer);
        }
        
        foreach (GameObject defender in objectsList.defensePlayers)
        {
            DefensePlayer defensePlayer = new DefensePlayer();
            defensePlayer.name = defender.name;
            defensePlayer.positionX = defender.transform.position.x;
            defensePlayer.positionZ = defender.transform.position.z;
            defensePlayer.rotation = defender.transform.rotation.eulerAngles;
            defensePlayer.behavior = defender.GetComponent<PlayerInterface>().behavior;
            mySceneObjects.defensePlayers.Add(defensePlayer);
        }

        GameObject ball = objectsList.ballObject;
        Ball ballObject = new Ball();
        ballObject.name = ball.name;
        ballObject.positionX = ball.transform.position.x;
        ballObject.positionZ = ball.transform.position.z;
        mySceneObjects.ball = ballObject;
        
        GameObject goal = objectsList.goalObject;
        Goal goalObject = new Goal();
        goalObject.name = goal.name;
        goalObject.positionX = goal.transform.position.x;
        goalObject.positionZ = goal.transform.position.z;
        goalObject.rotation = goal.transform.rotation.eulerAngles;
        mySceneObjects.goal = goalObject;
        
        GameObject coach = objectsList.humanPlayers[0];
        Coach coachObject = new Coach();
        coachObject.name = coach.name;
        coachObject.positionX = coach.transform.position.x;
        coachObject.positionZ = coach.transform.position.z;
        coachObject.rotation = coach.transform.rotation.eulerAngles;
        coachObject.explanation = coach.GetComponent<HumanInterface>().explanation;
        mySceneObjects.coach = coachObject;
        
        // Adding coach to defense players list as well
        DefensePlayer coachDefense = new DefensePlayer();
        coachDefense.name = coach.name;
        coachDefense.positionX = coach.transform.position.x;
        coachDefense.positionZ = coach.transform.position.z;
        coachDefense.rotation = coach.transform.rotation.eulerAngles;
        mySceneObjects.defensePlayers.Add(coachDefense);
    }

    public void CreateJSONString()
    {
        jsonString = JsonUtility.ToJson(mySceneObjects, true);
        chatBehaviour.jsonText = jsonString;
    }
    
    public void WriteFile()
    {
        JObject parsedJson = JObject.Parse(chatBehaviour.jsonResponseText);
        string formattedJson = parsedJson.ToString(Newtonsoft.Json.Formatting.Indented);
        int newSeg = segments + 1;
        segments = newSeg;
        string outputFilePath = Application.dataPath + "/output.txt";
        string segmentOutput = $@"
        Segment # {segments}
        Coach Explanation: {chatBehaviour.userInput}
        JSON:
        {formattedJson}
        Condition:
        {chatBehaviour.conditionOutput}
        Action:
        {chatBehaviour.actionOutput}
        ";
        File.AppendAllText(outputFilePath, segmentOutput);
        Debug.Log($"Segment written to {outputFilePath}");
    }
}
