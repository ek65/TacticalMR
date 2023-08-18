using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using System.Linq;
using System.Reflection;

public class ScenicParser
{
    public ScenicJson ParseData(string json)
    {
        var jsonResult = JsonConvert.DeserializeObject(json).ToString();
        ScenicJson t = JsonConvert.DeserializeObject<ScenicJson>(jsonResult);
        return t;
    }
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
    private ScenicMovementData HandleMovementData(Object data)
    {
        Vector3 pos = ListToVector(data.Position);
        // Vector3 vel = ListToVector(data.Velocity);
        // Vector3 angVel = ListToVector(data.AngularVelocity);
        // float speed = data.Speed;
        // Quaternion rot = ListToQuaternion(data.Rotation);
        string modelType = data.Model.ModelType;
        if (data.ActionDict.Count > 0)
        {
            string actionFunc = data.ActionDict.First().Key;
            ActionDictType actionValues = data.ActionDict.First().Value;
            Debug.Log(actionFunc);
            
            Type classType = Type.GetType("ActionAPI");
            if (classType.GetMethod(actionFunc) == null)
            {
                Debug.LogError("Given action function does not exist.");
            }
            else
            {
                MethodBase method = classType.GetMethod(actionFunc);
                ParameterInfo[] parameters = method.GetParameters();
                
                List<object> actionArgs = new List<object>();
                int vector3Index = 0;
                int boolIndex = 0;
                int floatIndex = 0;
                int intIndex = 0;
                int stringIndex = 0;

                foreach (ParameterInfo param in parameters)
                {
                    Debug.Log("For parameter #" + param.Position 
                                                + ", the ParameterType is: " + param.ParameterType);
                    if (param.ParameterType == typeof(Vector3))
                    {
                        Vector3 val;
                        // do not want to error here and rather add a null item as some values may have defaults 
                        if (actionValues.TupleVals.Count - vector3Index > 0 &&
                            vector3Index < parameters.Count(p => p.ParameterType == typeof(Vector3)))
                        {
                            val = ListToVector(actionValues.TupleVals[vector3Index]);
                            actionArgs.Add(val);
                        }
                        else
                        {
                            actionArgs.Add(null);
                        }
                        vector3Index++;
                    } else if (param.ParameterType == typeof(bool))
                    {
                        bool val;
                        if (actionValues.BoolVals.Count - boolIndex > 0 &&
                            boolIndex < parameters.Count(p => p.ParameterType == typeof(bool)))
                        {
                            val = actionValues.BoolVals[boolIndex];
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
                        string val;
                        if (actionValues.StringVals.Count - stringIndex > 0 &&
                            stringIndex < parameters.Count(p => p.ParameterType == typeof(string)))
                        {
                            val = actionValues.StringVals[stringIndex];
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
                        float val;
                        if (actionValues.FloatVals.Count - floatIndex > 0 &&
                            floatIndex < parameters.Count(p => p.ParameterType == typeof(float)))
                        {
                            val = actionValues.FloatVals[floatIndex];
                            actionArgs.Add(val);
                        }
                        else
                        {
                            actionArgs.Add(null);
                        }
                        floatIndex++;
                    }
                }
                return new ScenicMovementData(pos, modelType, actionFunc, actionArgs);
            }
            return new ScenicMovementData(pos, modelType);
        }

        return new ScenicMovementData(pos, modelType);
    }
    public void HandleControl(ScenicJson data)
    {
        if (data.Control)
        {
            if (data.AddObject && data.SpawnObjectQueue.Count != 0)
            {
                foreach (Object p in data.SpawnObjectQueue)
                {
                    Vector3 v = ListToVector(p.Position);
                    Quaternion rot = ListToQuaternion(p.Rotation);
                    //Scenic uses right hand coord system so have to flip?
                    rot.y = -rot.y;
                    rot.x = -rot.x;
                    rot.z = -rot.z;
                    string tag = p.Model.ModelType;
                    InstantiateScenicObject instObj = new InstantiateScenicObject(v, rot, tag);
                }
            }
            else if (data.Destroy)
            {
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

    // Helper Functions

    private Vector3 ListToVector(List<float> v)
    {
        /*
        v[0] = x
        v[1] = y
        v[2] = z
        NOTE: We swap y and z because scenic uses z as vertical axis
        */
        return new Vector3(v[0], v[2], v[1]);
    }
    private Quaternion ListToQuaternion(List<float> q)
    {
        /*
        q[0] = x
        q[1] = y
        q[2] = z
        q[3] = w
        NOTE: We switch y and z
        */
        return new Quaternion(q[0], q[2], q[1], q[3]);
    }
    

    //Json deparsing stuff here 

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
        public bool Destroy{ get; set; }
    }
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

    public partial class HUD
    {
        [JsonProperty("message")]
        public List<string> Message { get; set; }
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
        [JsonProperty("position")]
        public string Position { get; set; }
        [JsonProperty("thBoActive")]
        public bool ThBoActive { get; set; }
        [JsonProperty("brakeActive")]
        public bool BrakeActive { get; set; }
    }

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

        [JsonProperty("holdingDisc")]
        public bool HoldingDisc { get; set; }

        [JsonProperty("holdingWall")]
        public bool HoldingWall { get; set; }

        [JsonProperty("wallHeading")]
        public List<float> WallHeading { get; set;}

        [JsonProperty("pushMagnitude")]
        public float PushMagnitude { get; set; }

        [JsonProperty("brake")]
        public bool Brake { get; set; }

        [JsonProperty("velocity")]
        public List<float> Velocity { get; set; }

        [JsonProperty("angularVelocity")]
        public List<float> AngularVelocity { get; set; }

        [JsonProperty("speed")]
        public float Speed { get; set; }

        [JsonProperty("velocityStop")]
        public bool VelocityStop { get; set; }

        [JsonProperty("thrustHeading")]
        public List<float> ThrustHeading { get; set; }

        [JsonProperty("thrustOn")]
        public bool ThrustOn { get; set; }

        [JsonProperty("discHeading")]
        public List<float> DiscHeading { get; set; }

        [JsonProperty("throwMagnitude")]
        public float ThrowMagnitude { get; set; }

        [JsonProperty("thBoActive")]
        public bool ThBoActive { get; set; }
        
        // [JsonProperty("doMove")]
        // public bool DoMove { get; set; }
        // [JsonProperty("moveToPosition")]
        // public List<float> MoveToPosition { get; set; }
        [JsonProperty("actionDict")]
        public Dictionary<string, ActionDictType> ActionDict { get; set; }
        [JsonProperty("doMercunaFollow")]
        public bool DoMercunaFollow { get; set; }
        [JsonProperty("mercunaID")]
        public int MercunaID { get; set; }
        [JsonProperty("mercunaDistance")]
        public int MercunaDistance { get; set; }
        [JsonProperty("doTransform")]
        public bool DoTransform { get; set; }
        [JsonProperty("destroy")]
        public bool Destroy { get; set; }
        [JsonProperty("doPunch")]
        public bool DoPunch { get; set; }

        // Added variables to the Player Class
        // They will be read by the HandleMovementData above to populate the ScenicMovementData
        [JsonProperty("throwTop")]
        public bool DoThrowTop { get; set; }
        [JsonProperty("throwSide")]
        public bool throwSide { get; set; }
        [JsonProperty("transformPosition")]
        public List<float> TransformPosition { get; set; }
        [JsonProperty("doLineDraw")]
        public bool DoLineDraw { get; set; }
        [JsonProperty("lineDestination")]
        public List<List<float>> LineDestination { get; set; }
        [JsonProperty("stopButton")]
        public bool Stopbutton { get; set; }
        [JsonProperty("heldByHuman")]
        public bool HeldByHuman { get; set; }
        [JsonProperty("heldByScenic")]
        public bool HeldByScenic { get; set; }
        [JsonProperty("topSpeed")]
        public float TopSpeed { get; set; }
        [JsonProperty("catchRadius")]
        public float CatchRadius { get; set; }
    }
}

public partial class ActionDictType
{
    [JsonProperty("intVals")]
    public List<int> IntVals{ get; set; }
    [JsonProperty("floatVals")]
    public List<float> FloatVals{ get; set; }
    [JsonProperty("stringVals")]
    public List<string> StringVals{ get; set; }
    [JsonProperty("tupleVals")]
    public List<List<float>> TupleVals{ get; set; }
    [JsonProperty("boolVals")]
    public List<bool> BoolVals{ get; set; }
}
