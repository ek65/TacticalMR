using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using System.Linq;
using OpenAI.Samples.Chat;
using UnityEngine.InputSystem;

public class HumanInterface : MonoBehaviour
{
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
    
    public bool ballPossession;
    public GameObject ball;
    public Transform ballPosition;
    private bool canPossessBall = true;
    public bool canKickBall = true;
    BallOwnership ballOwnership;
    public GameObject closestPlayerInDirection;
    public bool objectPossession;
    public GameObject grabbedObject;
    public Transform objectPosition;

    
    public string behavior = "Idle";
    public string currAction = "No Action"; // just for debugging to see what actions function is being called
    private KeyboardInput keyboardInput;
    private JSONToLLM jsonToLLM;
    
    // TODO: RE-ADD, IMPLEMENT IsRobotScenario Bool in Scenic Manager, DISABLED FOR NOW FOR VR TESTING
    // public FloatingText floatingBehaviorText;
    // public FloatingText floatingNameText;
    
    public bool ally;
    public Renderer shirt;
    
    // Start is called before the first frame update
    void Start()
    {
        exitScene = GetComponent<ExitScenario>();
        source = GetComponent<AudioSource>();
        tlManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        // npc = GameObject.FindGameObjectWithTag("Character").GetComponent<ConvaiNPC>();
        
        // TODO: RE-ADD, IMPLEMENT IsRobotScenario Bool in Scenic Manager, DISABLED FOR NOW FOR VR TESTING
        // chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponentInChildren<ChatBehaviour>();
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
        // floatingNameText.SetText2(this.gameObject.name);
        // forwardArrow = SpawnArrow(this.transform.position, transform.forward * 8.5f);
        // forwardArrow.SetActive(false);
        
        if (objectList.humanPlayers.Count == 0)
        {
            objectList.humanPlayers.Add(this.gameObject);
        }
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
            distToBall = Vector3.Distance(transform.position, ballOnTheGround);
        }
        
        // TODO: RE-ADD, IMPLEMENT IsRobotScenario Bool in Scenic Manager, DISABLED FOR NOW FOR VR TESTING
        // closestPlayerInDirection = GetClosestToLinePoint(objectList.defensePlayers);
        // if (ballPossession)
        // {
        //     forwardArrow.SetActive(true);
        //     ArrowGenerator arrow = forwardArrow.GetComponentInChildren<ArrowGenerator>();
        //     arrow.SetOrigin(this.transform.position);
        //     arrow.SetTarget(transform.position + transform.forward * 8.5f);
        // }
        // else
        // {
        //     forwardArrow.SetActive(false);
        // }

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
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("ball") && canPossessBall && ballOwnership.heldByScenic == false)
        {
            GainPossession(other.gameObject);
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
    }
    
    
    public void PickUp()
    {
        actionAPI.PickUp();
        Debug.Log("Picking up");
    }

    public void PutDown()
    {
        if (!objectPossession)
        {
            return;
        }
        actionAPI.PutDown(transform.position + transform.forward * 1f + Vector3.up * 1f);
        Debug.Log("Putting down");
    }

    public void Packaging()
    {
        actionAPI.Packaging();
        Debug.Log("Packaging action triggered.");
    }
    
    public void RaiseHand()
    {
        actionAPI.RaiseHand();
        Debug.Log("Raising hand");
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
    
    public void LosePossession()
    {
        StartCoroutine(PossessionDebounce());
        ball.transform.SetParent(null);
        ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        ballPossession = false;
        ballOwnership.SetHumanOwnership(false);
        ballOwnership.SetBallOwner(null);
    }

    public void PassToPlayer()
    {
        if (!ballPossession || !canKickBall)
        {
            return;
        }

        closestPlayerInDirection = GetClosestToLinePoint(objectList.defensePlayers);
        LogPass();
        actionAPI.GroundPassFast(closestPlayerInDirection.transform.position);
        StartCoroutine(ResetToMovementController());
    }
    
    public void ThroughPass()
    {
        if (!ballPossession || !canKickBall)
        {
            return;
        }

        Vector3 passPosition = transform.position + transform.forward * 8.5f;
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
        actionAPI.SetAnimController("Movement");
        // actionAPI.alreadyInAnimation = true;
        // yield return new WaitForSeconds(1.0f);
        // actionAPI.SetAnimController("Movement");
        // actionAPI.alreadyInAnimation = false;
    }


    private GameObject GetClosestToLinePoint(List<GameObject> points) {
        Ray ray = new Ray(transform.position, transform.forward * 8.5f);
        
        GameObject closestPoint = null;
        float minDist = Mathf.Infinity;
        
        foreach (var obj in points)
        {
            Vector3 closestPointOnRay = GetClosestPointOnRay(ray, obj.transform.position);
            float distance = Vector3.Distance(closestPointOnRay, obj.transform.position);

            if (distance < minDist)
            {
                minDist = distance;
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
    
    public void SetTransform(Vector3 pos)
    {
        Debug.LogError("In SetTransform: " + pos);
        source.PlayOneShot(source.clip);
        this.transform.position = pos;
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
        actionAPI.alreadyInAnimation = false;
        forwardArrow.SetActive(false);
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
        GameObject arrow = Instantiate(arrowGenerator, spawnPos, arrowGenerator.transform.rotation);
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
        // // if player is already in kick animation, dont update behavior text yet
        // if (!actionAPI.alreadyInAnimation)
        // {
        //     if (data.behavior == " " || data.behavior == "" || data.behavior == "Idle")
        //     {
        //         behavior = "Idle";
        //         floatingBehaviorText.SetText("Idle");
        //     }
        //     else if (data.behavior != "" || data.behavior != null)
        //     {
        //         behavior = data.behavior;
        //         floatingBehaviorText.SetText(data.behavior);
        //     }
        //     else
        //     {
        //         behavior = "Idle";
        //         floatingBehaviorText.SetText("Idle");
        //     }
        // }
        
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
