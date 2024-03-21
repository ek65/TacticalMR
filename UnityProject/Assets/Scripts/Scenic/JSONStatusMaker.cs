using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class JSONStatusMaker : MonoBehaviour
{
    ObjectsList objectList;
    Root root;
    // private TickData tickData;
    private bool snapTurnedLastTimestep;
    private int lastTick;
    private ZMQServer server;

    void Start()
    {
        //we want to start this from the server?
        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        lastTick = -1;
        server = GetComponent<ZMQServer>();
    }
    public string getUnityData()
    {
        var test = root.TickData.Ball.movementData.transform;
        //lets set this to false so that we do not re-send if not true
        // Debug.LogError("test: " + test.x +"," + test.y +","+ test.z);
        snapTurnedLastTimestep = false;
        return JsonConvert.SerializeObject(root);
    }
    void Update()
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
        Rigidbody rb = player.GetComponentInChildren<Rigidbody>();
        GameObject rig = rb.gameObject;
        if (player.GetComponent<ExitScenario>() != null && lastTick > 5)
        {
            
            pData.movementData.stopButton = player.GetComponent<ExitScenario>().endScenario;
            if (pData.movementData.stopButton == true)
            {
                Debug.LogError("stopbutton: " + pData.movementData.stopButton);
            }
            // dont need to set endScenario to false here because it is set to false in InstantiateScenicObject on the next simulation
            
            // assumes that the player has a reference to TimelineManager since ExitScenario is not null, so I'm not checking if it's null
            TimelineManager tlManager =
                GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
            // pData.movementData.pause = tlManager.Paused;

        }
        pData.movementData.speed = rb.velocity.magnitude;
        //NOTE: We go from (x,y,z) to (x,z,y) because that is how scenic handles the coordinate system.
        Vector3ToJsonClass(rb.angularVelocity, pData.movementData.angularVelocity);
        Vector3ToJsonClass(rb.velocity, pData.movementData.velocity);
        // MercunaAI mercuna = player.GetComponentInChildren<MercunaAI>();
        // if (mercuna != null && mercuna.mercunaPath != null)
        // {
        //     Vector3ListToJsonList(mercuna.mercunaPath, pData.movementData.path);
        // }
        
        // if (pI != null)
        // {
        //     bool b = (pI.trigger || pI.laserPointed);
        //     pData.movementData.trigger = b;
        // }
        
        // pData.clientID = ((int)player.GetComponent<NetworkObject>().NetworkObjectId);
        if (isHuman)
        {
            GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
            Quaternion realRotation = camera.transform.rotation;
            Vector3ToJsonClass(rig.transform.position, pData.movementData.transform);
            // QuaternionToJsonClass(realRotation, pData.movementData.rotation);
            QuaternionToJsonClass(rig.transform.rotation, pData.movementData.rotation);

        }
        else
        {
            PlayerInterface pI = rig.GetComponent<PlayerInterface>();
            Debug.LogError(pI.ballPossession);
            pData.movementData.ballPossession = pI.ballPossession;
            Vector3 offsetPos = new Vector3(rig.transform.position.x, rig.transform.position.y, rig.transform.position.z);
            Vector3ToJsonClass(offsetPos, pData.movementData.transform);
            // Debug.LogError(offsetPos);
            QuaternionToJsonClass(rig.transform.rotation, pData.movementData.rotation);
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
        dData.movementData.speed = rb.velocity.magnitude;
        Vector3ToJsonClass(rb.angularVelocity, dData.movementData.angularVelocity);
        Vector3ToJsonClass(rb.velocity, dData.movementData.velocity);
        Vector3ToJsonClass(disc.transform.position, dData.movementData.transform);
        QuaternionToJsonClass(disc.transform.rotation, dData.movementData.rotation);
        // DiscOwnership ownership = disc.GetComponent<DiscOwnership>();
        // dData.movementData.heldByHuman = ownership.heldByHuman;
        // dData.movementData.heldByScenic = ownership.heldByScenic;
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
            speed = 0.0;
            velocity = new Vector3Json();
            angularVelocity = new Vector3Json();
            rotation = new QuaternionJson();
            stopButton = false;
            ballPossession = false;
            heldByHuman = false;
            heldByScenic = false;
        }
        public Vector3Json transform { get; set; }
        public double speed { get; set; }
        public Vector3Json velocity { get; set; }
        public Vector3Json angularVelocity { get; set; }
        public QuaternionJson rotation { get; set; }
        public bool stopButton { get; set; }
        public bool ballPossession { get; set; }
        public bool heldByHuman { get; set; }
        public bool heldByScenic { get; set; }
        
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
