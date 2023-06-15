using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;


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
        // foreach (Player p in t.Players)
        // {
        //     objData.Add(HandleMovementData(p));
        // }
        return objData;
    }
    /*private ScenicMovementData HandleMovementData(Player data)
    {
        Dictionary<string, float> floatVals = new Dictionary<string, float>();
        Dictionary<string, bool> boolVals = new Dictionary<string, bool>();
        Dictionary<string, Vector3> vectorVals = new Dictionary<string, Vector3>();
        Dictionary<string, Quaternion> quaternionVals = new Dictionary<string, Quaternion>();
        Dictionary<string, int> intVals = new Dictionary<string, int>();
        Dictionary<string, string> stringVals = new Dictionary<string, string>();
        Dictionary<string, List<string>> listVals = new Dictionary<string, List<string>>(); 
        Dictionary<string, List<Vector3>> listVectorVals = new Dictionary<string, List<Vector3>>();
        Vector3 pos = ListToVector(data.Position);
        Vector3 wHead = ListToVector(data.WallHeading);
        Vector3 vel = ListToVector(data.Velocity);
        Vector3 angVel = ListToVector(data.AngularVelocity);
        Vector3 tHead = ListToVector(data.ThrustHeading);
        Vector3 dHead = ListToVector(data.DiscHeading);
        Vector3 mPos = ListToVector(data.MercunaPosition);
        Vector3 tPos = ListToVector(data.TransformPosition);
        List<Vector3> lineDest = new List<Vector3>();
        foreach (List<float> linePos in data.LineDestination)
        {
            lineDest.Add(ListToVector(linePos));
        }
        Quaternion rot = ListToQuaternion(data.Rotation);
        vectorVals.Add("Position", pos);
        vectorVals.Add("WallHeading", wHead);
        vectorVals.Add("Velocity", vel);
        vectorVals.Add("AngularVelocity", angVel);
        vectorVals.Add("ThrustHeading", tHead);
        vectorVals.Add("DiscHeading", dHead);
        floatVals.Add("PushMagnitude", data.PushMagnitude);
        floatVals.Add("Speed", data.Speed);
        floatVals.Add("ThrowMagnitude", data.ThrowMagnitude);
        boolVals.Add("HoldingDisc", data.HoldingDisc);
        boolVals.Add("HoldingWall", data.HoldingWall);
        boolVals.Add("Brake", data.Brake);
        boolVals.Add("VelocityStop", data.VelocityStop);
        boolVals.Add("ThrustOn", data.ThrustOn);
        boolVals.Add("ThBoActive", data.ThBoActive);
        quaternionVals.Add("Rotation", rot);

        boolVals.Add("DoMercunaMove", data.DoMercunaMove);
        boolVals.Add("DoMercunaFollow", data.DoMercunaFollow);

        intVals.Add("MercunaID", data.MercunaID);
        intVals.Add("MercunaDistance", data.MercunaDistance);
        
        vectorVals.Add("MercunaPosition", mPos);

        floatVals.Add("RedColor", data.Model.color[0]);
        floatVals.Add("GreenColor", data.Model.color[1]);
        floatVals.Add("BlueColor", data.Model.color[2]);
        floatVals.Add("Opacity", data.Model.color[3]);
        stringVals.Add("ModelType", data.Model.ModelType);
        boolVals.Add("DoTransform", data.DoTransform);
        boolVals.Add("Destroy", data.Destroy);
        listVals.Add("Message", data.HUD.Message);
        boolVals.Add("Enabled", data.HUD.Enabled);
        stringVals.Add("Position", data.HUD.Position);
        boolVals.Add("HumanThBoActive", data.HUD.ThBoActive);
        boolVals.Add("DoPunch", data.DoPunch);

        // Adding variables to the ScenicMovementData
        // Follows the format: "typeVals.Add("name", data.val)"
        boolVals.Add("DoThrowTop", data.DoThrowTop);
        boolVals.Add("ThrowSide", data.throwSide);
        vectorVals.Add("TransformPosition", tPos);

        listVectorVals.Add("LineDestination", lineDest);
        boolVals.Add("DoLineDraw", data.DoLineDraw);
        boolVals.Add("StopButton", data.Stopbutton);
        boolVals.Add("BrakeActive", data.HUD.BrakeActive);
        
        floatVals.Add("TopSpeed", data.TopSpeed);
        floatVals.Add("CatchRadius", data.CatchRadius);
        return new ScenicMovementData(intVals, boolVals, floatVals, vectorVals, quaternionVals, stringVals, listVals, listVectorVals);
    }*/
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
                    //For whatever reason scenic flips it
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
        else{
            return;
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
        public List<Object> Players { get; set; }

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
        
        [JsonProperty("doMercunaMove")]
        public bool DoMercunaMove { get; set; }
        [JsonProperty("mercunaPosition")]
        public List<float> MercunaPosition { get; set; }
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
