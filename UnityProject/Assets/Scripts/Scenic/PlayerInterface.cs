using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using Fusion;
using Pathfinding;

/// <summary>
/// Interface for AI-controlled player objects in the soccer simulation.
/// Handles ball possession, movement, team allegiance, and communication with Scenic simulation system.
/// Implements IObjectInterface to receive movement commands from the simulation engine.
/// </summary>
public class PlayerInterface : NetworkBehaviour, IObjectInterface
{
    #region Network Properties
    [Networked, OnChangedRender(nameof(UpdateGameObjectName))]
    public NetworkString<_32> ObjName { get; set; }
    
    [Networked] public NetworkBool ballPossession { get; set; }
    
    [Networked] public NetworkBool handRaised { get; set; }
    
    [Networked] public Vector3 currVelocity { get; set; }
    
    [Networked] public NetworkString<_32> behavior { get; set; }
    #endregion

    #region Team Allegiance Properties
    /// <summary>
    /// Whether this player is on the opposing team
    /// </summary>
    public bool enemy;
    
    /// <summary>
    /// Whether this player is on the allied team
    /// </summary>
    public bool ally;
    
    /// <summary>
    /// Whether this player represents the human player
    /// </summary>
    public bool self;
    
    /// <summary>
    /// Renderer for team shirt colors
    /// </summary>
    public Renderer shirt;
    #endregion

    #region Game Object References
    /// <summary>
    /// Reference to the soccer ball
    /// </summary>
    public GameObject ball;
    
    /// <summary>
    /// Reference to the goal object
    /// </summary>
    public GameObject goal;
    
    /// <summary>
    /// Position where ball attaches when possessed
    /// </summary>
    public Transform ballPosition;
    
    /// <summary>
    /// Reference to ActionAPI for executing actions
    /// </summary>
    public ActionAPI actionAPI;
    
    /// <summary>
    /// UI text for displaying current behavior
    /// </summary>
    public FloatingText floatingBehaviorText;
    
    /// <summary>
    /// UI text for displaying player name
    /// </summary>
    public FloatingText floatingNameText;
    #endregion

    #region Robot/Factory Properties
    [Space(10)] 
    [Header("For robot only")]
    
    /// <summary>
    /// Whether this is a robot player (for factory scenarios)
    /// </summary>
    public bool isRobot;
    
    /// <summary>
    /// Position where objects are held
    /// </summary>
    public Transform objectPosition;
    
    /// <summary>
    /// Whether currently holding an object
    /// </summary>
    public bool objectPossession;
    
    /// <summary>
    /// Reference to currently held object
    /// </summary>
    public GameObject grabbedObject;
    #endregion

    #region Game State Properties
    /// <summary>
    /// Player movement speed
    /// </summary>
    public float speed;
    
    /// <summary>
    /// Ball position projected to ground level
    /// </summary>
    public Vector3 ballOnTheGround;
    
    /// <summary>
    /// Target position for movement
    /// </summary>
    public Vector3 targetPosition;
    
    /// <summary>
    /// Force applied to ball
    /// </summary>
    public float force;
    
    /// <summary>
    /// Distance to ball in world units
    /// </summary>
    public float distToBall;
    
    /// <summary>
    /// Whether player is currently moving
    /// </summary>
    public bool isMoving;
    
    /// <summary>
    /// Whether player can kick ball (debounce system)
    /// </summary>
    public bool canKickBall = true;
    
    /// <summary>
    /// Current action being performed (for debugging)
    /// </summary>
    public string currAction = "No Action";
    #endregion

    #region Private Fields
    private bool canPossessBall = true;
    private int localTick;
    private KeyboardInput keyboardInput;
    private JSONToLLM jsonToLLM;
    private RichAI richAI;
    private ProgramSynthesisManager programSynthesisManager;
    private AnnotationManager annotationManager;
    #endregion

    #region Component References
    BallOwnership ballOwnership;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeTeamColors();
        InitializeReferences();
        RegisterPlayer();
        
        localTick = -1;
        ballPossession = false;
        
        floatingNameText.SetText2(this.gameObject.name);
    }

    void Update()
    {
        UpdateReferences();
        UpdateBallTracking();
        HandleMovementStop();
        HandlePathfindingRadius();
    }
    
    private void LateUpdate()
    {
        // Reset AI destination if not actively moving to position
        // if (this.GetComponent<AIDestinationSetter>())
        // {
        //     AIDestinationSetter dest = this.GetComponent<AIDestinationSetter>();
        //     if (currAction != "MoveToPos" && dest.target != null)
        //     {
        //         dest.target.localPosition = Vector3.zero;
        //     }
        // }
    }
    #endregion

    #region Initialization Methods
    /// <summary>
    /// Sets up team colors based on allegiance
    /// </summary>
    private void InitializeTeamColors()
    {
        if (enemy)
        {
            shirt.material.SetColor("_Color", Color.red);
        }
        if (ally)
        {
            shirt.material.SetColor("_Color", Color.blue);
        }
        if (self)
        {
            shirt.material.SetColor("_Color", Color.yellow);
        }
    }

    /// <summary>
    /// Initializes component references
    /// </summary>
    private void InitializeReferences()
    {
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("ball");
        }
        if (goal == null)
        {
            goal = GameObject.FindGameObjectWithTag("goal");
        }
        
        ballOwnership = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<BallOwnership>();
        programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();
        annotationManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<AnnotationManager>();
    }

    /// <summary>
    /// Registers player in the object list
    /// </summary>
    private void RegisterPlayer()
    {
        ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        if (this.gameObject != null && !objectList.scenicPlayers.Contains(this.gameObject))
        {
            objectList.scenicPlayers.Add(this.gameObject);
        }
        
    }
    #endregion

    #region Update Methods
    /// <summary>
    /// Updates component references if they become null
    /// </summary>
    private void UpdateReferences()
    {
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("ball");
        }
        if (goal == null)
        {
            goal = GameObject.FindGameObjectWithTag("goal");
        }
    }

    /// <summary>
    /// Updates ball tracking and distance calculations
    /// </summary>
    private void UpdateBallTracking()
    {
        if (ball)
        {
            ballOnTheGround.x = ball.transform.position.x;
            ballOnTheGround.y = ball.transform.position.y;
            ballOnTheGround.z = ball.transform.position.z;
            distToBall = Vector3.Distance(transform.position, ballOnTheGround);
        }
    }

    /// <summary>
    /// Handles movement stop requests
    /// </summary>
    private void HandleMovementStop()
    {
        if (actionAPI.stopMovement == true)
        {
            actionAPI.Idle();
        }
    }

    /// <summary>
    /// Adjusts pathfinding radius based on movement state
    /// </summary>
    private void HandlePathfindingRadius()
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

    #region Network Methods
    public override void Spawned() {
        if (Object.HasStateAuthority) {
            behavior = "Idle";
            richAI = GetComponent<RichAI>();
        }
    }
    
    public override void FixedUpdateNetwork() {
        // Update velocity on state-authoritative instance only
        if (Object.HasStateAuthority && richAI != null) {
            currVelocity = richAI.velocity;
        }
    }
    
    private void UpdateGameObjectName()
    {
        gameObject.name = ObjName.ToString();
    }
    
    public void SetObjectName(string newName)
    {
        ObjName = newName;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_InstantiateValues(bool isAlly = false)
    {
        if (ScenarioTypeManager.ScenarioType.FactoryScenarioCreation == ScenarioTypeManager.Instance.currentScenario)
        {
            ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
            objectList.defensePlayers.Add(this.gameObject);
        }
        else if (ScenarioTypeManager.ScenarioType.Soccer == ScenarioTypeManager.Instance.currentScenario)
        {
            if (isAlly)
            {
                ally = true;
                enemy = false;
            
                shirt.material.SetColor("_Color", Color.blue);
            
                ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
                objectList.defensePlayers.Add(this.gameObject);
            }
            else
            {
                ally = false;
                enemy = true;
            
                shirt.material.SetColor("_Color", Color.red);
            
                ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
                objectList.offensePlayers.Add(this.gameObject);
            }
        }
    }
    #endregion

    #region Ball Possession System
    /// <summary>
    /// Handles collision detection for ball possession
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            if (other.CompareTag("ball") && canPossessBall && !ballPossession && ballOwnership.ballOwner == null)
            {
                LogReceiveBall();
                GainPossession(other.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Takes possession of the ball
    /// </summary>
    private void GainPossession(GameObject other)
    {
        int layerIgnoreBallCollision = LayerMask.NameToLayer("PlayerBall");
        this.gameObject.layer = layerIgnoreBallCollision;
        
        ball.transform.position = ballPosition.position;
        ball.transform.SetParent(ballPosition);
        ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        ballPossession = true;
        ballOwnership.SetScenicOwnership(true);
        ballOwnership.SetHumanOwnership(false);
        ballOwnership.SetBallOwner(this.gameObject);
        actionAPI.ReceiveBall(other.transform.position);
    }
    
    /// <summary>
    /// Forcibly takes possession from another player
    /// </summary>
    public void ForciblyGainPossession()
    {
        if (ballOwnership.heldByScenic && canPossessBall && distToBall < 2f)
        {
            Debug.LogError(distToBall);
            LogIntercept();
            if (ballOwnership.ballOwner.GetComponent<PlayerInterface>())
            {
                ballOwnership.ballOwner.GetComponent<PlayerInterface>().LosePossession();
            } else if (ballOwnership.ballOwner.GetComponent<HumanInterface>())
            {
                ballOwnership.ballOwner.GetComponent<HumanInterface>().LosePossession();
            }
            GainPossession(ball);
        }
    }
    
    /// <summary>
    /// Releases ball possession
    /// </summary>
    public void LosePossession()
    {
        StartCoroutine(PossessionDebounce());
        ball.transform.SetParent(null);
        ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        ballPossession = false;
        ballOwnership.SetScenicOwnership(false);
        ballOwnership.SetBallOwner(null);
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

    #region Utility Methods
    /// <summary>
    /// Sets movement state with synchronization delay
    /// </summary>
    public IEnumerator SetIsMoving(bool isMoving)
    {
        yield return new WaitForSeconds(0.05f);
        this.isMoving = isMoving;
    }
    #endregion

    #region Logging Methods
    /// <summary>
    /// Logs interception action for AI analysis
    /// </summary>
    private void LogIntercept()
    {
        annotationManager.CreateInterceptAnnotation(this.gameObject);
    }
    
    /// <summary>
    /// Logs ball reception for AI analysis
    /// </summary>
    private void LogReceiveBall()
    {
        annotationManager.CreateReceivePassAnnotation(this.gameObject);
    }
    #endregion

    #region IObjectInterface Implementation
    /// <summary>
    /// Applies movement data from Scenic simulation using reflection to invoke action methods.
    /// Handles behavior updates and animation state management.
    /// </summary>
    /// <param name="data">Movement data containing action function, arguments, and behavior state</param>
    public void ApplyMovement(ScenicMovementData data)
    {
        localTick += 1;
        // Skip first few ticks for initialization
        if (localTick < 4)
        {
            return;
        }
        
        // Update behavior text if not currently in animation
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
            
            // Skip kick actions if recently kicked (debounce)
            if (currAction == "GroundPassFast" || currAction == "Shoot")
            {
                if (!canKickBall)
                {
                    return;
                }
            }
            
            // Use reflection to invoke the specified action method
            Type type = actionAPI.GetType();
            MethodInfo method = type.GetMethod(data.actionFunc);
            method.Invoke(actionAPI, data.actionArgs.ToArray());
        }
        else // Default to idle state
        {
            actionAPI.stopMovement = true;
        }
    }
    #endregion
}