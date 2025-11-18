using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Pathfinding;

/// <summary>
/// Serializes Unity game state data to JSON format for communication with the Scenic simulation system.
/// Converts player positions, ball state, and game status into structured data that can be transmitted
/// over the network to external AI systems for analysis and decision making.
/// </summary>
public class JSONStatusMaker : MonoBehaviour
{
    #region Private Fields
    /// <summary>
    /// Reference to the central object list manager
    /// </summary>
    ObjectsList objectList;
    
    /// <summary>
    /// Reference to ball ownership tracking system
    /// </summary>
    BallOwnership ownership;
    
    /// <summary>
    /// Root data structure for JSON serialization
    /// </summary>
    Root root;
    
    /// <summary>
    /// Tracks previous tick for change detection
    /// </summary>
    private int lastTick;
    
    /// <summary>
    /// Reference to the ZMQ server for tick information
    /// </summary>
    private ZMQServer server;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        var scenicManager = GameObject.FindGameObjectWithTag("ScenicManager");
        objectList = scenicManager.GetComponent<ObjectsList>();
        ownership = scenicManager.GetComponent<BallOwnership>();
        lastTick = -1;
        server = GetComponent<ZMQServer>();
    }

    void LateUpdate()
    {
        UpdateGameStateData();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Serializes current game state to JSON string for transmission to Scenic
    /// </summary>
    /// <returns>JSON string containing complete game state data</returns>
    public string getUnityData()
    {
        return JsonConvert.SerializeObject(root);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Updates the complete game state data structure with current information
    /// </summary>
    private void UpdateGameStateData()
    {
        lastTick = server.lastTick;
        
        Root r = new Root();
        r.TickData.numPlayers = objectList.humanPlayers.Count + objectList.scenicPlayers.Count;
        GameObject ball = objectList.ballObject;
        
        // Process human players
        for (int i = 0; i < objectList.humanPlayers.Count; i++)
        {
            GameObject humanPlayer = objectList.humanPlayers[i];
            Player p = new Player();
            AddPlayerData(humanPlayer, p, true);
            r.TickData.HumanPlayers.Add(p);
        }
        
        // Process AI-controlled scenic players
        for (int i = 0; i < objectList.scenicPlayers.Count; i++)
        {
            Player p = new Player();
            AddPlayerData(objectList.scenicPlayers[i], p, false);
            r.TickData.ScenicPlayers.Add(p);
        }
        
        // Process other scenic objects (goals, lines, etc.)
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

        // Process ball data
        if (ball != null)
        {
            AddBallData(ball, r.TickData.Ball);
        }

        root = r;
    }

    /// <summary>
    /// Extracts and formats player data for JSON serialization
    /// </summary>
    /// <param name="player">Player GameObject to extract data from</param>
    /// <param name="pData">Player data structure to populate</param>
    /// <param name="isHuman">Whether this is a human-controlled player</param>
    void AddPlayerData(GameObject player, Player pData, bool isHuman) 
    {
        if (isHuman)
        {
            AddHumanPlayerData(player, pData);
        }
        else
        {
            AddScenicPlayerData(player, pData);
        }
    }

    /// <summary>
    /// Adds human player specific data including VR support and input handling
    /// </summary>
    /// <param name="player">Human player GameObject</param>
    /// <param name="pData">Player data structure to populate</param>
    private void AddHumanPlayerData(GameObject player, Player pData)
    {
        HumanInterface hI = player.GetComponent<HumanInterface>();
        
        // Handle position (VR vs standard)
        Vector3 pos = player.transform.position;
        if (hI.isVR)
        {
            pos = hI.vrTransform.position;
        }
        Vector3ToJsonClass(pos, pData.movementData.transform);

        // Handle rotation (VR vs standard)
        Quaternion rot = player.transform.rotation;
        if (hI.isVR)
        {
            rot = hI.vrTransform.rotation;
        }
        QuaternionToJsonClass(rot, pData.movementData.rotation);

        // Handle exit scenario detection
        if (player.GetComponent<ExitScenario>() != null && lastTick > 5)
        {
            pData.movementData.stopButton = player.GetComponent<ExitScenario>().endScenario;
        }

        // Handle pause state
        TimelineManager tlManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        pData.movementData.pause = tlManager.Paused;

        // Handle velocity (keyboard vs VR)
        Vector3 velo = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>().movement;
        if (hI.isVR)
        {
            velo = hI.velocity;
        }
        Vector3ToJsonClass(velo, pData.movementData.velocity);
        pData.movementData.speed = velo.magnitude;
        
        // Add game state information
        pData.movementData.ballPossession = hI.ballPossession;
        pData.movementData.isMoving = hI.isMoving;
        Vector3ToJsonClass(hI.xMark, pData.movementData.xMark);
        pData.movementData.triggerPass = hI.triggerPass;
        pData.movementData.behavior = hI.behavior.Value;
        
        pData.movementData.handRaised = hI.handRaised;
    }

    /// <summary>
    /// Adds AI-controlled scenic player data
    /// </summary>
    /// <param name="player">Scenic player GameObject</param>
    /// <param name="pData">Player data structure to populate</param>
    private void AddScenicPlayerData(GameObject player, Player pData)
    {
        Vector3ToJsonClass(player.transform.position, pData.movementData.transform);
        QuaternionToJsonClass(player.transform.rotation, pData.movementData.rotation);
        
        PlayerInterface pI = player.GetComponent<PlayerInterface>();
        pData.movementData.ballPossession = pI.ballPossession;
        pData.movementData.isMoving = pI.isMoving;
        pData.movementData.behavior = pI.behavior.Value;
        Vector3 velo = pI.currVelocity;
        Vector3ToJsonClass(velo, pData.movementData.velocity);
        pData.movementData.speed = velo.magnitude;
        
        pData.movementData.handRaised = pI.handRaised;
    }

    /// <summary>
    /// Extracts data from static objects like goals and field markers
    /// </summary>
    /// <param name="obj">Object to extract data from</param>
    /// <param name="oData">Data structure to populate</param>
    void AddObjectData(GameObject obj, Player oData)
    {
        Vector3ToJsonClass(obj.transform.position, oData.movementData.transform);
        QuaternionToJsonClass(obj.transform.rotation, oData.movementData.rotation);
        
        // if this object is a box, get isPackaged state
        BoxInterface bI = obj.GetComponent<BoxInterface>();
        if (bI != null)
        {
            oData.movementData.isPackaged = bI.isPackaged;
        }
    }

    /// <summary>
    /// Extracts ball physics and ownership data
    /// </summary>
    /// <param name="disc">Ball GameObject</param>
    /// <param name="dData">Ball data structure to populate</param>
    void AddBallData(GameObject disc, Ball dData) 
    {
        Rigidbody rb = disc.GetComponent<Rigidbody>();
        dData.movementData.speed = rb.linearVelocity.magnitude;
        Vector3ToJsonClass(rb.angularVelocity, dData.movementData.angularVelocity);
        Vector3ToJsonClass(rb.linearVelocity, dData.movementData.velocity);
        Vector3ToJsonClass(disc.transform.position, dData.movementData.transform);
        QuaternionToJsonClass(disc.transform.rotation, dData.movementData.rotation);
        dData.movementData.heldByHuman = ownership.heldByHuman;
        dData.movementData.heldByScenic = ownership.heldByScenic;
    }
    #endregion

    #region Coordinate System Conversion
    /// <summary>
    /// Converts Unity Vector3 to JSON format with coordinate system transformation.
    /// Unity uses (x,y,z) while Scenic uses (x,z,y) coordinate system.
    /// </summary>
    /// <param name="v">Unity Vector3</param>
    /// <param name="vj">JSON Vector3 structure to populate</param>
    void Vector3ToJsonClass(Vector3 v, Vector3Json vj)
    {
        vj.x = v.x;
        vj.z = v.y;  // Unity Y becomes Scenic Z
        vj.y = v.z;  // Unity Z becomes Scenic Y
    }

    /// <summary>
    /// Converts Unity Quaternion to JSON format with coordinate system transformation
    /// </summary>
    /// <param name="q">Unity Quaternion</param>
    /// <param name="qj">JSON Quaternion structure to populate</param>
    void QuaternionToJsonClass(Quaternion q, QuaternionJson qj)
    {
        qj.x = q.x;
        qj.z = q.y;  // Unity Y becomes Scenic Z
        qj.y = q.z;  // Unity Z becomes Scenic Y
        qj.w = q.w;
    }
    #endregion

    #region JSON Data Structures
    /// <summary>
    /// JSON representation of a 3D vector with Scenic coordinate system
    /// </summary>
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

    /// <summary>
    /// JSON representation of a quaternion rotation
    /// </summary>
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

    /// <summary>
    /// Movement and state data for any game object
    /// </summary>
    public class MovementData
    {
        public MovementData()
        {
            transform = new Vector3Json();
            speed = 0.0f;
            velocity = new Vector3Json();
            angularVelocity = new Vector3Json();
            rotation = new QuaternionJson();
            stopButton = false;
            pause = false;
            ballPossession = false;
            handRaised = false;
            isPackaged = false;
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
        public bool handRaised { get; set; }
        public bool isPackaged { get; set; }
        public bool isMoving { get; set; }
        public Vector3Json xMark { get; set; }
        public bool triggerPass { get; set; }
        public bool heldByHuman { get; set; }
        public bool heldByScenic { get; set; }
        public string behavior { get; set; }
    }

    /// <summary>
    /// Ball-specific data structure
    /// </summary>
    public class Ball
    {
        public Ball() 
        {
            movementData = new MovementData();
        }
        public MovementData movementData { get; set; }
        public int clientID { get; set; }
    }

    /// <summary>
    /// Player data structure (also used for static objects)
    /// </summary>
    public class Player
    {
        public Player() 
        {
            movementData = new MovementData();
        }
        public MovementData movementData { get; set; }
        public int clientID { get; set; }
    }

    /// <summary>
    /// Complete tick data for one simulation frame
    /// </summary>
    public class TickData
    {
        public TickData()
        {
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

    /// <summary>
    /// Root data structure for JSON serialization
    /// </summary>
    public class Root
    {
        public Root()
        {
            this.TickData = new TickData();
        }
        public TickData TickData { get; set; }
    }
    #endregion
}