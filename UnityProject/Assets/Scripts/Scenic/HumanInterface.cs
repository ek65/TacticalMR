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
using OpenAI.Samples.Chat;
using Pathfinding;
using UnityEngine.InputSystem;

public class HumanInterface : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnNameChanged))] public NetworkString<_32> ObjName { get; set; }
    public bool isVR = false;

    public bool isViewer = false;
    
    private int localTick;  // NOTE: This is not the true tick and is what we will use to internally record a timestep.

    private ExitScenario exitScene;
    private AudioSource source;
    public ActionAPI actionAPI;
    public GameObject arrowGenerator;
    public GameObject circleGenerator;
    
    private TimelineManager tlManager;
    // private ConvaiNPC npc;
    private ChatBehaviour chatBehaviour;
    private ObjectsList objectList;

    private bool circleSpawned = false;
    private bool arrowSpawned = false;

    public List<GameObject> circleObjects;
    public List<GameObject> arrowObjects;

    public GameObject forwardArrow;
    // private GameObject circle1;
    // private GameObject arrow0;

    public string explanation;
    
    public float distToBall;
    public Vector3 ballOnTheGround;
    
    // public bool ballPossession;
    [Networked] public NetworkBool ballPossession { get; set; }
    public GameObject ball;
    public Transform ballPosition;
    private bool canPossessBall = true;
    public bool canKickBall = true;
    BallOwnership ballOwnership;
    public GameObject closestPlayerInDirection;
    
    private Vector3 lastPosition;
    public Vector3 velocity;
    
    public Transform vrTransform;
    
    public bool objectPossession;
    public GameObject grabbedObject;
    public Transform objectPosition;

    
    // public string behavior = "Idle";
    [Networked] public NetworkString<_32> behavior { get; set; }
    public string currAction = "No Action"; // just for debugging to see what actions function is being called
    private KeyboardInput keyboardInput;
    private JSONToLLM jsonToLLM;
    
    // TODO: RE-ADD, IMPLEMENT IsRobotScenario Bool in Scenic Manager, DISABLED FOR NOW FOR VR TESTING
    public FloatingText floatingBehaviorText;
    public FloatingText floatingNameText;
    
    public bool ally = true;
    public Renderer shirt;
    
    // Start is called before the first frame update
    void Start()
    {
        lastPosition = transform.position;
        if (isVR)
        {
            lastPosition = vrTransform.position;
        }
        exitScene = GetComponent<ExitScenario>();
        source = GetComponent<AudioSource>();
        tlManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        // npc = GameObject.FindGameObjectWithTag("Character").GetComponent<ConvaiNPC>();
        
        // TODO: RE-ADD, IMPLEMENT IsRobotScenario Bool in Scenic Manager, DISABLED FOR NOW FOR VR TESTING
        chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponentInChildren<ChatBehaviour>();
        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        circleObjects = new List<GameObject>();
        arrowObjects = new List<GameObject>();
        // SpawnCircle(this.transform.position); // spawn the circle around the coach
        ballOwnership = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<BallOwnership>();
        
        if (ally)
        {
            shirt.material.SetColor("_Color", Color.blue);
        }

        ballPossession = false;
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("ball");
        }
        
        // TODO: RE-ADD, IMPLEMENT IsRobotScenario Bool in Scenic Manager, DISABLED FOR NOW FOR VR TESTING
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
            // forwardArrow.SetActive(false);
        }
        
        if (objectList.humanPlayers.Count == 0 && !isViewer)
        {
            objectList.humanPlayers.Add(this.gameObject);
        }

        if (isViewer)
        {
            objectList.viewerPlayer = this.gameObject;
        }
        
        // make sure vr cam is not enabled for client
        Camera vrCam = GameObject.Find("CenterEyeAnchor").GetComponent<Camera>();
        if (!gm.isHost && vrCam != null)
        {
            vrCam.enabled = false;
        }
        
        // find gameobject with tag "InfoCanvas" and assign the canvas object to this object's camera
        GameObject infoCanvas = GameObject.FindGameObjectWithTag("InfoCanvas");
        if (gm.isHost && isVR && infoCanvas != null)
        {
            infoCanvas.GetComponent<Canvas>().worldCamera = Camera.main;

            // set the parent of the canvas to the vr camera and scale it down
            infoCanvas.transform.SetParent(vrCam.gameObject.transform);
            // infoCanvas.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            
            RectTransform t = GameObject.Find("Paused Text").GetComponent<RectTransform>();
            t.anchoredPosition = new Vector3(t.position.x, 100);
            
            RectTransform t2 = GameObject.Find("Recording Dot").GetComponent<RectTransform>();
            t2.anchoredPosition = new Vector3(300, 150);
        }
    }
    
    private void RPC_SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    // Update is called once per frame
    void Update()
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
        
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        
        // TODO: RE-ADD, IMPLEMENT IsRobotScenario Bool in Scenic Manager, DISABLED FOR NOW FOR VR TESTING
        // closestPlayerInDirection = GetClosestToLinePoint(objectList.defensePlayers);
        if (ballPossession)
        {
            if (gm.isHost)
            {
                forwardArrow.GetComponentInChildren<ArrowGenerator>().RPC_SetActiveState(true);
                // forwardArrow.SetActive(true);
                ArrowGenerator arrow = forwardArrow.GetComponentInChildren<ArrowGenerator>();
                Vector3 pos = transform.position;
                Vector3 forward = transform.forward;
                if (isVR)
                {
                    pos = vrTransform.position;
                    pos.y = 0.1f;
                    // Flatten the forward vector to only rotate around Y-axis
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
                // forwardArrow.SetActive(false);
                forwardArrow.GetComponentInChildren<ArrowGenerator>().RPC_SetActiveState(false);
            }
        }

        // string currResponse = "";
        // // if (chatBehaviour.sentences.Length > 0)
        // // {
        // //     currResponse = chatBehaviour.sentences[chatBehaviour.sentenceIndex];
        // // }
        // if (circleObjects.Count > 0 && circleObjects[0] != null)
        // {
        //     var temp = new Vector3(this.transform.position.x, 2f, this.transform.position.z);
        //     circleObjects[0].transform.position = temp;
        // }
        // // if (currResponse != null || currResponse != "")
        // // {
        // //     Debug.LogError("current response: " + currResponse);
        // // }
        // if (!circleSpawned && ContainsAll(currResponse, "closest", "opponent"))
        // {
        //     circleSpawned = true;
        //     if (objectList.scenicPlayers[0] != null)
        //     {
        //         GameObject closest = objectList.scenicPlayers[0]; // hardcoded closest opponent
        //         SpawnCircle(closest.transform.position); 
        //     }
        // } 
        // if (!arrowSpawned && ContainsAll(currResponse, "move in", "within a meter"))
        // {
        //     arrowSpawned = true;
        //     if (objectList.scenicPlayers[0] != null)
        //     {
        //         GameObject closest = objectList.scenicPlayers[0]; // hardcoded closest opponent
        //         SpawnArrow(this.transform.position, closest.transform.position);
        //     }
        // }
        // if (tlManager.Paused == false && (circleObjects.Count > 1 || arrowObjects.Count > 0))
        // {
        //     if (circleObjects.Count > 1)
        //     {
        //         for (int i = 1; i < circleObjects.Count; i++)
        //         {
        //             Destroy(circleObjects[i]);
        //         }
        //         circleObjects.RemoveRange(1, circleObjects.Count - 1);
        //     }
        //     if (arrowObjects.Count > 0)
        //     {
        //         for (int i = 0; i < arrowObjects.Count; i++)
        //         {
        //             Destroy(arrowObjects[i]);
        //         }
        //         arrowObjects.RemoveRange(0, arrowObjects.Count - 1);
        //     }
        //     circleSpawned = false;
        //     arrowSpawned = false;
        // }

    }
    
    public override void Spawned() {
        if (Object.HasStateAuthority) {
            behavior = "Idle";
        }
    }
    
    static void OnNameChanged(Changed<HumanInterface> changed)
    {
        changed.Behaviour.UpdateGameObjectName();
    }
    
    private void UpdateGameObjectName()
    {
        gameObject.name = ObjName.ToString();
        floatingNameText.SetText2(this.gameObject.name);
    }
    
    // static void OnBallPossessionChanged(Changed<HumanInterface> changed)
    // {
    //     changed.Behaviour.UpdateBallPossession();
    // }
    //
    // private void UpdateBallPossession()
    // {
    //     ballPossession = networkedBallPossession;
    // }
    
    public void SetObjectName(string newName)
    {
        if (Object.HasStateAuthority) // Only the host or owner should update this
        {
            ObjName = newName; // This will trigger OnNameChanged() on all clients
        }
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
    
    private void OnCollisionEnter(Collision other)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            if (other.collider.CompareTag("ball") && canPossessBall && ballOwnership.heldByScenic == false && !ballPossession)
            {
                GainPossession(other.gameObject);
            }
        }
    }
    
    public void ForciblyGainPossession()
    {
        if (ballOwnership.heldByScenic && canPossessBall && distToBall < 1f)
        {
            // Debug.LogError("forcibly get ball");
            LogIntercept();
            ballOwnership.ballOwner.GetComponent<PlayerInterface>().LosePossession();
            GainPossession(ball);
        }
    }

    private void LogIntercept()
    {
        int interceptID = keyboardInput.clickOrder;
        float interceptTime = jsonToLLM.time;
        
        keyboardInput.annotation.Add(keyboardInput.clickOrder, new Dictionary<string, string>
        {
            { "type", "Intercept" }
        });

        keyboardInput.annotationTimes.Add(interceptID, interceptTime);
        Debug.Log($"Intercept action recorded with ID {interceptID} at time: {interceptTime}");
        keyboardInput.clickOrder++; 
        // RPC_LogIntercept();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_LogIntercept()
    {
        int interceptID = keyboardInput.clickOrder;
        float interceptTime = jsonToLLM.time;
        
        keyboardInput.annotation.Add(keyboardInput.clickOrder, new Dictionary<string, string>
        {
            { "type", "Intercept" }
        });

        keyboardInput.annotationTimes.Add(interceptID, interceptTime);
        Debug.Log($"Intercept action recorded with ID {interceptID} at time: {interceptTime}");
        keyboardInput.clickOrder++; 
    }
    
    private void LogPass()
    {
        if (closestPlayerInDirection == null)
        {
            Debug.LogWarning("No target player found for pass.");
            return;
        }

        int passID = keyboardInput.clickOrder;
        float passTime = jsonToLLM.time;
    
        GameObject targetPlayer = closestPlayerInDirection;
        keyboardInput.annotation.Add(passID, new Dictionary<string, object>
        {
            { "type", "Pass" },
            { "from", this.name },
            { "to", targetPlayer.name }
        });

        keyboardInput.annotationDescriptions.Add(passID, $"({this.name} passed to {targetPlayer.name})");
        
        keyboardInput.annotationTimes.Add(passID, passTime);
        Debug.Log($"Pass action recorded with ID {passID}, from: {this.name} to: {targetPlayer.name} at time: {passTime}");
        keyboardInput.clickOrder++; 
        // RPC_LogPass();
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_LogPass()
    {
        if (closestPlayerInDirection == null)
        {
            Debug.LogWarning("No target player found for pass.");
            return;
        }

        int passID = keyboardInput.clickOrder;
        float passTime = jsonToLLM.time;
    
        GameObject targetPlayer = closestPlayerInDirection;
        keyboardInput.annotation.Add(passID, new Dictionary<string, object>
        {
            { "type", "Pass" },
            { "from", this.name },
            { "to", targetPlayer.name }
        });

        keyboardInput.annotationDescriptions.Add(passID, $"({this.name} passed to {targetPlayer.name})");
        
        keyboardInput.annotationTimes.Add(passID, passTime);
        Debug.Log($"Pass action recorded with ID {passID}, from: {this.name} to: {targetPlayer.name} at time: {passTime}");
        keyboardInput.clickOrder++; 
    }
    
    public void PickUp()
    {
        actionAPI.PickUp();
        Debug.Log("Picking up");
        LogPickUp();
    }

    public void PutDown()
    {
        actionAPI.PutDown(transform.position + transform.forward * 1f + Vector3.up * 1f);
        Debug.Log("Putting down");
        LogPutDown();
    }

    private void LogPickUp()
    {
        int eventID = keyboardInput.clickOrder;
        float eventTime = jsonToLLM.time;
        Debug.Log("test");
    
        keyboardInput.annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "PickUp" },
            { "player", this.name }
        });
        Debug.Log(keyboardInput.annotation);
    
        keyboardInput.annotationDescriptions.Add(eventID, $"({this.name} picked up an object)");
        Debug.Log($"Added pick up to annotations at {eventTime:F2}s, key {eventTime}");
        keyboardInput.annotationTimes.Add(eventID, eventTime);
        keyboardInput.clickOrder++;
    }

    private void LogPutDown()
    {
        int eventID = keyboardInput.clickOrder;
        float eventTime = jsonToLLM.time;
        Debug.Log("test put down");
    
        keyboardInput.annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "PutDown" },
            { "player", this.name }
        });
        Debug.Log(keyboardInput.annotation);
    
        keyboardInput.annotationDescriptions.Add(eventID, $"({this.name} put down an object)");
        keyboardInput.annotationTimes.Add(eventID, eventTime);
        keyboardInput.clickOrder++;
    }
    

    private void LogThroughPass(Vector3 pos)
    {
        int passID = keyboardInput.clickOrder;
        float passTime = jsonToLLM.time;

        var pointDict = new Dictionary<string, float>
        {
            { "x", pos.x },
            { "y", pos.z }
        };
        keyboardInput.annotation.Add(passID, new Dictionary<string, object>
        {
            { "type", "Through Pass" },
            { "from", this.name},
            { "to", pointDict }
        });
        
        keyboardInput.annotationDescriptions.Add(passID, $"({this.name} passed to position: {pointDict})");
    
        keyboardInput.annotationTimes.Add(passID, passTime);
        Debug.Log($"Through Pass action recorded with ID {passID} at time: {passTime}");
        keyboardInput.clickOrder++; 
        // RPC_LogThroughPass(pos);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_LogThroughPass(Vector3 pos)
    {
        int passID = keyboardInput.clickOrder;
        float passTime = jsonToLLM.time;

        var pointDict = new Dictionary<string, float>
        {
            { "x", pos.x },
            { "y", pos.z }
        };
        keyboardInput.annotation.Add(passID, new Dictionary<string, object>
        {
            { "type", "Through Pass" },
            { "from", this.name},
            { "to", pointDict }
        });
        
        keyboardInput.annotationDescriptions.Add(passID, $"({this.name} passed to position: {pointDict})");
    
        keyboardInput.annotationTimes.Add(passID, passTime);
        Debug.Log($"Through Pass action recorded with ID {passID} at time: {passTime}");
        keyboardInput.clickOrder++; 
    }
    
    public void Packaging()
    {
        actionAPI.Packaging();
        Debug.Log("Packaging action triggered.");
        LogPackaging();
    }

    private void LogPackaging()
    {
        int eventID = keyboardInput.clickOrder;
        float eventTime = jsonToLLM.time;
    
        GameObject targetObject = FindNearestObject();
        if (targetObject == null)
        {
            Debug.LogError("No object found for Packaging.");
            return;
        }

        keyboardInput.annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "Packaging" },
            { "player", this.name },
            { "object", targetObject.name }
        });

        keyboardInput.annotationDescriptions.Add(eventID, $"({this.name} packaged {targetObject.name})");

        keyboardInput.annotationTimes.Add(eventID, eventTime);
        keyboardInput.clickOrder++;
    }

    private GameObject FindNearestObject()
    {
        GameObject[] grabbableObjects = GameObject.FindGameObjectsWithTag("Grabbable");
        Vector3 originPosition = this.gameObject.transform.position;

        return grabbableObjects
            .Where(obj => Vector3.Distance(obj.transform.position, originPosition) <= 5f)
            .OrderBy(obj => Vector3.Distance(obj.transform.position, originPosition))
            .FirstOrDefault();
    }
    
    private void GainPossession(GameObject other)
    {
        LogReceiveBall();
        int layerIgnoreBallCollision = LayerMask.NameToLayer("PlayerBall");
        this.gameObject.layer = layerIgnoreBallCollision;
        
        ball.transform.position = ballPosition.position;
        ball.transform.SetParent(ballPosition);
        ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        ballPossession = true;
        ballOwnership.SetScenicOwnership(false);
        ballOwnership.SetHumanOwnership(true);
        ballOwnership.SetBallOwner(this.gameObject);
        actionAPI.ReceiveBall(other.transform.position);
    }
    
    private void LogReceiveBall()
    {
        int receiveBallID = keyboardInput.clickOrder;
        float receiveBallTime = jsonToLLM.time;
        keyboardInput.annotation.Add(receiveBallID, new Dictionary<string, object>
        {
            { "type", "ReceiveBall" },
            { "player", this.gameObject.name }
        });
        keyboardInput.annotationDescriptions.Add(receiveBallID, $"({this.gameObject.name} received the ball)");
        
        keyboardInput.annotationTimes.Add(receiveBallID, receiveBallTime);
        Debug.Log($"ReceiveBall action recorded with ID {receiveBallID} at time: {receiveBallTime}");
        
        keyboardInput.clickOrder++;
        // RPC_LogReceiveBall();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_LogReceiveBall()
    {
        int receiveBallID = keyboardInput.clickOrder;
        float receiveBallTime = jsonToLLM.time;
        keyboardInput.annotation.Add(receiveBallID, new Dictionary<string, object>
        {
            { "type", "ReceiveBall" },
            { "player", this.gameObject.name }
        });
        keyboardInput.annotationDescriptions.Add(receiveBallID, $"({this.gameObject.name} received the ball)");
        
        keyboardInput.annotationTimes.Add(receiveBallID, receiveBallTime);
        Debug.Log($"ReceiveBall action recorded with ID {receiveBallID} at time: {receiveBallTime}");
        
        keyboardInput.clickOrder++;
    }
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

    private IEnumerator ResetToMovementController()
    {
        while (actionAPI.alreadyInAnimation)
        {
            yield return null;
        }
        yield return new WaitForSeconds(1.0f);
        actionAPI.RPC_SetAnimController("Movement");
        // actionAPI.alreadyInAnimation = true;
        // yield return new WaitForSeconds(1.0f);
        // actionAPI.SetAnimController("Movement");
        // actionAPI.alreadyInAnimation = false;
    }


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
        
        float maxDistanceFromRay = 1.0f; // ← How far off the ray a player can be
        float maxForwardDotThreshold = 0.5f; // ← Between -1 (behind) to 1 (fully ahead)
        
        foreach (var obj in points)
        {
            Vector3 toPlayer = obj.transform.position - pos;
            toPlayer.y = 0;

            // Ignore if behind
            float dot = Vector3.Dot(forward, toPlayer.normalized);
            if (dot < maxForwardDotThreshold) continue;

            // Closest point on the ray
            Vector3 closestPointOnRay = GetClosestPointOnRay(ray, obj.transform.position);
            float distanceFromRay = Vector3.Distance(closestPointOnRay, obj.transform.position);

            // Ignore if too far from ray
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
    
    private IEnumerator PossessionDebounce()
    {
        canPossessBall = false;
        yield return new WaitForSeconds(1f);
        canPossessBall = true;
        this.gameObject.layer = LayerMask.NameToLayer("Default");
    }
    
    public IEnumerator KickDebounce()
    {
        canKickBall = false;
        yield return new WaitForSeconds(2.5f);
        canKickBall = true;
    }
    
    public static bool ContainsAll(string source, params string[] values)
    {
        // Debug.LogError("values: " + values[0]);
        return values.All(x => source.Contains(x));
    }

    public void PlayAudioClip()
    {
        source.PlayOneShot(source.clip);
    }
    
    public void SetTransform(Vector3 pos, Quaternion rot)
    {
        // Debug.LogError("In SetTransform: " + pos);
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

    public void SetTransform2(GameObject go, Vector3 pos)
    {
        source.PlayOneShot(source.clip);
        go.transform.position = pos;

    }

    public void ResetHuman()
    {
        LosePossession();
        ballPossession = false;
        
        if (actionAPI)
        {
            actionAPI.alreadyInAnimation = false;
            actionAPI.RPC_SetAnimController("Movement");
        }

        if (forwardArrow)
        {
            forwardArrow.GetComponentInChildren<ArrowGenerator>().RPC_SetActiveState(false);
            // forwardArrow.SetActive(false);
        }
        
        if (this.GetComponent<AIDestinationSetter>())
        {
            AIDestinationSetter dest = this.GetComponent<AIDestinationSetter>();
            RichAI aiNav = this.GetComponent<RichAI>();
            
            dest.target.localPosition = Vector3.zero;
            actionAPI.stopMovement = true;
            
            this.GetComponent<Animator>().SetFloat("VelZ", 0);
            this.GetComponent<Animator>().SetFloat("VelX", 0);
        }
        
        localTick = 0;
    }
    
    public void SpawnCircle(Vector3 pos)
    {
        pos = new Vector3(pos.x, 2f, pos.z);
        GameObject circle = Instantiate(circleGenerator, pos, circleGenerator.transform.rotation);
        circleObjects.Add(circle);
        if (circleObjects.Count == 1) // 0th circle should always be the one circling the coach
        {
            circle.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
        }
        else if (circleObjects.Count > 1)
        {
            circle.GetComponent<Renderer>().material.SetColor("_Color", Color.red);

        }
    }

    public GameObject SpawnArrow(Vector3 from, Vector3 to)
    {
        Vector3 spawnPos = new Vector3(from.x, from.y, from.z);
        // GameObject arrow = Instantiate(arrowGenerator, spawnPos, arrowGenerator.transform.rotation);
        NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
        NetworkObject temp = runner.Spawn(arrowGenerator, spawnPos, Quaternion.identity);
        GameObject arrow = temp.gameObject;
        arrow.GetComponentInChildren<ArrowGenerator>().SetOrigin(from);
        arrow.GetComponentInChildren<ArrowGenerator>().SetTarget(to);
        arrowObjects.Add(arrow);
        arrow.GetComponentInChildren<Renderer>().material.SetColor("_Color", new Color(1f, 0.92f, 0.016f, 0.25f));
        return arrow;
    }
    
    public void ApplyMovement(ScenicMovementData data)
    {
        localTick += 1;
        // Debug.Log(GetComponent<Rigidbody>().velocity);
        if (localTick < 4)
        {
            return;
        }
        
        // TODO: RE-ADD, IMPLEMENT IsRobotScenario Bool in Scenic Manager, DISABLED FOR NOW FOR VR TESTING
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
            if (data.actionFunc == "Speak")
            {
                explanation = data.actionArgs[0].ToString();
            }
            Type type = actionAPI.GetType();
            MethodInfo method = type.GetMethod(data.actionFunc);
            
            // Debug.LogError("im in here");

            method.Invoke(actionAPI, data.actionArgs.ToArray());
        }
        else //idle
        {
            // Debug.LogError("im in here2");
            actionAPI.stopMovement = true;
        }
    }
}
