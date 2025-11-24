using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using System.Linq;

/// <summary>
/// Parses JSON data received from the Scenic simulation system and converts it into Unity-compatible data structures.
/// Handles object creation, destruction, movement data, and action command parsing for the multiplayer soccer simulation.
/// Provides coordinate system conversion between Scenic's right-handed system and Unity's left-handed system.
/// </summary>
public class ScenicParser
{
    #region Public Methods
    /// <summary>
    /// Parses raw JSON string into ScenicJson data structure
    /// </summary>
    /// <param name="json">Raw JSON string from Scenic</param>
    /// <returns>Parsed ScenicJson object</returns>
    public ScenicJson ParseData(string json)
    {
        var jsonResult = JsonConvert.DeserializeObject(json).ToString();
        ScenicJson t = JsonConvert.DeserializeObject<ScenicJson>(jsonResult);
        return t;
    }

    /// <summary>
    /// Converts ScenicJson data into movement data for Unity objects.
    /// Handles control commands and extracts movement/action data for each object.
    /// </summary>
    /// <param name="t">Parsed ScenicJson data</param>
    /// <returns>List of movement data for each object</returns>
    public List<ScenicMovementData> ScenicMovementParser(ScenicJson t)
    {
        HandleControl(t);
        List<ScenicMovementData> objData = new List<ScenicMovementData>();
        foreach (Object p in t.Objects)
        {
            objData.Add(HandleMovementData(p));
        }
        return objData;
    }
    #endregion
    
    #region Private Methods
    /// <summary>
    /// Processes individual object movement data and converts to Unity format.
    /// Handles action command parsing using reflection to match method signatures.
    /// </summary>
    /// <param name="data">Object data from Scenic</param>
    /// <returns>ScenicMovementData for Unity consumption</returns>
    private ScenicMovementData HandleMovementData(Object data)
    {
        Vector3 pos = ListToVector(data.Position);
        string modelType = data.Model.ModelType;
        bool pause = data.Pause;
        string behavior = data.Behavior;
        
        if (data.ActionDict.Count > 0)
        {
            string actionFunc = data.ActionDict.First().Key;
            ActionDictType actionValues = data.ActionDict.First().Value;

            Type classType = Type.GetType("ActionAPI");
            if (classType.GetMethod(actionFunc) == null)
            {
                Debug.LogError("Given action function does not exist.");
            }
            else
            {
                MethodBase method = classType.GetMethod(actionFunc);
                ParameterInfo[] parameters = method.GetParameters();

                List<object> actionArgs = ParseActionArguments(parameters, actionValues);
                return new ScenicMovementData(pos, modelType, behavior, actionFunc, actionArgs, pause);
            }
            return new ScenicMovementData(pos, modelType, behavior, pause);
        }

        return new ScenicMovementData(pos, modelType, behavior, pause);
    }

    /// <summary>
    /// Parses action arguments from Scenic data to match Unity method parameters using reflection
    /// </summary>
    /// <param name="parameters">Method parameters from reflection</param>
    /// <param name="actionValues">Values from Scenic action dictionary</param>
    /// <returns>List of parsed arguments ready for method invocation</returns>
    private List<object> ParseActionArguments(ParameterInfo[] parameters, ActionDictType actionValues)
    {
        List<object> actionArgs = new List<object>();
        int vector3Index = 0;
        int boolIndex = 0;
        int floatIndex = 0;
        int intIndex = 0;
        int stringIndex = 0;

        foreach (ParameterInfo param in parameters)
        {
            if (param.ParameterType == typeof(Vector3))
            {
                if (actionValues.TupleVals.Count - vector3Index > 0 &&
                    vector3Index < parameters.Count(p => p.ParameterType == typeof(Vector3)))
                {
                    Vector3 val = ListToVector(actionValues.TupleVals[vector3Index]);
                    actionArgs.Add(val);
                }
                else
                {
                    actionArgs.Add(null);
                }
                vector3Index++;
            }
            else if (param.ParameterType == typeof(bool))
            {
                if (actionValues.BoolVals.Count - boolIndex > 0 &&
                    boolIndex < parameters.Count(p => p.ParameterType == typeof(bool)))
                {
                    bool val = actionValues.BoolVals[boolIndex];
                    actionArgs.Add(val);
                }
                else
                {
                    actionArgs.Add(null);
                }
                boolIndex++;
            }
            else if (param.ParameterType == typeof(string))
            {
                if (actionValues.StringVals.Count - stringIndex > 0 &&
                    stringIndex < parameters.Count(p => p.ParameterType == typeof(string)))
                {
                    string val = actionValues.StringVals[stringIndex];
                    actionArgs.Add(val);
                }
                else
                {
                    actionArgs.Add(null);
                }
                stringIndex++;
            }
            else if (param.ParameterType == typeof(float))
            {
                if (actionValues.FloatVals.Count - floatIndex > 0 &&
                    floatIndex < parameters.Count(p => p.ParameterType == typeof(float)))
                {
                    float val = actionValues.FloatVals[floatIndex];
                    actionArgs.Add(val);
                }
                else
                {
                    actionArgs.Add(null);
                }
                floatIndex++;
            }
        }
        return actionArgs;
    }

    /// <summary>
    /// Handles control commands from Scenic including object creation and destruction
    /// </summary>
    /// <param name="data">Scenic JSON data containing control information</param>
    public void HandleControl(ScenicJson data)
    {
        if (data.Control)
        {
            if (data.AddObject && data.SpawnObjectQueue.Count != 0)
            {
                // Reset scene for new simulation run (typically at timestep 0)
                GameObject manager = GameObject.FindGameObjectWithTag("ScenicManager");
                ObjectsList objectsList = manager.GetComponent<ObjectsList>();
                objectsList.Reset();
                
                // Create all objects in spawn queue
                foreach (Object p in data.SpawnObjectQueue)
                {
                    Vector3 pos = ListToVector(p.Position);
                    Quaternion rot = ListToQuaternion(p.Rotation);
                    
                    // Convert from Scenic's right-handed to Unity's left-handed coordinate system
                    rot.y = -rot.y;
                    rot.x = -rot.x;
                    rot.z = -rot.z;
                    
                    string modelType = p.Model.ModelType;
                    Color color = ListToColor(p.Model.color);
                    string name = p.Name;
                    InstantiateScenicObject instObj = new InstantiateScenicObject(pos, rot, modelType, color, name);
                }
            }
            else if (data.Destroy)
            {
                // Clean up all objects and reset simulation state
                GameObject manager = GameObject.FindGameObjectWithTag("ScenicManager");
                ObjectsList objectsList = manager.GetComponent<ObjectsList>();

                if (objectsList != null)
                {
                    objectsList.Reset();
                    ZMQServer server = manager.GetComponent<ZMQServer>();
                    server.ResetTickServerRpc();
                }
            }
        }
    }
    #endregion

    #region Coordinate System Conversion
    /// <summary>
    /// Converts Scenic coordinate list to Unity Vector3.
    /// Scenic uses (x,y,z) where z is vertical, Unity uses (x,z,y) where y is vertical.
    /// </summary>
    /// <param name="v">List of 3 floats from Scenic [x,y,z]</param>
    /// <returns>Unity Vector3 with swapped y and z coordinates</returns>
    private Vector3 ListToVector(List<float> v)
    {
        return new Vector3(v[0], v[2], v[1]); // Swap y and z
    }

    /// <summary>
    /// Converts Scenic quaternion list to Unity Quaternion.
    /// Applies coordinate system transformation for rotations.
    /// </summary>
    /// <param name="q">List of 4 floats from Scenic [x,y,z,w]</param>
    /// <returns>Unity Quaternion with coordinate system conversion</returns>
    private Quaternion ListToQuaternion(List<float> q)
    {
        return new Quaternion(q[0], q[2], q[1], q[3]); // Swap y and z
    }

    /// <summary>
    /// Converts RGBA color values from Scenic to Unity Color
    /// </summary>
    /// <param name="v">List of 4 floats [r,g,b,a] in range 0-1</param>
    /// <returns>Unity Color object</returns>
    private Color ListToColor(List<float> v)
    {
        return new Color(v[0], v[1], v[2], v[3]);
    }
    #endregion

    #region JSON Data Structures
    /// <summary>
    /// Root JSON data structure from Scenic simulation
    /// </summary>
    public partial class ScenicJson
    {
        [JsonProperty("control")]
        public bool Control { get; set; }

        [JsonProperty("addObject")]
        public bool AddObject { get; set; }

        [JsonProperty("objects")]
        public List<Object> Objects { get; set; }

        [JsonProperty("spawnQueue")]
        public List<Object> SpawnObjectQueue { get; set; }
        
        [JsonProperty("timestepNumber")]
        public int TimestepNumber { get; set; }
        
        [JsonProperty("destroy")]
        public bool Destroy { get; set; }
    }

    /// <summary>
    /// Model definition for objects in the simulation
    /// </summary>
    public partial class Model
    {
        [JsonProperty("length")]
        public float Length { get; set; }
        
        [JsonProperty("width")]
        public float Width { get; set; }
        
        [JsonProperty("type")]
        public string ModelType { get; set; }
        
        [JsonProperty("color")]
        public List<float> color { get; set; }
    }

    /// <summary>
    /// HUD (Heads-Up Display) information for UI elements
    /// </summary>
    public partial class HUD
    {
        [JsonProperty("message")]
        public List<string> Message { get; set; }
        
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
        
        [JsonProperty("position")]
        public string Position { get; set; }
    }

    /// <summary>
    /// Complete object data from Scenic including transform, physics, and game state
    /// </summary>
    public partial class Object
    {
        [JsonProperty("model")]
        public Model Model { get; set; }

        [JsonProperty("hud")]
        public HUD HUD { get; set; }

        [JsonProperty("position")]
        public List<float> Position { get; set; }

        [JsonProperty("rotation")]
        public List<float> Rotation { get; set; }

        [JsonProperty("ballPossession")]
        public bool BallPossession { get; set; }
        
        [JsonProperty("handRaised")]
        public bool HandRaised { get; set; }
        
        [JsonProperty("isPackaged")]
        public bool IsPackaged { get; set; }
        
        [JsonProperty("isPossessed")]
        public bool IsPossessed { get; set; }
        
        [JsonProperty("isMoving")]
        public bool IsMoving { get; set; }
        
        [JsonProperty("xMark")]
        public List<float> XMark { get; set; }
        
        [JsonProperty("triggerPass")]
        public bool TriggerPass { get; set; }

        [JsonProperty("velocity")]
        public List<float> Velocity { get; set; }

        [JsonProperty("angularVelocity")]
        public List<float> AngularVelocity { get; set; }

        [JsonProperty("speed")]
        public float Speed { get; set; }

        [JsonProperty("velocityStop")]
        public bool VelocityStop { get; set; }

        [JsonProperty("actionDict")]
        public Dictionary<string, ActionDictType> ActionDict { get; set; }
        
        [JsonProperty("behavior")]
        public string Behavior { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("destroy")]
        public bool Destroy { get; set; }

        [JsonProperty("pause")]
        public bool Pause { get; set; }
        
        [JsonProperty("stopButton")]
        public bool Stopbutton { get; set; }
  
        [JsonProperty("heldByHuman")]
        public bool HeldByHuman { get; set; }
        
        [JsonProperty("heldByScenic")]
        public bool HeldByScenic { get; set; }
    }
    #endregion
}

/// <summary>
/// Action dictionary data structure for method arguments from Scenic
/// </summary>
public partial class ActionDictType
{
    [JsonProperty("intVals")]
    public List<int> IntVals { get; set; }
    
    [JsonProperty("floatVals")]
    public List<float> FloatVals { get; set; }
    
    [JsonProperty("stringVals")]
    public List<string> StringVals { get; set; }
    
    [JsonProperty("tupleVals")]
    public List<List<float>> TupleVals { get; set; }
    
    [JsonProperty("boolVals")]
    public List<bool> BoolVals { get; set; }
}