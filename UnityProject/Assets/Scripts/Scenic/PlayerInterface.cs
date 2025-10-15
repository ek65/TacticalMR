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

// TODO: Rename script, this is the player logic script
public class PlayerInterface : NetworkBehaviour, IObjectInterface
{
    [Networked, OnChangedRender(nameof(UpdateGameObjectName))]
    public NetworkString<_32> ObjName { get; set; }
    
    public bool enemy;
    public bool ally;
    public bool self;
    public Renderer shirt;
    public float speed;
    public GameObject ball;
    public Vector3 ballOnTheGround;
    public Vector3 targetPosition;
    public float force;
    public float distToBall;
    public GameObject goal;
    public bool isMoving;
    // public bool ballPossession;
    [Networked] public NetworkBool ballPossession { get; set; }
    public Transform ballPosition;
    private KeyboardInput keyboardInput;
    private JSONToLLM jsonToLLM;

    // public Vector3 currVelocity => this.GetComponent<RichAI>().velocity;
    [Networked] public Vector3 currVelocity { get; set; }
    private RichAI richAI;

    private bool canPossessBall = true;
    public bool canKickBall = true;
    BallOwnership ballOwnership;
    
    private int localTick;  // NOTE: This is not the true tick and is what we will use to internally record a timestep.

    public ActionAPI actionAPI;
    public FloatingText floatingBehaviorText;
    public FloatingText floatingNameText;
    // public string behavior = "Idle";
    [Networked] public NetworkString<_32> behavior { get; set; }
    public string currAction = "No Action"; // just for debugging to see what actions function is being called
    
    [Space(10)] 
    [Header("For robot only")]
    
    public bool isRobot;
    public Transform objectPosition;
    public bool objectPossession;
    public GameObject grabbedObject;
    
    private ProgramSynthesisManager programSynthesisManager;
    private AnnotationManager annotationManager;
    
    private void Start()
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
        localTick = -1;
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("ball");
        }
        if (goal == null)
        {
            goal = GameObject.FindGameObjectWithTag("goal");
        }

        ballPossession = false;
        
        ballOwnership = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<BallOwnership>();
        
        floatingNameText.SetText2(this.gameObject.name);
        
        ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        objectList.scenicPlayers.Add(this.gameObject);
        
        programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();
        annotationManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<AnnotationManager>();
    }

    // Update is called once per frame
    void Update()
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

        if (ball)
        {
            ballOnTheGround.x = ball.transform.position.x;
            ballOnTheGround.y = ball.transform.position.y;
            ballOnTheGround.z = ball.transform.position.z;
            distToBall = Vector3.Distance(transform.position, ballOnTheGround);
        }

        if (actionAPI.stopMovement == true)
        {
            actionAPI.Idle();
        }
        
        // Set RichAI radius to .1 when moving to avoid weird pathing behavior
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
    
    private void LateUpdate()
    {
        // make sure ai target position is set back to 0 if behavior is not "MoveTo"
        if (this.GetComponent<AIDestinationSetter>())
        {
            AIDestinationSetter dest = this.GetComponent<AIDestinationSetter>();
            if (currAction != "MoveToPos" && dest.target != null)
            {
                dest.target.localPosition = Vector3.zero;
            }
        }
    }
    
    public override void Spawned() {
        if (Object.HasStateAuthority) {
            behavior = "Idle";
            richAI = GetComponent<RichAI>();
        }
    }
    
    public override void FixedUpdateNetwork() {
        // Only update the velocity on the state-authoritative instance.
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
        ObjName = newName; // This will trigger OnNameChanged() on all clients
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_InstantiateValues(bool isAlly = false)
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
    
    // For ball possession
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
    
    private void LogIntercept()
    {
        annotationManager.CreateInterceptAnnotation(this.gameObject);
    }
    
    private void LogReceiveBall()
    {
        annotationManager.CreateReceivePassAnnotation(this.gameObject);
    }
    
    public void LosePossession()
    {
        StartCoroutine(PossessionDebounce());
        ball.transform.SetParent(null);
        ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        ballPossession = false;
        ballOwnership.SetScenicOwnership(false);
        ballOwnership.SetBallOwner(null);
    }
    
    private IEnumerator PossessionDebounce()
    {
        canPossessBall = false;
        yield return new WaitForSeconds(2.5f);
        canPossessBall = true;
        this.gameObject.layer = LayerMask.NameToLayer("Default");
    }
    
    public IEnumerator KickDebounce()
    {
        canKickBall = false;
        yield return new WaitForSeconds(2.5f);
        canKickBall = true;
    }

    public IEnumerator SetIsMoving(bool isMoving)
    {
        yield return new WaitForSeconds(0.05f);
        this.isMoving = isMoving;
    }
    
    public void ApplyMovement(ScenicMovementData data)
    {
        localTick += 1;
        // Debug.Log(GetComponent<Rigidbody>().velocity);
        if (localTick < 4)
        {
            return;
        }
        
        // if player is already in kick animation, dont update behavior text yet
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
            
            // check for if player already kicked recently, skip this action
            if (currAction == "GroundPassFast" || currAction == "Shoot")
            {
                if (!canKickBall)
                {
                    return;
                }
            }
            
            Type type = actionAPI.GetType();
            MethodInfo method = type.GetMethod(data.actionFunc);
            // Debug.Log("here12");
            // Debug.Log(data.actionFunc);
            // Debug.Log(data.actionArgs.ToArray().Length);
            // foreach (var v in data.actionArgs.ToArray())
            // {
            //     Debug.Log(v);
            // }
            method.Invoke(actionAPI, data.actionArgs.ToArray());
        }
        else //idle
        {
            actionAPI.stopMovement = true;
        }
    }
}
