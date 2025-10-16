using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structure containing movement and action information for objects controlled by Scenic simulation.
/// Serves as the primary communication protocol between the Scenic AI system and Unity game objects.
/// </summary>
public class ScenicMovementData
{
    #region Core Properties
    /// <summary>
    /// Model information including type and visual properties
    /// </summary>
    public Model model;
    
    /// <summary>
    /// Target world position for the object
    /// </summary>
    public Vector3 position;

    /// <summary>
    /// Name of the action function to invoke via reflection
    /// </summary>
    public string actionFunc;
    
    /// <summary>
    /// Arguments to pass to the action function
    /// </summary>
    public List<object> actionArgs;
    
    /// <summary>
    /// Current behavior state description for UI display
    /// </summary>
    public string behavior;

    /// <summary>
    /// Whether the stop button has been pressed
    /// </summary>
    public bool stopButton;

    /// <summary>
    /// Whether the simulation should be paused
    /// </summary>
    public bool pause;
    #endregion

    #region Constructors
    /// <summary>
    /// Creates movement data with basic position and behavior information
    /// </summary>
    /// <param name="position">Target world position</param>
    /// <param name="modelType">Type of model/object</param>
    /// <param name="behavior">Behavior description</param>
    /// <param name="pause">Pause state</param>
    public ScenicMovementData(Vector3 position, string modelType, string behavior, bool pause)
    {
        this.position = position;
        this.model = new Model(modelType);
        this.behavior = behavior;
        this.pause = pause;
    }

    /// <summary>
    /// Creates movement data with action function and arguments
    /// </summary>
    /// <param name="position">Target world position</param>
    /// <param name="modelType">Type of model/object</param>
    /// <param name="behavior">Behavior description</param>
    /// <param name="actionFunc">Action function name to invoke</param>
    /// <param name="actionArgs">Arguments for the action function</param>
    /// <param name="pause">Pause state</param>
    public ScenicMovementData(Vector3 position, string modelType, string behavior, string actionFunc, List<object> actionArgs, bool pause)
    {
        this.position = position;
        this.model = new Model(modelType);
        this.behavior = behavior;
        this.actionFunc = actionFunc;
        this.actionArgs = actionArgs;
        this.pause = pause;
    }
    #endregion
}

/// <summary>
/// Model information container for Scenic objects.
/// Defines the type and visual properties of objects in the simulation.
/// </summary>
public class Model
{
    #region Properties
    /// <summary>
    /// String identifier for the model type (e.g., "Player", "Ball", "Goal")
    /// </summary>
    public string modelType;
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new model with the specified type
    /// </summary>
    /// <param name="modelType">Type identifier for the model</param>
    public Model(string modelType)
    {
        this.modelType = modelType;
    }
    #endregion
}