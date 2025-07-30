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
using Pathfinding;
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

    public bool isMoving;
    public Vector3 xMark;
    public bool triggerPass;
    public bool ballPossession;
    public GameObject ball;
    public Transform ballPosition;
    private bool canPossessBall = true;
    public bool canKickBall = true;
    BallOwnership ballOwnership;
    public GameObject closestPlayerInDirection;
    
    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    private Queue<float> timeHistory = new Queue<float>();
    private int maxHistoryFrames = 5; // Average over 5 frames
    private float minDeltaTime = 0.01f; // Minimum deltaTime to consider
    private Vector3 lastPosition;
    public Vector3 velocity;
    
    public string behavior = "Idle";
    public string currAction = "No Action"; // just for debugging to see what actions function is being called
    private KeyboardInput keyboardInput;
    private JSONToLLM jsonToLLM;
    
    public FloatingText floatingBehaviorText;
    public FloatingText floatingNameText;
    
    public bool ally;
    public Renderer shirt;
    
    // Start is called before the first frame update
    void Start()
    {
        exitScene = GetComponent<ExitScenario>();
        source = GetComponent<AudioSource>();
        tlManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        // npc = GameObject.FindGameObjectWithTag("Character").GetComponent<ConvaiNPC>();
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
        
        floatingNameText.SetText2(this.gameObject.name);
        
        forwardArrow = SpawnArrow(this.transform.position, transform.forward * 8.5f);
        forwardArrow.SetActive(false);
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
        
        closestPlayerInDirection = GetClosestToLinePoint(objectList.defensePlayers);

        if (ballPossession)
        {
            // make sure teammate does not pass when human has ball
            triggerPass = false;
            forwardArrow.SetActive(true);
            ArrowGenerator arrow = forwardArrow.GetComponentInChildren<ArrowGenerator>();
            arrow.SetOrigin(this.transform.position);
            arrow.SetTarget(transform.position + transform.forward * 8.5f);
        }
        else
        {
            forwardArrow.SetActive(false);
        }

        CalculateSmoothedVelocity();

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
    
    private void CalculateSmoothedVelocity()
    {
        // Add current position and time to history
        positionHistory.Enqueue(transform.position);
        timeHistory.Enqueue(Time.time);
        
        // Remove old entries if we exceed max frames
        while (positionHistory.Count > maxHistoryFrames)
        {
            positionHistory.Dequeue();
            timeHistory.Dequeue();
        }
        
        // Calculate velocity if we have enough history
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
                // If deltaTime is too small, keep previous velocity or set to zero
                if (velocity.magnitude < 0.01f) // Very small velocity threshold
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
    
    // triggerPass should be disabled after it is set true
    public IEnumerator SetTriggerPass(GameObject teammate)
    {
        triggerPass = true;
        LogTriggerPass(teammate);
        yield return new WaitForSeconds(0.1f);
        triggerPass = false;
    }
    
    private void LogTriggerPass(GameObject teammate)
    {
        int eventID = keyboardInput.clickOrder;
        float eventTime = jsonToLLM.time;
    
        keyboardInput.annotation.Add(eventID, new Dictionary<string, object>
        {
            { "type", "TriggerPass" },
            { "from", teammate.name }
        });
        Debug.Log(keyboardInput.annotation);
    
        keyboardInput.annotationDescriptions.Add(eventID, $"(Coach told {teammate.name} to pass the ball)");
        Debug.Log($"Added trigger pass to annotations at {eventTime:F2}s, key {eventTime}");
        keyboardInput.annotationTimes.Add(eventID, eventTime);
        keyboardInput.clickOrder++;
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("ball") && canPossessBall && ballOwnership.heldByScenic == false && !ballPossession)
        {
            LogReceiveBall();
            GainPossession(other.gameObject);
        }
    }
    
    public void ForciblyGainPossession()
    {
        if (ballOwnership.heldByScenic && canPossessBall && distToBall < 1.5f)
        {
            // Debug.LogError("forcibly get ball");
            LogIntercept();
            ballOwnership.ballOwner.GetComponent<PlayerInterface>().LosePossession();
            GainPossession(ball);
        }
    }
    
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
        Vector3 currentPosition = transform.position;
        Vector3 direction = goalPosition - currentPosition;
        float currentDistance = direction.magnitude;
        
        Vector3 targetPosition;
        
        // If the distance is greater than 8.5, clamp it
        if (currentDistance > 8.5f)
        {
            // Normalize the direction and multiply by max distance
            direction = direction.normalized;
            targetPosition = currentPosition + direction * 8.5f;
        }
        else
        {
            // Use the original goal position if it's within range
            targetPosition = goalPosition;
        }
        
        actionAPI.GroundPassFast(targetPosition);
        StartCoroutine(ResetToMovementController());
    }

    private void LogIntercept()
    {
        int interceptID = keyboardInput.clickOrder;
        float interceptTime = jsonToLLM.time;
        
        keyboardInput.annotation.Add(keyboardInput.clickOrder, new Dictionary<string, string>
        {
            { "type", "Intercept" },
            { "player", this.name }
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
    
    private void LogShootGoal()
    {
        GameObject goalObj = objectList.goalObject; 
        if (goalObj == null)
        {
            Debug.LogWarning("No goal found for pass.");
            return;
        }

        int passID = keyboardInput.clickOrder;
        float passTime = jsonToLLM.time;
    
        keyboardInput.annotation.Add(passID, new Dictionary<string, object>
        {
            { "type", "Shoot Goal" },
            { "from", this.name },
            { "to", goalObj.name }
        });

        keyboardInput.annotationDescriptions.Add(passID, $"({this.name} shot towards Goal)");
        
        keyboardInput.annotationTimes.Add(passID, passTime);
        Debug.Log($"Shoot goal action recorded with ID {passID}, from: {this.name} at time: {passTime}");
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
    }

    
    public void LosePossession()
    {
        if (!ball)
        {
            return;
        }
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
    
    public IEnumerator SetIsMovingTrue()
    {
        yield return new WaitForSeconds(0.5f);
        isMoving = true;
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
        triggerPass = false;
        isMoving = false;
        xMark = Vector3.zero;
        ballPossession = false;
        actionAPI.alreadyInAnimation = false;
        actionAPI.SetAnimController("Movement");
        forwardArrow.SetActive(false);

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
