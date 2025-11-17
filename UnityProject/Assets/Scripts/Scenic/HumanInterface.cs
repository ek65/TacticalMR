using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using System.Linq;
using Fusion;
using Pathfinding;
using UnityEngine.InputSystem;
using Utilities.Extensions;

/// <summary>
/// Main interface for human player objects in the networked multiplayer environment.
/// Handles both VR and non-VR player interactions, ball possession, movement, and communication with AI systems.
/// Implements IObjectInterface to receive commands from Scenic simulation.
/// </summary>
public class HumanInterface : NetworkBehaviour, IObjectInterface
{
    #region Network Properties
    [Networked, OnChangedRender(nameof(UpdateGameObjectName))]
    public NetworkString<_32> ObjName { get; set; }
    
    [Networked] public NetworkBool ballPossession { get; set; }
    
    [Networked] public NetworkBool handRaised { get; set; }
    
    [Networked] public NetworkString<_32> behavior { get; set; }
    #endregion

    #region Inspector Fields
    /// <summary>
    /// Whether this player is using VR controls
    /// </summary>
    public bool isVR = false;

    /// <summary>
    /// Whether this player is just viewing (spectator mode)
    /// </summary>
    public bool isViewer = false;
    
    /// <summary>
    /// Reference to ActionAPI for executing player actions
    /// </summary>
    public ActionAPI actionAPI;
    
    /// <summary>
    /// Prefab for generating arrow indicators
    /// </summary>
    public GameObject arrowGenerator;
    
    /// <summary>
    /// Prefab for generating circle indicators
    /// </summary>
    public GameObject circleGenerator;

    /// <summary>
    /// Forward direction arrow for ball possession
    /// </summary>
    public GameObject forwardArrow;

    /// <summary>
    /// Ball object reference
    /// </summary>
    public GameObject ball;
    
    /// <summary>
    /// Position where ball attaches when possessed
    /// </summary>
    public Transform ballPosition;
    
    /// <summary>
    /// VR headset transform for VR players
    /// </summary>
    public Transform vrTransform;
    
    /// <summary>
    /// VR collision detection object
    /// </summary>
    public GameObject vrCollider;
    
    /// <summary>
    /// Position where objects are held in factory scenarios
    /// </summary>
    public Transform objectPosition;

    /// <summary>
    /// UI text displaying current behavior
    /// </summary>
    public FloatingText floatingBehaviorText;
    
    /// <summary>
    /// UI text displaying player name
    /// </summary>
    public FloatingText floatingNameText;

    /// <summary>
    /// Player shirt renderer for team colors
    /// </summary>
    public Renderer shirt;
    #endregion

    #region Private Fields
    private int localTick;
    private ExitScenario exitScene;
    private AudioSource source;
    private TimelineManager tlManager;
    private ObjectsList objectList;
    private bool circleSpawned = false;
    private bool arrowSpawned = false;
    private bool canPossessBall = true;
    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    private Queue<float> timeHistory = new Queue<float>();
    private int maxHistoryFrames = 5;
    private float minDeltaTime = 0.01f;
    private Vector3 lastPosition;
    private KeyboardInput keyboardInput;
    private ProgramSynthesisManager programSynthesisManager;
    private AnnotationManager annotationManager;
    private JSONToLLM jsonToLLM;
    #endregion

    #region Public Properties
    /// <summary>
    /// Current player explanation text
    /// </summary>
    public string explanation;
    
    /// <summary>
    /// Distance to ball in world units
    /// </summary>
    public float distToBall;
    
    /// <summary>
    /// Ball position projected to ground level
    /// </summary>
    public Vector3 ballOnTheGround;

    /// <summary>
    /// Whether player is currently moving
    /// </summary>
    public bool isMoving;
    
    /// <summary>
    /// Target position marker for UI
    /// </summary>
    public Vector3 xMark;
    
    /// <summary>
    /// Whether to trigger automatic pass to teammate
    /// </summary>
    public bool triggerPass;
    
    /// <summary>
    /// Whether player can kick ball (debounce mechanism)
    /// </summary>
    public bool canKickBall = true;
    
    /// <summary>
    /// Current player velocity vector
    /// </summary>
    public Vector3 velocity;
    
    /// <summary>
    /// Closest player in forward direction for passing
    /// </summary>
    public GameObject closestPlayerInDirection;
    
    /// <summary>
    /// Whether holding object in factory scenarios
    /// </summary>
    public bool objectPossession;
    
    /// <summary>
    /// Currently held object reference
    /// </summary>
    public GameObject grabbedObject;
    
    /// <summary>
    /// Current action being performed (for debugging)
    /// </summary>
    public string currAction = "No Action";
    
    /// <summary>
    /// Whether player is on allied team
    /// </summary>
    public bool ally = true;

    /// <summary>
    /// List of circle indicator objects
    /// </summary>
    public List<GameObject> circleObjects;
    
    /// <summary>
    /// List of arrow indicator objects
    /// </summary>
    public List<GameObject> arrowObjects;
    #endregion

    #region Component References
    BallOwnership ballOwnership;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        lastPosition = transform.position;
        if (isVR)
        {
            lastPosition = vrTransform.position;
        }
        
        InitializeComponents();
        SetupTeamColors();
        InitializeBallPossession();
        SetupUI();
        SetupVRCamera();
        HandlePlayerRegistration();
    }
    
    void Update()
    {
        UpdateBallTracking();
        UpdateVelocityCalculation();
        HandleBallPossessionVisuals();
        CalculateSmoothedVelocity();
        HandlePathfindingRadiusAdjustment();
    }
    
    private void LateUpdate()
    {
        // Reset AI target position if not actively moving to a position
        if (this.GetComponent<AIDestinationSetter>())
        {
            AIDestinationSetter dest = this.GetComponent<AIDestinationSetter>();
            if (currAction != "MoveToPos" && dest.target != null)
            {
                dest.target.localPosition = Vector3.zero;
            }
        }
    }
    #endregion

    #region Initialization Methods
    /// <summary>
    /// Initializes component references
    /// </summary>
    private void InitializeComponents()
    {
        exitScene = GetComponent<ExitScenario>();
        source = GetComponent<AudioSource>();
        tlManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();
        annotationManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<AnnotationManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        ballOwnership = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<BallOwnership>();
        
        circleObjects = new List<GameObject>();
        arrowObjects = new List<GameObject>();
    }

    /// <summary>
    /// Sets up team colors based on allegiance
    /// </summary>
    private void SetupTeamColors()
    {
        if (ally)
        {
            shirt.material.SetColor("_Color", Color.blue);
        }
    }

    /// <summary>
    /// Initializes ball possession state
    /// </summary>
    private void InitializeBallPossession()
    {
        ballPossession = false;
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("ball");
        }
    }

    /// <summary>
    /// Sets up UI elements and forward arrow
    /// </summary>
    private void SetupUI()
    {
        floatingNameText.SetText2(this.gameObject.name);
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            Vector3 pos = this.transform.position;
            Vector3 forward = transform.forward;
            if (isVR)
            {
                pos = vrTransform.position;
                pos.y = 0.1f;
                forward = vrTransform.forward;
                forward.y = 0;
                forward.Normalize();
            }
            forwardArrow = SpawnArrow(pos, forward * 8.5f);
            forwardArrow.GetComponentInChildren<ArrowGenerator>().RPC_SetActiveState(false);
        }
    }

    /// <summary>
    /// Configures VR camera settings for multiplayer
    /// </summary>
    private void SetupVRCamera()
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        Camera vrCam = GameObject.Find("CenterEyeAnchor")?.GetComponent<Camera>();
        
        if (!gm.isHost && vrCam != null)
        {
            vrCam.enabled = false;
        }
        
        // Configure info canvas for VR
        GameObject infoCanvas = GameObject.FindGameObjectWithTag("InfoCanvas");
        if (gm.isHost && infoCanvas != null && vrCam != null)
        {
            infoCanvas.GetComponent<Canvas>().worldCamera = Camera.main;
            infoCanvas.transform.SetParent(vrCam.gameObject.transform);
            
            RectTransform pausedText = GameObject.Find("Paused Text")?.GetComponent<RectTransform>();
            RectTransform recordingDot = GameObject.Find("Recording Dot")?.GetComponent<RectTransform>();
            
            if (pausedText != null)
                pausedText.anchoredPosition = new Vector3(pausedText.position.x, 100);
            
            if (recordingDot != null)
            {
                recordingDot.anchoredPosition = new Vector3(250, 200);
                recordingDot.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Handles player registration in object lists
    /// </summary>
    private void HandlePlayerRegistration()
    {
        if (objectList.humanPlayers.Count == 0 && !isViewer)
        {
            objectList.humanPlayers.Add(this.gameObject);
        }

        if (isViewer)
        {
            objectList.viewerPlayer = this.gameObject;
        }
    }
    #endregion

    #region Update Methods
    /// <summary>
    /// Updates ball tracking and distance calculations
    /// </summary>
    private void UpdateBallTracking()
    {
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("ball");
        }

        if (ball != null)
        {
            ballOnTheGround.x = ball.transform.position.x;
            ballOnTheGround.y = ball.transform.position.y;
            ballOnTheGround.z = ball.transform.position.z;
            Vector3 pos = transform.position;
            if (isVR)
            {
                pos = vrTransform.position;
            }
            distToBall = Vector3.Distance(pos, ballOnTheGround);
        }
    }

    /// <summary>
    /// Updates velocity calculation for movement tracking
    /// </summary>
    private void UpdateVelocityCalculation()
    {
        if (!isVR)
        {
            velocity = (transform.position - lastPosition) / Time.deltaTime;
            lastPosition = transform.position;
        }
        else
        {
            velocity = (vrTransform.position - lastPosition) / Time.deltaTime;
            lastPosition = vrTransform.position;
        }
    }

    /// <summary>
    /// Handles visual indicators for ball possession
    /// </summary>
    private void HandleBallPossessionVisuals()
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        
        if (ballPossession)
        {
            if (gm.isHost)
            {
                triggerPass = false;
                
                forwardArrow.GetComponentInChildren<ArrowGenerator>().RPC_SetActiveState(true);
                ArrowGenerator arrow = forwardArrow.GetComponentInChildren<ArrowGenerator>();
                Vector3 pos = transform.position;
                Vector3 forward = transform.forward;
                if (isVR)
                {
                    pos = vrTransform.position;
                    pos.y = 0.1f;
                    forward = vrTransform.forward;
                    forward.y = 0;
                    forward.Normalize();
                }
                arrow.SetOrigin(pos);
                arrow.SetTarget(pos + forward * 8.5f);
            }
        }
        else
        {
            if (gm.isHost)
            {
                forwardArrow.SetActive(false);
                forwardArrow.GetComponentInChildren<ArrowGenerator>().RPC_SetActiveState(false);
            }
        }
    }

    /// <summary>
    /// Adjusts pathfinding radius based on movement state
    /// </summary>
    private void HandlePathfindingRadiusAdjustment()
    {
        if (this.GetComponent<RichAI>() != null)
        {
            RichAI aiNav = this.GetComponent<RichAI>();
            if (aiNav.radius != 0.05f && isMoving)
            {
                aiNav.radius = 0.05f;
            }
            else if (aiNav.radius == 0.05f && !isMoving)
            {
                aiNav.radius = 0.5f;
            }
        }
    }
    #endregion

    #region Velocity Calculation
    /// <summary>
    /// Calculates smoothed velocity over multiple frames to reduce jitter
    /// </summary>
    private void CalculateSmoothedVelocity()
    {
        Vector3 currentPos = isVR ? vrTransform.position : transform.position;
        
        positionHistory.Enqueue(currentPos);
        timeHistory.Enqueue(Time.time);
        
        while (positionHistory.Count > maxHistoryFrames)
        {
            positionHistory.Dequeue();
            timeHistory.Dequeue();
        }
        
        if (positionHistory.Count >= 2)
        {
            Vector3[] positions = positionHistory.ToArray();
            float[] times = timeHistory.ToArray();
            
            Vector3 oldestPosition = positions[0];
            float oldestTime = times[0];
            Vector3 currentPosition = positions[positions.Length - 1];
            float currentTime = times[times.Length - 1];
            
            float deltaTime = currentTime - oldestTime;
            
            if (deltaTime > minDeltaTime)
            {
                velocity = (currentPosition - oldestPosition) / deltaTime;
            }
            else
            {
                if (velocity.magnitude < 0.01f)
                {
                    velocity = Vector3.zero;
                }
            }
        }
        else
        {
            velocity = Vector3.zero;
        }
    }
    #endregion

    #region Network Methods
    public override void Spawned() {
        if (Object.HasStateAuthority) {
            behavior = "Idle";
        }
    }
    
    private void UpdateGameObjectName()
    {
        gameObject.name = ObjName.ToString();
        floatingNameText.SetText2(this.gameObject.name);
    }
    
    public void SetObjectName(string newName)
    {
        ObjName = newName;
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_InstantiateValues(bool isAlly = false)
    {
        if (isAlly)
        {
            ally = true;
            ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
            objectList.humanPlayers.Add(this.gameObject);
        }
    }
    #endregion

    #region Ball Possession System
    /// <summary>
    /// Handles ball collision for automatic possession
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            if (other.CompareTag("ball") && canPossessBall && ballOwnership.heldByScenic == false && !ballPossession && ballOwnership.ballOwner == null)
            {
                LogReceiveBall();
                GainPossession(other.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Forcibly takes possession of ball from opponent
    /// </summary>
    public void ForciblyGainPossession()
    {
        if (ballOwnership.heldByScenic && canPossessBall && distToBall < 2f)
        {
            LogIntercept();
            ballOwnership.ballOwner.GetComponent<PlayerInterface>().LosePossession();
            GainPossession(ball);
        }
    }
    
    /// <summary>
    /// Takes possession of the ball and updates game state
    /// </summary>
    private void GainPossession(GameObject other)
    {
        int layerIgnoreBallCollision = LayerMask.NameToLayer("PlayerBall");
        this.gameObject.layer = layerIgnoreBallCollision;
        if (isVR && !isViewer)
        {
            vrCollider.layer = layerIgnoreBallCollision;
        }
        
        ball.transform.position = ballPosition.position;
        ball.transform.SetParent(ballPosition);
        ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        ballPossession = true;
        ballOwnership.SetScenicOwnership(false);
        ballOwnership.SetHumanOwnership(true);
        ballOwnership.SetBallOwner(this.gameObject);
        actionAPI.ReceiveBall(other.transform.position);
    }

    /// <summary>
    /// Releases ball possession
    /// </summary>
    public void LosePossession()
    {
        if (ball)
        {
            StartCoroutine(PossessionDebounce());
            ball.transform.SetParent(null);
            ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            ballPossession = false;
            ballOwnership.SetHumanOwnership(false);
            ballOwnership.SetBallOwner(null);
        }
    }

    /// <summary>
    /// Prevents immediate re-possession after losing ball
    /// </summary>
    private IEnumerator PossessionDebounce()
    {
        canPossessBall = false;
        yield return new WaitForSeconds(2.5f);
        canPossessBall = true;  
        this.gameObject.layer = LayerMask.NameToLayer("Default");
        if (isVR && !isViewer)
        {
            vrCollider.layer = LayerMask.NameToLayer("Default");
        }
    }
    
    /// <summary>
    /// Prevents rapid consecutive kicks
    /// </summary>
    public IEnumerator KickDebounce()
    {
        canKickBall = false;
        yield return new WaitForSeconds(2.5f);
        canKickBall = true;
    }
    #endregion

    #region Player Actions
    /// <summary>
    /// Passes ball to closest teammate in forward direction
    /// </summary>
    public void PassToPlayer()
    {
        if (!ballPossession || !canKickBall)
        {
            return;
        }

        closestPlayerInDirection = GetClosestToLinePoint(objectList.defensePlayers);
        if (closestPlayerInDirection == null)
        {
            return;
        }
        LogPass();
        actionAPI.GroundPassFast(closestPlayerInDirection.transform.position);
        StartCoroutine(ResetToMovementController());
        closestPlayerInDirection = null;
    }
    
    /// <summary>
    /// Shoots ball towards goal
    /// </summary>
    public void ShootGoal()
    {
        if (!ballPossession || !canKickBall)
        {
            return;
        }

        GameObject goalObj = objectList.goalObject; 
        if (goalObj == null)
        {
            return;
        }
        
        LogShootGoal();
        
        Vector3 goalPosition = goalObj.transform.position;
        Vector3 currentPosition = isVR ? vrTransform.position : transform.position;
        Vector3 targetPosition = goalPosition;
        
        actionAPI.GroundPassFast(targetPosition);
        StartCoroutine(ResetToMovementController());
    }
    
    /// <summary>
    /// Performs through pass in forward direction
    /// </summary>
    public void ThroughPass()
    {
        if (!ballPossession || !canKickBall)
        {
            return;
        }

        Vector3 pos = transform.position;
        Vector3 forward = transform.forward;
        if (isVR)
        {
            pos = vrTransform.position;
            pos.y = 0.1f;
            forward = vrTransform.forward;
            forward.y = 0;
            forward.Normalize();
        }
        
        Vector3 passPosition = pos + forward * 8.5f;
        LogThroughPass(passPosition);
        actionAPI.GroundPassFast(passPosition);
        StartCoroutine(ResetToMovementController());
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Finds closest player along forward ray for passing
    /// </summary>
    private GameObject GetClosestToLinePoint(List<GameObject> points) {
        Vector3 pos = transform.position;
        Vector3 forward = transform.forward;
        if (isVR)
        {
            pos = vrTransform.position;
            pos.y = 0.1f;
            forward = vrTransform.forward;
            forward.y = 0;
            forward.Normalize();
        }
        Ray ray = new Ray(pos, forward * 8.5f);
        
        GameObject closestPoint = null;
        float minDist = Mathf.Infinity;
        
        float maxDistanceFromRay = 1.0f;
        float maxForwardDotThreshold = 0.5f;
        
        foreach (var obj in points)
        {
            Vector3 toPlayer = obj.transform.position - pos;
            toPlayer.y = 0;

            float dot = Vector3.Dot(forward, toPlayer.normalized);
            if (dot < maxForwardDotThreshold) continue;

            Vector3 closestPointOnRay = GetClosestPointOnRay(ray, obj.transform.position);
            float distanceFromRay = Vector3.Distance(closestPointOnRay, obj.transform.position);

            if (distanceFromRay > maxDistanceFromRay) continue;

            float distanceAlongRay = Vector3.Distance(pos, closestPointOnRay);
            if (distanceAlongRay < minDist)
            {
                minDist = distanceAlongRay;
                closestPoint = obj;
            }
        }

        return closestPoint;
    }
    
    /// <summary>
    /// Calculates closest point on a ray to a given position
    /// </summary>
    private Vector3 GetClosestPointOnRay(Ray ray, Vector3 point)
    {
        Vector3 rayToPoint = point - ray.origin;
        float t = Vector3.Dot(rayToPoint, ray.direction);

        if (t <= 0)
        {
            return ray.origin;
        }
        else
        {
            return ray.origin + ray.direction * t;
        }
    }

    /// <summary>
    /// Resets animation controller after action completion
    /// </summary>
    private IEnumerator ResetToMovementController()
    {
        while (actionAPI.alreadyInAnimation)
        {
            yield return null;
        }
        yield return new WaitForSeconds(1.0f);
        actionAPI.RPC_SetAnimController("Movement");
    }

    /// <summary>
    /// Sets up pass trigger for AI teammates
    /// </summary>
    public IEnumerator SetTriggerPass(GameObject teammate)
    {
        triggerPass = true;
        LogTriggerPass(teammate);
        yield return new WaitForSeconds(0.1f);
        triggerPass = false;
    }

    /// <summary>
    /// Sets movement state with small delay for synchronization
    /// </summary>
    public IEnumerator SetIsMoving(bool isMoving)
    {
        yield return new WaitForSeconds(0.05f);
        this.isMoving = isMoving;
    }
    #endregion

    #region Object Spawning
    /// <summary>
    /// Spawns visual circle indicator at position
    /// </summary>
    public void SpawnCircle(Vector3 pos)
    {
        pos = new Vector3(pos.x, 2f, pos.z);
        GameObject circle = Instantiate(circleGenerator, pos, circleGenerator.transform.rotation);
        circleObjects.Add(circle);
        if (circleObjects.Count == 1)
        {
            circle.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
        }
        else if (circleObjects.Count > 1)
        {
            circle.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
        }
    }

    /// <summary>
    /// Spawns networked arrow indicator between two positions
    /// </summary>
    public GameObject SpawnArrow(Vector3 from, Vector3 to)
    {
        Vector3 spawnPos = new Vector3(from.x, from.y, from.z);
        NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
        NetworkObject temp = runner.Spawn(arrowGenerator, spawnPos, Quaternion.identity);
        GameObject arrow = temp.gameObject;
        arrow.GetComponentInChildren<ArrowGenerator>().SetOrigin(from);
        arrow.GetComponentInChildren<ArrowGenerator>().SetTarget(to);
        arrowObjects.Add(arrow);
        arrow.GetComponentInChildren<Renderer>().material.SetColor("_Color", new Color(1f, 0.92f, 0.016f, 0.25f));
        return arrow;
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Checks if source string contains all specified values
    /// </summary>
    public static bool ContainsAll(string source, params string[] values)
    {
        return values.All(x => source.Contains(x));
    }

    /// <summary>
    /// Plays audio clip once
    /// </summary>
    public void PlayAudioClip()
    {
        source.PlayOneShot(source.clip);
    }

    /// <summary>
    /// Sets transform position and rotation with physics handling
    /// </summary>
    public void SetTransform(Vector3 pos, Quaternion rot)
    {
        source.PlayOneShot(source.clip);
        this.GetComponent<Rigidbody>().isKinematic = false;
        this.transform.position = pos;
        this.transform.rotation = rot;

        if (isVR)
        {
            vrTransform.position = pos;
            vrTransform.rotation = rot;
        }
        
        this.GetComponent<Rigidbody>().isKinematic = true;

        Debug.LogWarning("Local: I am transforming to: " + pos.ToString());

        ResetHuman();
    }

    /// <summary>
    /// Sets position for specified game object
    /// </summary>
    public void SetTransform2(GameObject go, Vector3 pos)
    {
        source.PlayOneShot(source.clip);
        go.transform.position = pos;
    }

    /// <summary>
    /// Resets human player to default state
    /// </summary>
    public void ResetHuman()
    {
        LosePossession();
        triggerPass = false;
        isMoving = false;
        xMark = Vector3.zero;
        ballPossession = false;
        
        if (actionAPI)
        {
            actionAPI.alreadyInAnimation = false;
            actionAPI.RPC_SetAnimController("Movement");
        }

        if (forwardArrow)
        {
            forwardArrow.GetComponentInChildren<ArrowGenerator>().RPC_SetActiveState(false);
        }
        
        if (this.GetComponent<AIDestinationSetter>())
        {
            AIDestinationSetter dest = this.GetComponent<AIDestinationSetter>();
            dest.target.localPosition = Vector3.zero;
            actionAPI.stopMovement = true;
            
            this.GetComponent<Animator>().SetFloat("VelZ", 0);
            this.GetComponent<Animator>().SetFloat("VelX", 0);
        }
        
        localTick = 0;
    }

    /// <summary>
    /// Finds nearest grabbable object for factory scenarios
    /// </summary>
    private GameObject FindNearestObject()
    {
        GameObject[] grabbableObjects = GameObject.FindGameObjectsWithTag("Grabbable");
        Vector3 originPosition = this.gameObject.transform.position;

        return grabbableObjects
            .Where(obj => Vector3.Distance(obj.transform.position, originPosition) <= 5f)
            .OrderBy(obj => Vector3.Distance(obj.transform.position, originPosition))
            .FirstOrDefault();
    }
    #endregion

    #region Logging Methods
    private void LogIntercept()
    {
        annotationManager.CreateInterceptAnnotation(this.gameObject);
    }
    
    private void LogPass()
    {
        if (closestPlayerInDirection == null)
        {
            Debug.LogWarning("No target player found for pass.");
            return;
        }

        annotationManager.CreatePassAnnotation(this.gameObject, closestPlayerInDirection);
    }
    
    private void LogShootGoal()
    {
        GameObject goalObj = objectList.goalObject; 
        if (goalObj == null)
        {
            Debug.LogWarning("No goal found for pass.");
            return;
        }

        annotationManager.CreateShootGoalAnnotation(this.gameObject, goalObj);
    }
    
    private void LogThroughPass(Vector3 pos)
    {
        annotationManager.CreateThroughPassAnnotation(this.gameObject, pos);
    }
    
    private void LogReceiveBall()
    {
        annotationManager.CreateReceivePassAnnotation(this.gameObject);
    }

    private void LogTriggerPass(GameObject teammate)
    {
        annotationManager.CreateTriggerPassAnnotation(teammate);
    }
    #endregion

    #region IObjectInterface Implementation
    /// <summary>
    /// Applies movement data from Scenic simulation using reflection
    /// </summary>
    public void ApplyMovement(ScenicMovementData data)
    {
        localTick += 1;
        if (localTick < 4)
        {
            return;
        }
        
        // Update behavior text if not in animation
        if (!actionAPI.alreadyInAnimation)
        {
            if (data.behavior == " " || data.behavior == "" || data.behavior == "Idle")
            {
                behavior = "Idle";
                floatingBehaviorText.SetText("Idle");
            }
            else if (data.behavior != "" || data.behavior != null)
            {
                behavior = data.behavior;
                floatingBehaviorText.SetText(data.behavior);
            }
            else
            {
                behavior = "Idle";
                floatingBehaviorText.SetText("Idle");
            }
        }
        
        if (behavior == "Idle")
        {
            currAction = "No Action";
        }

        if (data.actionFunc != null)
        {
            currAction = data.actionFunc;
            if (data.actionFunc == "Speak")
            {
                explanation = data.actionArgs[0].ToString();
            }
            Type type = actionAPI.GetType();
            MethodInfo method = type.GetMethod(data.actionFunc);

            method.Invoke(actionAPI, data.actionArgs.ToArray());
        }
        else
        {
            actionAPI.stopMovement = true;
        }
    }
    #endregion
}