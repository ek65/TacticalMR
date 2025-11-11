using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Event arguments for scenic object addition events
/// </summary>
public class ScenicObectAddEventArg : EventArgs 
{ 
    public GameObject gameObject { get; set; } 
}

/// <summary>
/// Factory class responsible for instantiating and configuring game objects based on Scenic simulation data.
/// Handles the creation of players, balls, goals, and other objects in the networked multiplayer environment.
/// Publishes events when objects are created for timeline management and other systems to respond to.
/// </summary>
public class InstantiateScenicObject
{
    #region Events
    /// <summary>
    /// Delegate for scenic object addition events
    /// </summary>
    public delegate void PublishScenicAddObjectEvent(ScenicObectAddEventArg arg);
    
    /// <summary>
    /// Event published when a new scenic object is added to the scene.
    /// Timeline manager and other systems subscribe to this event.
    /// </summary>
    public static event PublishScenicAddObjectEvent Publish;
    #endregion

    #region Private Fields
    /// <summary>
    /// Reference to the central object list manager
    /// </summary>
    ObjectsList objectList;
    #endregion

    #region Constructor
    /// <summary>
    /// Creates and instantiates a new scenic object in the networked environment
    /// </summary>
    /// <param name="pos">World position for the new object</param>
    /// <param name="rot">World rotation for the new object</param>
    /// <param name="modelType">Type of object to create (Player, Ball, Goal, etc.)</param>
    /// <param name="color">Color/team information for the object</param>
    /// <param name="name">Name to assign to the object</param>
    public InstantiateScenicObject(Vector3 pos, Quaternion rot, string modelType, Color color, string name)
    {
        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        Debug.Log(modelType);
        AddScenicObject(pos, rot, modelType, color, name);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Creates and configures a scenic object based on the specified parameters.
    /// Handles different object types including players, balls, goals, and human players.
    /// Sets up networking, team assignments, and registers objects with the global object list.
    /// </summary>
    /// <param name="pos">World position</param>
    /// <param name="rot">World rotation</param>
    /// <param name="modelType">Object type identifier</param>
    /// <param name="color">Color for team assignment</param>
    /// <param name="name">Object name</param>
    private void AddScenicObject(Vector3 pos, Quaternion rot, string modelType, Color color, string name)
    {
        GameObject addedGameObject = null;
        NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
        
        if (modelType == "Ball")
        {
            NetworkObject temp = runner.Spawn(objectList.modelList["soccer_ball"], pos, Quaternion.identity);
            addedGameObject = temp.gameObject;
            
            BallInterface bI = addedGameObject.GetComponent<BallInterface>();
            bI.RPC_InstantiateValues();
            bI.SetObjectName("Ball");
        }
        else if (modelType == "goal")
        {
            NetworkObject temp = runner.Spawn(objectList.modelList["goal"], pos, rot);
            addedGameObject = temp.gameObject;
            
            GoalInterface gI = addedGameObject.GetComponent<GoalInterface>();
            gI.RPC_InstantiateValues();
            gI.SetObjectName("Goal");
        }
        else if (modelType == "line")
        {
            NetworkObject temp = runner.Spawn(objectList.modelList["line"], pos, rot);
            addedGameObject = temp.gameObject;
            
            LineInterface lI = addedGameObject.GetComponent<LineInterface>();
            lI.RPC_InstantiateValues();
            lI.SetObjectName("Goal");
        }
        else if (modelType == "Player" || modelType == "Robot")
        {
            if (modelType == "Player")
            {
                NetworkObject temp = runner.Spawn(objectList.modelList["player.scenic"], pos, rot);
                addedGameObject = temp.gameObject;
                
                PlayerInterface pI = addedGameObject.GetComponent<PlayerInterface>();
                
                // Assign team based on color
                if (color == new Color(0f, 0f, 255f, 1f)) // Blue = Defense team
                {
                    pI.RPC_InstantiateValues(isAlly: true);
                }
                else if (color == new Color(255f, 0f, 0f, 1f)) // Red = Offense team
                {
                    pI.RPC_InstantiateValues(isAlly: false);
                }
                
                pI.SetObjectName(name);
                addedGameObject.name = name;
            } 
            else if (modelType == "Robot")
            {
                NetworkObject temp = runner.Spawn(objectList.modelList["player.robot"], pos, rot);
                addedGameObject = temp.gameObject;
                
                PlayerInterface pI = addedGameObject.GetComponent<PlayerInterface>();
                pI.SetObjectName(name);
            }

            Debug.Log("Added Scenic Player");
        }
        else if (modelType == "Human" || modelType == "Coach" || modelType == "RobotCoach")
        {
            // Handle human player creation/repositioning
            if (objectList.humanPlayers.Count == 0)
            {
                // Create new human player
                if (modelType == "Human")
                {
                    // Choose VR or standard human based on game manager settings
                    if (GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().laptopMode)
                    {
                        NetworkObject temp = runner.Spawn(objectList.modelList["player.human"], pos, rot);
                        addedGameObject = temp.gameObject;
                    }
                    else
                    {
                        NetworkObject temp = runner.Spawn(objectList.modelList["player.human VR"], pos, rot);
                        addedGameObject = temp.gameObject;
                    }
                } 
                else if (modelType == "Coach")
                {
                    NetworkObject temp = runner.Spawn(objectList.modelList["player.coach"], pos, rot);
                    addedGameObject = temp.gameObject;
                }
                else if (modelType == "RobotCoach")
                {
                    NetworkObject temp = runner.Spawn(objectList.modelList["player.robotcoach"], pos, rot);
                    addedGameObject = temp.gameObject;
                }
                
                // Configure human interface
                HumanInterface hI = addedGameObject.GetComponent<HumanInterface>();
                hI.RPC_InstantiateValues(isAlly: true);
                hI.SetObjectName("Coach");
                addedGameObject.name = "Coach";
                
                Debug.Log("Added Human Player");
            }
            else
            {
                // Reposition existing human player
                if (GameObject.FindGameObjectWithTag("human") != null)
                {
                    addedGameObject = objectList.humanPlayers[0];
                    HumanInterface hI = addedGameObject.GetComponent<HumanInterface>();
                    
                    // Set name (hardcoded as "Coach" due to name parameter being empty)
                    addedGameObject.name = "Coach";
                    hI.SetObjectName("Coach");
                    
                    // Handle repositioning with fade effect
                    Fade f = objectList.humanPlayers[0].GetComponent<Fade>();
                    ExitScenario e = objectList.humanPlayers[0].GetComponent<ExitScenario>();
                    e.endScenario = false;
                    f.StartFadeAndMove(pos, rot);
                }
            }
        }
        else // misc objects
        {
            NetworkObject temp = runner.Spawn(objectList.modelList[modelType], pos, rot);
            addedGameObject = temp.gameObject;
            addedGameObject.name = name;
            objectList.scenicObjects.Add(addedGameObject);
        }
        // Publish object creation event for other systems to respond
        if (Publish != null)
        {
            Publish(new ScenicObectAddEventArg() { gameObject = addedGameObject });
        }
    }
    #endregion
}