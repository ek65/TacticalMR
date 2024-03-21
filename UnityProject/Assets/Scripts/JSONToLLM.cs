using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class JSONToLLM : MonoBehaviour
{
    public ObjectsList objectsList;
    private string filename = "";

    [System.Serializable]
    public class OffensePlayer
    {
        public string name;
        public float positionX;
        public float positionZ;
    }
    
    [System.Serializable]
    public class DefensePlayer
    {
        public string name;
        public float positionX;
        public float positionZ;
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
    }
    
    [System.Serializable]
    public class Coach
    {
        public string name;
        public float positionX;
        public float positionZ;
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
    
    // Start is called before the first frame update
    void Start()
    {
        filename = Application.dataPath + "/test.json";
    }

    // Update is called once per frame
    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     WriteCSV();
        // }
    }

    // TODO: change scenic so that there are "offense players" and "defense players".
    public void PopulateSceneObjects()
    {
        SceneObjects mySceneObjects = new SceneObjects();
        
        foreach (GameObject offense in objectsList.scenicPlayers)
        {
            OffensePlayer offensePlayer = new OffensePlayer();
            offensePlayer.name = offense.name;
            offensePlayer.positionX = offense.transform.position.x;
            offensePlayer.positionZ = offense.transform.position.z;
            mySceneObjects.offsensePlayers.Add(offensePlayer);
        }
        
        // foreach (GameObject defender in objectsList.humanPlayers)
        // {
        //     DefensePlayer defensePlayer = new DefensePlayer();
        //     defensePlayer.name = defender.name;
        //     defensePlayer.positionX = defender.transform.position.x;
        //     defensePlayer.positionZ = defender.transform.position.z;
        //     mySceneObjectsList.defensePlayers.Add(defensePlayer);
        // }

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
        mySceneObjects.goal = goalObject;
        
        GameObject coach = objectsList.humanPlayers[0];
        Coach coachObject = new Coach();
        coachObject.name = coach.name;
        coachObject.positionX = coach.transform.position.x;
        coachObject.positionZ = coach.transform.position.z;
        mySceneObjects.coach = coachObject;
    }

    // public void WriteCSV()
    // {
    //     if (mySceneObjectsList.sceneObjects.Count > 0)
    //     {
    //         TextWriter tw = new StreamWriter(filename, false);
    //         tw.WriteLine("Name,PositionX,PositionZ");
    //         foreach (SceneObjects sceneObject in mySceneObjectsList.sceneObjects)
    //         {
    //             tw.WriteLine(sceneObject.name + "," + sceneObject.positionX + "," + sceneObject.positionZ);
    //         }
    //         tw.Close();
    //         
    //         Debug.Log("CSV file written to " + filename);
    //     }
    // }

    public void WriteJSON()
    {
        string json = JsonUtility.ToJson(mySceneObjects, true);
        File.WriteAllText(filename, json);
        
        Debug.Log("JSON file written to " + filename);
    }
}
