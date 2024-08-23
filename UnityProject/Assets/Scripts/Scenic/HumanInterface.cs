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

public class HumanInterface : MonoBehaviour
{
    public Vector3 pointDebug;
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
    public bool inAnimation = false;
    
    public string behavior = "Idle";
    public string currAction = "No Action"; // just for debugging to see what actions function is being called

    
    public FloatingText floatingText;
    
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
        
        string currResponse = "";
        // if (chatBehaviour.sentences.Length > 0)
        // {
        //     currResponse = chatBehaviour.sentences[chatBehaviour.sentenceIndex];
        // }
        if (circleObjects.Count > 0 && circleObjects[0] != null)
        {
            var temp = new Vector3(this.transform.position.x, 2f, this.transform.position.z);
            circleObjects[0].transform.position = temp;
        }
        // if (currResponse != null || currResponse != "")
        // {
        //     Debug.LogError("current response: " + currResponse);
        // }
        if (!circleSpawned && ContainsAll(currResponse, "closest", "opponent"))
        {
            circleSpawned = true;
            if (objectList.scenicPlayers[0] != null)
            {
                GameObject closest = objectList.scenicPlayers[0]; // hardcoded closest opponent
                SpawnCircle(closest.transform.position); 
            }
        } 
        if (!arrowSpawned && ContainsAll(currResponse, "move in", "within a meter"))
        {
            arrowSpawned = true;
            if (objectList.scenicPlayers[0] != null)
            {
                GameObject closest = objectList.scenicPlayers[0]; // hardcoded closest opponent
                SpawnArrow(this.transform.position, closest.transform.position);
            }
        }
        if (tlManager.Paused == false && (circleObjects.Count > 1 || arrowObjects.Count > 0))
        {
            if (circleObjects.Count > 1)
            {
                for (int i = 1; i < circleObjects.Count; i++)
                {
                    Destroy(circleObjects[i]);
                }
                circleObjects.RemoveRange(1, circleObjects.Count - 1);
            }
            if (arrowObjects.Count > 0)
            {
                for (int i = 0; i < arrowObjects.Count; i++)
                {
                    Destroy(arrowObjects[i]);
                }
                arrowObjects.RemoveRange(0, arrowObjects.Count - 1);
            }
            circleSpawned = false;
            arrowSpawned = false;
        }
        
        List<Vector3> posList = new List<Vector3>();
        foreach (GameObject player in objectList.scenicPlayers)
        {
            if (player == this.gameObject)
            {
                continue;
            }
            posList.Add(player.transform.position);
        }
        
        Vector3 pos = GetClosestToLinePoint(posList);
        pointDebug = pos;
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
            Debug.LogError("forcibly get ball");
            ballOwnership.ballOwner.GetComponent<PlayerInterface>().LosePossession();
            GainPossession(ball);
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
        ballOwnership.SetScenicOwnership(false);
        ballOwnership.SetHumanOwnership(true);
        ballOwnership.SetBallOwner(this.gameObject);
        this.GetComponentInParent<ActionAPI>().ReceiveBall(other.transform.position);
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
        List<Vector3> posList = new List<Vector3>();
        foreach (GameObject player in objectList.scenicPlayers)
        {
            if (player == this.gameObject)
            {
                continue;
            }
            posList.Add(player.transform.position);
        }
        
        Vector3 pos = GetClosestToLinePoint(posList);
        pointDebug = pos;
        Debug.LogError(pointDebug);
        
        actionAPI.GroundPassFast(pos);
        StartCoroutine(ResetToMovementController());
    }

    private IEnumerator ResetToMovementController()
    {
        inAnimation = true;
        yield return new WaitForSeconds(1.0f);
        actionAPI.SetAnimController("Movement");
        inAnimation = false;
    }

    public float DistancePointToLineSqr(Ray ray, Vector3 point) {
        return Vector3.Cross(ray.direction, point - ray.origin).sqrMagnitude;
    }

    private Vector3 GetClosestToLinePoint(List<Vector3> points) {
        Ray ray = new Ray(transform.position, GetComponent<ControllerInput>().playerDirection.normalized);
        Vector3 closestPoint = points.OrderBy(point => DistancePointToLineSqr(ray, point)).FirstOrDefault();

        return closestPoint;
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
        source.PlayOneShot(source.clip);
        this.transform.position = pos;
        Debug.LogWarning("Local: I am transforming to: " + pos.ToString());

    }
    
    public void SetTransform2(GameObject go, Vector3 pos)
    {
        source.PlayOneShot(source.clip);
        go.transform.position = pos;

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

    public void SpawnArrow(Vector3 from, Vector3 to)
    {
        Vector3 spawnPos = new Vector3(from.x, from.y, from.z);
        GameObject arrow = Instantiate(arrowGenerator, spawnPos, arrowGenerator.transform.rotation);
        arrow.GetComponentInChildren<ArrowGenerator>().SetOrigin(from);
        arrow.GetComponentInChildren<ArrowGenerator>().SetTarget(to);
        arrowObjects.Add(arrow);
        arrow.GetComponentInChildren<Renderer>().material.SetColor("_Color", Color.yellow);
    }
    
    public void ApplyMovement(ScenicMovementData data)
    {
        localTick += 1;
        // Debug.Log(GetComponent<Rigidbody>().velocity);
        if (localTick < 4)
        {
            return;
        }
        
        if (data.behavior == " " || data.behavior == "" || data.behavior == "Idle")
        {
            behavior = "Idle";
            floatingText.SetText("Idle");
        }
        else if (data.behavior != "" || data.behavior != null)
        {
            behavior = data.behavior;
            floatingText.SetText(data.behavior);
        }
        else
        {
            behavior = "Idle";
            floatingText.SetText("Idle");
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
