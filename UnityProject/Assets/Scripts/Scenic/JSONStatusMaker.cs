using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Pathfinding;

public class JSONStatusMaker : MonoBehaviour
{
    ObjectsList objectList;
    BallOwnership ownership;
    Root root;
    // private TickData tickData;
    private bool snapTurnedLastTimestep;
    private int lastTick;
    private ZMQServer server;
    void Start()
    {
        //we want to start this from the server?
        var scenicManager = GameObject.FindGameObjectWithTag("ScenicManager");
        objectList = scenicManager.GetComponent<ObjectsList>();
        ownership = scenicManager.GetComponent<BallOwnership>();
        lastTick = -1;
        server = GetComponent<ZMQServer>();
    }
    public string getUnityData()
    {
        // var test = root.TickData.Ball.movementData.transform;
        //lets set this to false so that we do not re-send if not true
        // Debug.LogError("test: " + test.x +"," + test.y +","+ test.z);
        snapTurnedLastTimestep = false;
        return JsonConvert.SerializeObject(root);
    }
    void LateUpdate()
    {
        lastTick = server.lastTick;
        //pull the ball and the players from objects list and get their data.
        Root r = new Root();
        //We need to change this later when we expand to 1-4 players.
        r.TickData.numPlayers = objectList.humanPlayers.Count + objectList.scenicPlayers.Count;
        GameObject ball = objectList.ballObject;
        for (int i = 0; i < objectList.humanPlayers.Count; i++)
        {
            GameObject humanPlayer = objectList.humanPlayers[i];
            Player p = new Player();
            // p.leftController = new ControllerInputData();
            // p.rightController = new ControllerInputData();
            AddPlayerData(humanPlayer, p, true);
            // Rigidbody rb = humanPlayer.GetComponentInChildren<Rigidbody>();
            // GameObject rig = rb.gameObject;
            // ObjectList xrRigObjects = rig.GetComponent<ObjectList>();
            // GameObject leftController = xrRigObjects.leftController;
            // GameObject rightController = xrRigObjects.rightController;
            // AddControllerData(rig, leftController, p.leftController, true);
            // AddControllerData(rig, rightController, p.rightController, false);
        
            r.TickData.HumanPlayers.Add(p);
        }
        for (int i = 0; i < objectList.scenicPlayers.Count; i++)
        {
            Player p = new Player();
            AddPlayerData(objectList.scenicPlayers[i], p, false);
            // Debug.LogError(p.movementData);
            r.TickData.ScenicPlayers.Add(p);
        }
        for (int i = 0; i < objectList.scenicObjects.Count; i++)
        {
            GameObject obj = objectList.scenicObjects[i];
            if (obj != ball)
            {
                Player p = new Player();
                AddObjectData(obj, p);
                r.TickData.ScenicObjects.Add(p);
            }
            
        }

        if (ball != null)
        {
            AddBallData(ball, r.TickData.Ball);
        }

        root = r;
    }
    // UNUSED. Left for reference (from echo arena project) if we want to add a whole new class for controller data
    /*void AddControllerData(GameObject humanRig, GameObject controller, ControllerInputData cData, bool isLeftController)
    {
        //NOTE: this does not read the actual button press but rather if they are in "Thrust" or not
        Thrust controllerThrust = controller.GetComponent<Thrust>();
        cData.primaryButton = controllerThrust.thrustOn;
        if (isLeftController)
        {
            cData.primary2DAxisClick = controllerThrust.boostOn;
        }
        else
        {
            cData.primary2DAxisClick = controllerThrust.brakeOn;
            bool snapTurnOnFrame = controller.GetComponent<SnapTurnMonitor>().snapTurnEnabled;
            if (!snapTurnedLastTimestep && snapTurnOnFrame)
            {
                //ensure we send on timestep
                cData.primary2DAxis = true;
                snapTurnedLastTimestep = true;
            }
        }
        Holder holder = humanRig.GetComponent<Holder>();
        Debug.LogWarning("This is what the human grip looks like: " + holder.altHeld.ToString());
        if (holder.altHeld){
            //player is holding something --> just set true, we can compare with ball to see if it is holding ball or wall or nothing
            cData.gripButton = true;
        }
    }*/
    void AddPlayerData(GameObject player, Player pData, bool isHuman) {
        
        
        // Unused for now
        // pData.clientID = ((int)player.GetComponent<NetworkObject>().NetworkObjectId);
        
        /* NOTE: We can't get speed, velocity, and angular velocity from rigidbody because we're using this RichAI pathfinding thing for movement
        angular velocity sent is (0,0,0) right now since we don't have a way to get it from rigidbody and it's not really important.
        If we really DO want angular velocity, we have to calculate it ourselves */
        
        // Vector3ToJsonClass(rb.angularVelocity, pData.movementData.angularVelocity);
        // Vector3ToJsonClass(rb.velocity, pData.movementData.velocity);
        
        if (isHuman)
        {
            HumanInterface hI = player.GetComponent<HumanInterface>();
            
            //NOTE: We go from (x,y,z) to (x,z,y) because that is how scenic handles the coordinate system.
            Vector3 pos = player.transform.position;
            if (hI.isVR)
            {
                pos = hI.vrTransform.position;
            }
            Vector3ToJsonClass(pos, pData.movementData.transform);

            Quaternion rot = player.transform.rotation;
            if (hI.isVR)
            {
                rot = hI.vrTransform.rotation;
            }
            QuaternionToJsonClass(rot, pData.movementData.rotation);

            // dont need to set endScenario back to false here because it is set to false in InstantiateScenicObject on the next simulation
            if (player.GetComponent<ExitScenario>() != null && lastTick > 5)
            {
                pData.movementData.stopButton = player.GetComponent<ExitScenario>().endScenario;
            }

            TimelineManager tlManager =
                GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
            pData.movementData.pause = tlManager.Paused;

            // TODO: should change this when we have better movement system for human
            Vector3 velo = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>().movement;
            if (hI.isVR)
            {
                velo = hI.velocity;
            }
            Vector3ToJsonClass(velo, pData.movementData.velocity);
            pData.movementData.speed = velo.magnitude;
            pData.movementData.ballPossession = hI.ballPossession;
            pData.movementData.isMoving = hI.isMoving;
            Vector3ToJsonClass(hI.xMark, pData.movementData.xMark);
            pData.movementData.triggerPass = hI.triggerPass;
            pData.movementData.behavior = hI.behavior.Value;
            
        }
        else // non-human player
        {
            //NOTE: We go from (x,y,z) to (x,z,y) because that is how scenic handles the coordinate system.
            Vector3ToJsonClass(player.transform.position, pData.movementData.transform);
            QuaternionToJsonClass(player.transform.rotation, pData.movementData.rotation);
            
            PlayerInterface pI = player.GetComponent<PlayerInterface>();
            pData.movementData.ballPossession = pI.ballPossession;
            pData.movementData.isMoving = pI.isMoving;
            pData.movementData.behavior = pI.behavior.Value;
            Vector3 velo = pI.currVelocity;
            Vector3ToJsonClass(velo, pData.movementData.velocity);
            pData.movementData.speed = velo.magnitude;
        }
        
    }
    void AddObjectData(GameObject obj, Player oData)
    {
        // NOTE: "Player" class can still be an object
        Vector3ToJsonClass(obj.transform.position, oData.movementData.transform);
        QuaternionToJsonClass(obj.transform.rotation, oData.movementData.rotation);
        // oData.clientID = ((int) obj.GetComponent<NetworkObject>().NetworkObjectId);
    }
    void AddBallData(GameObject disc, Ball dData) {
        Rigidbody rb = disc.GetComponent<Rigidbody>();
        dData.movementData.speed = rb.linearVelocity.magnitude;
        Vector3ToJsonClass(rb.angularVelocity, dData.movementData.angularVelocity);
        Vector3ToJsonClass(rb.linearVelocity, dData.movementData.velocity);
        Vector3ToJsonClass(disc.transform.position, dData.movementData.transform);
        QuaternionToJsonClass(disc.transform.rotation, dData.movementData.rotation);
        // DiscOwnership ownership = disc.GetComponent<DiscOwnership>();
        dData.movementData.heldByHuman = ownership.heldByHuman;
        dData.movementData.heldByScenic = ownership.heldByScenic;
        // dData.clientID = ((int)disc.GetComponent<NetworkObject>().NetworkObjectId);
    }
    void Vector3ListToJsonList(List<Vector3> v3List, List<Vector3Json> v3jList)
    {
        for(int i = 0; i < v3List.Count; i++)
        {
            Vector3Json v3j = new Vector3Json();
            Vector3ToJsonClass(v3List[i], v3j);
            v3jList.Add(v3j);
        }
    }
    void Vector3ToJsonClass(Vector3 v, Vector3Json vj)
    {
        //we change to (x,z,y) for scenic
        vj.x = v.x;
        vj.z = v.y;
        vj.y = v.z;
    }
    void QuaternionToJsonClass(Quaternion q, QuaternionJson qj)
    {
        qj.x = q.x;
        qj.z = q.y;
        qj.y = q.z;
        qj.w = q.w;
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Vector3Json
    {
        public Vector3Json()
        {
            this.x = 0f;
            this.y = 0f;
            this.z = 0f;
        }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
    public class QuaternionJson
    {
        public QuaternionJson()
        {
            this.x = 0f;
            this.y = 0f;
            this.z = 0f;
            this.w = 1f;
        }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float w { get; set; }
    }
    /*
    public class Vector2Json
    {
        public Vector2Json()
        {
            this.x = 0f;
            this.y = 0f;
        }
        public float x { get; set; }
        public float y { get; set; }
    }
    */
    public class MovementData
    {
        public MovementData(){
            transform = new Vector3Json();
            speed = 0.0f;
            velocity = new Vector3Json();
            angularVelocity = new Vector3Json();
            rotation = new QuaternionJson();
            stopButton = false;
            pause = false;
            ballPossession = false;
            isMoving = false;
            xMark = new Vector3Json();
            triggerPass = false;
            heldByHuman = false;
            heldByScenic = false;
            behavior = "";
        }
        public Vector3Json transform { get; set; }
        public float speed { get; set; }
        public Vector3Json velocity { get; set; }
        public Vector3Json angularVelocity { get; set; }
        public QuaternionJson rotation { get; set; }
        public bool stopButton { get; set; }
        public bool pause { get; set; }
        public bool ballPossession { get; set; }
        public bool isMoving { get; set; }
        public Vector3Json xMark { get; set; }
        public bool triggerPass { get; set; }
        public bool heldByHuman { get; set; }
        public bool heldByScenic { get; set; }
        public string behavior { get; set; }
        
    }
    public class ControllerInputData
    {
    /*
    Modeled off of https://docs.unity3d.com/2020.3/Documentation/Manual/xr_input.html
    */
    //NOTE: This is biased towards obtaining what action the human is acting. This will NOT get raw input data.
        public ControllerInputData()
        {
            //primary2DAxis = new Vector2Json();
            //Because of scenic running at 60hz and scenic at 10, snap turn happens on a single frame. We cannot capture that raw input consistently.
            primary2DAxis = false;
            primaryButton = false;
            secondaryButton = false;
            gripButton = false;
            primary2DAxisClick = false;
            //Note, these two are unused for now
            triggerButton = false;
            menuButton = false;
        }
        public bool primary2DAxis;
        public bool primaryButton;
        public bool secondaryButton;
        public bool gripButton;
        public bool triggerButton;
        public bool menuButton;
        public bool primary2DAxisClick;
    }

    public class Ball
    {
        public Ball() {
            movementData = new MovementData();
        }
        public MovementData movementData {get; set;}
        
        //TODO: We need to define this within a ball
        public int clientID {get; set;}
    }

    public class Player
    {
        public Player() {
            movementData = new MovementData();
        }
        public MovementData movementData {get; set;}
        public int clientID {get; set;}
    }

    public class TickData
    {
        public TickData(){
            this.Ball = new Ball();
            this.numPlayers = 0;
            this.HumanPlayers = new List<Player>();
            this.ScenicPlayers = new List<Player>();
            this.ScenicObjects = new List<Player>();
        }
        public int numPlayers { get; set; }
        public Ball Ball { get; set; }
        public List<Player> HumanPlayers { get; set; }
        public List<Player> ScenicPlayers { get; set; }
        public List<Player> ScenicObjects { get; set; }
    }
    public class Root
    {
        public Root(){
            this.TickData = new TickData();
        }
        public TickData TickData { get; set; }
    }
}
