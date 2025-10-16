using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.ConstrainedExecution;
using Fusion;
using Old.OpenAI.Samples.Chat;
using Pathfinding;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

#region Mathematical Reference for Parabolic Trajectories
// Parabola equations for ball trajectory calculations:
// Standard form: (x-5)^2 = -4(25/8)(y-2) => y = -2/25(x^2 - 10x)  (Max Distance 10)
// General form: (x-(d/2))^2 = -4((d/2)^2/8)(y-2)) => y = (8 (d x - x^2))/d^2
#endregion

/// <summary>
/// Main API class for handling all player actions including movement, ball interactions, and animations.
/// Provides a comprehensive interface for soccer gameplay mechanics in a networked multiplayer environment.
/// </summary>
public class ActionAPI : NetworkBehaviour
{
    #region Inspector Fields
    [SerializeField] float playerRunningSpeed = 1f;
    [SerializeField] float timeDuration = 5f;
    [SerializeField] float goalWidth = 7.44f;
    [SerializeField] GameObject soccerBall;
    #endregion

    #region Public Properties
    public Vector3 test;
    public bool stopMovement = false;
    public bool alreadyInAnimation = false;
    #endregion

    #region Private Fields
    string transitionTo = "t";

    // Force multipliers for different ball actions
    float forceFactor = 7f;
    float weakPassForce = 1f;
    float strongPassForce = 10f;
    float airPassForce = 3f;
    float chipForce = 1.5f;
    float shootForce = 8f;

    float rotationDuration = 0.4f;
    
    // Ball movement parameters set by SetMoveBallValues()
    private Vector3 finalPos;
    private float aerialOffset;
    private float forceMagnitude;
    private Vector3 ballPositionAtPassTime;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (soccerBall == null)
        {
            soccerBall = GameObject.FindGameObjectWithTag("ball");
        }
    }

    private void Update()
    {
        if (soccerBall == null)
        {
            soccerBall = GameObject.FindGameObjectWithTag("ball");
        }

        // Reset animation flag when returning to Movement controller
        if (this.GetComponent<Animator>().isActiveAndEnabled && this.GetComponent<Animator>().runtimeAnimatorController != null)
        {
            string currAnimationController = this.GetComponent<Animator>().runtimeAnimatorController.name;
            if (currAnimationController == "Movement")
            {
                alreadyInAnimation = false;
            }
        }
    }
    #endregion

    #region API Methods for BlendTree Animations
    /// <summary>
    /// Sets player to idle state by zeroing all movement parameters
    /// </summary>
    public void Idle()
    {
        this.gameObject.GetComponent<Animator>().SetFloat("VelZ", 0);
        this.gameObject.GetComponent<Animator>().SetFloat("VelX", 0);
    }
    
    /// <summary>
    /// Moves player to specified position using standard movement controller
    /// </summary>
    /// <param name="destinationPosition">Target world position</param>
    /// <param name="speed">Movement speed multiplier</param>
    /// <param name="lookAt">Whether to look at destination while moving</param>
    public void MoveToPos(Vector3 destinationPosition, float speed = 2f, bool lookAt = false)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            RPC_SetAnimController("Movement");
        }
        StartCoroutine(MoveToPosHelper(destinationPosition, lookAt));
    }
    
    /// <summary>
    /// Moves player using factory-specific movement controller (for industrial scenarios)
    /// </summary>
    public void FactoryMoveToPos(Vector3 destinationPosition, float speed = 1f, bool lookAt = false)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            RPC_SetAnimController("FactoryMovement");
        }
        StartCoroutine(MoveToPosHelper(destinationPosition, lookAt));
    }
    
    /// <summary>
    /// Moves to position while continuously looking at the ball
    /// </summary>
    public void MoveToPosLookAtBall(Vector3 destinationPosition, float speed = 2f, bool lookAt = true)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            RPC_SetAnimController("Movement");
        }
        StartCoroutine(MoveToPosHelper(destinationPosition, true));
    }

    /// <summary>
    /// Sets the player's movement speed
    /// </summary>
    public void SetPlayerSpeed(float speed)
    {
        playerRunningSpeed = speed;
    }

    /// <summary>
    /// Outputs debug message to console (for Scenic script debugging)
    /// </summary>
    public void ScenicPrint(string output) {
        Debug.Log(output);
    }
    
    /// <summary>
    /// Moves player while dribbling ball to destination
    /// </summary>
    public void DribbleFromOnePositionToAnother(Vector3 destinationPosition)
    {
        SetAnimController("Dribbling");
        StartCoroutine(DribbleLerp(destinationPosition));
    }

    /// <summary>
    /// Performs header shot towards destination with specified height
    /// </summary>
    public void BallHeaderShoot(Vector3 destinationPosition, string ballProjectileHeight)
    {
        SetAnimController("Headers");
        StartCoroutine(BallHeader(destinationPosition, VerticalForce(ballProjectileHeight)));
    }
    #endregion
    
    #region Methods for Human/AI Agent Communication
    /// <summary>
    /// Makes character speak text through chat system with timeline pause/unpause
    /// </summary>
    public void Speak(string text)
    {
        CallPause();
        
        OldChatBehaviour chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponentInChildren<OldChatBehaviour>();
        StartCoroutine(WaitForChat(text, chatBehaviour));
    }
    
    /// <summary>
    /// Coroutine that waits for speech to finish before unpausing timeline
    /// </summary>
    IEnumerator WaitForChat(string text, OldChatBehaviour chatBehaviour)
    {
        if (chatBehaviour != null)
        {
            chatBehaviour.SetInputTextAndSubmit(text);
        }
        else
        {
            Debug.LogError("ChatBehaviour instance not found in the scene.");
        }
        
        while (!chatBehaviour.HasSpeechFinished())
        {
            yield return null;
        }
        chatBehaviour.IsSpeechFinished = false;
        CallUnpause();
    }
    
    /// <summary>
    /// Makes character explain something without pausing timeline
    /// </summary>
    public void Explain(string text)
    {
        OldChatBehaviour chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponentInChildren<OldChatBehaviour>();
        if (chatBehaviour != null)
        {
            chatBehaviour.SetInputTextAndSubmit(text);
        }
        else
        {
            Debug.LogError("ChatBehaviour instance not found in the scene.");
        }
    }

    /// <summary>
    /// Pauses the timeline manager
    /// </summary>
    public void CallPause()
    {
        TimelineManager tlManager =
            GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        tlManager.Pause();
    }
    
    /// <summary>
    /// Unpauses the timeline manager
    /// </summary>
    public void CallUnpause()
    {
        TimelineManager tlManager =
            GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        tlManager.Unpause();
    }

    /// <summary>
    /// Starts a new program synthesis segment
    /// </summary>
    public void SegmentStart()
    {
        ProgramSynthesisManager programSynthesisManager =
            GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();
        programSynthesisManager.StartSegment();
    }
    
    /// <summary>
    /// Ends current program synthesis segment
    /// </summary>
    public void SegmentEnd()
    {
        ProgramSynthesisManager programSynthesisManager =
            GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();
        programSynthesisManager.StartSegment();
    }
    #endregion

    #region API Methods for Singleton Animations

    #region Player Singleton Methods
    
    /// <summary>
    /// Picks up nearest grabbable object within range (for factory scenarios)
    /// </summary>
    public void PickUp() 
    {
        if (this.GetComponent<Animator>().isActiveAndEnabled == true)
        {
            SetAnimController("FactoryMovement");
            stopMovement = true;
        }

        if (this.GetComponent<PlayerInterface>() == true)
        {
            PlayerInterface pI = this.GetComponent<PlayerInterface>();
            Vector3 lookAtPosition = pI.objectPosition.position;

            GameObject[] grabbableObjects = GameObject.FindGameObjectsWithTag("Grabbable");
            Vector3 originPosition = pI.gameObject.transform.position;

            GameObject closestObject = grabbableObjects
                .Where(obj => Vector3.Distance(obj.transform.position, originPosition) <= 2f)
                .OrderBy(obj => Vector3.Distance(obj.transform.position, originPosition))
                .FirstOrDefault();

            if (closestObject != null)
            {
                int layerIgnoreBallCollision = LayerMask.NameToLayer("PlayerBall");
                pI.gameObject.layer = layerIgnoreBallCollision;

                closestObject.transform.position = pI.objectPosition.position;
                closestObject.transform.SetParent(pI.objectPosition);

                // Disable physics when picked up
                Rigidbody rb = closestObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }

                pI.objectPossession = true;
                pI.grabbedObject = closestObject;

                StartCoroutine(LookTowards(lookAtPosition, "PickUp"));
            }
        }
        else if (this.GetComponent<HumanInterface>() == true)
        {
            HumanInterface hI = this.GetComponent<HumanInterface>();
            Vector3 lookAtPosition = hI.objectPosition.position;

            GameObject[] grabbableObjects = GameObject.FindGameObjectsWithTag("Grabbable");
            Vector3 originPosition = hI.gameObject.transform.position;

            GameObject closestObject = grabbableObjects
                .Where(obj => Vector3.Distance(obj.transform.position, originPosition) <= 2f)
                .OrderBy(obj => Vector3.Distance(obj.transform.position, originPosition))
                .FirstOrDefault();

            if (closestObject != null)
            {
                int layerIgnoreBallCollision = LayerMask.NameToLayer("PlayerBall");
                hI.gameObject.layer = layerIgnoreBallCollision;

                closestObject.transform.position = hI.objectPosition.position;
                closestObject.transform.SetParent(hI.objectPosition);

                // Disable physics when picked up
                Rigidbody rb = closestObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }

                hI.objectPossession = true;
                hI.grabbedObject = closestObject;

                if (!hI.isVR)
                {
                    StartCoroutine(LookTowards(lookAtPosition, "PickUp"));
                }
            }
        }
    }

    /// <summary>
    /// Puts down currently held object at specified position
    /// </summary>
    public void PutDown(Vector3 putDownPosition)
    {
        if (this.GetComponent<Animator>().isActiveAndEnabled == true)
        {
            SetAnimController("FactoryMovement");
            stopMovement = true;
        }

        if (this.GetComponent<PlayerInterface>() == true)
        {
            PlayerInterface pI = this.GetComponent<PlayerInterface>();

            if (pI.objectPossession == false)
            {
                return;
            }

            this.gameObject.layer = LayerMask.NameToLayer("Default");
            GameObject droppedObject = pI.grabbedObject;
            droppedObject.transform.SetParent(null);
            droppedObject.transform.position = putDownPosition;

            // Re-enable physics after putting down
            Rigidbody rb = droppedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.isKinematic = false;
            }

            pI.grabbedObject = null;
            pI.objectPossession = false;
            
            StartCoroutine(LookTowards(putDownPosition, "PutDown"));
        }
        else if (this.GetComponent<HumanInterface>() == true)
        {
            HumanInterface hI = this.GetComponent<HumanInterface>();

            if (hI.objectPossession == false)
            {
                return;
            }

            this.gameObject.layer = LayerMask.NameToLayer("Default");
            GameObject droppedObject = hI.grabbedObject;
            droppedObject.transform.SetParent(null);
            droppedObject.transform.position = putDownPosition;

            // Re-enable physics after putting down
            Rigidbody rb = droppedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.isKinematic = false;
            }

            hI.grabbedObject = null;
            hI.objectPossession = false;

            if (!hI.isVR)
            {
                StartCoroutine(LookTowards(putDownPosition, "PutDown"));
            }
        }
    }

    /// <summary>
    /// Performs packaging action on nearest object (for factory scenarios)
    /// </summary>
    public void Packaging()
    {
        if (this.GetComponent<Animator>() == true)
        {
            SetAnimController("FactoryMovement");
            stopMovement = true;
        }

        GameObject closestObject = FindNearestObject();

        if (closestObject != null)
        {
            StartCoroutine(ChangeObjectColorAfterDelay(closestObject, Color.magenta, 3f));
            Debug.Log("Finished packaging the object");
            StartCoroutine(LookTowards(closestObject.transform.position, "Packaging"));
        }
        else
        {
            Debug.LogError("No object found for Packaging.");
        }
    }

    /// <summary>
    /// Changes object color after specified delay (visual feedback for packaging)
    /// </summary>
    private IEnumerator ChangeObjectColorAfterDelay(GameObject targetObject, Color color, float delay)
    {
        yield return new WaitForSeconds(delay);
        Renderer objRenderer = targetObject.GetComponent<Renderer>();

        if (objRenderer != null)
        {
            objRenderer.material.color = color;
        }
        else
        {
            Debug.LogError("Target object does not have a Renderer component.");
        }
    }

    /// <summary>
    /// Finds nearest grabbable object within 5 units
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

    /// <summary>
    /// Prepares to receive ball from specified direction
    /// </summary>
    public void ReceiveBall(Vector3 receiveFrom)
    {
        HumanInterface hI = this.GetComponent<HumanInterface>();
        PlayerInterface pI = this.GetComponent<PlayerInterface>();
        if (pI)
        {
            GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
            if (gm.isHost)
            {
                RPC_SetAnimController("Movement");
            }
            stopMovement = true;
            StartCoroutine(LookTowards(receiveFrom, "Receive"));
        } else if (hI)
        {
            if (hI.isVR)
            {
                return;
            }
            GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
            if (gm.isHost && !hI.isVR)
            {
                RPC_SetAnimController("Movement");
            }
            stopMovement = true;

            if (!hI.isVR)
            {
                StartCoroutine(LookTowards(receiveFrom, "Receive"));
            }
        }
    }
    
    /// <summary>
    /// Forcibly intercepts the ball (defensive action)
    /// </summary>
    public void InterceptBall()
    {
        HumanInterface hI = this.GetComponent<HumanInterface>();
        PlayerInterface pI = this.GetComponent<PlayerInterface>();
        if (pI)
        {
            pI.ForciblyGainPossession();
        } else if (hI)
        {
            if (hI.isVR)
            {
                return;
            }
            hI.ForciblyGainPossession();
        }
    }

    /// <summary>
    /// Makes player look at specified position
    /// </summary>
    public void LookAt(Vector3 lookAtPosition)
    {
        stopMovement = true;
        StartCoroutine(LookTowards(lookAtPosition, null));
    }
    
    /// <summary>
    /// Performs tackle animation towards specified direction
    /// </summary>
    public void TackleBall(Vector3 tackleFrom)
    {
        SetAnimController("Movement");
        stopMovement = true;
        StartCoroutine(LookTowards(tackleFrom, "StrongTackle"));
    }

    /// <summary>
    /// Performs slow ground pass to destination
    /// </summary>
    public void GroundPassSlow(Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController("Dribbling");
        StartCoroutine(LookTowards(destinationPosition, "GroundPassSlow"));
        
        SetMoveBallValues(destinationPosition, 0, weakPassForce);
    }

    /// <summary>
    /// Performs fast ground pass to destination with automatic target finding for human players
    /// </summary>
    public void GroundPassFast(Vector3 destinationPosition)
    {
        stopMovement = true;
        if (this.GetComponent<Animator>().enabled == false)
        {
            return;
        }
        if (this.GetComponent<PlayerInterface>() == true && this.GetComponent<PlayerInterface>().ballPossession == false && this.GetComponent<PlayerInterface>().canKickBall == false)
        {
            return;
        } else if (this.GetComponent<HumanInterface>() == true && this.GetComponent<HumanInterface>().ballPossession == false && this.GetComponent<HumanInterface>().canKickBall == false)
        {
            return;
        }
        HumanInterface hI = this.GetComponent<HumanInterface>();
        PlayerInterface pI = this.GetComponent<PlayerInterface>();
        if (pI)
        {
            // Special case: pass to coach when destination is zero vector
            if (destinationPosition == Vector3.zero)
            {
                GameObject human = GameObject.FindGameObjectWithTag("human");
                if (human != null)
                {
                    AIDestinationSetter aiDestinationSetter = human.GetComponent<AIDestinationSetter>();
                    if (aiDestinationSetter != null && aiDestinationSetter.enabled && 
                        (human.GetComponent<HumanInterface>()?.isMoving ?? false))
                    {
                        destinationPosition = aiDestinationSetter.target.position;
                    }
                    else
                    {
                        destinationPosition = human.transform.position;
                        if (human.GetComponent<HumanInterface>().isVR)
                        {
                            destinationPosition = human.GetComponent<HumanInterface>().vrTransform.position;
                        }
                    }
                }
                else
                {
                    Debug.LogError("Coach not found in the scene.");
                    return;
                }
            }
                
            GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
            if (gm.isHost)
            {
                RPC_SetAnimController("Dribbling");
            }
            
            StartCoroutine(LookTowards(destinationPosition, "GroundPassFast"));
            StartCoroutine(this.GetComponent<PlayerInterface>().KickDebounce());
            
            ballPositionAtPassTime = soccerBall.transform.position;
            SetMoveBallValues(destinationPosition, 0, strongPassForce);
        } else if (hI)
        {
            // For human players, find closest defensive player near destination
            if (this.gameObject.CompareTag("human"))
            {
                ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();

                GameObject closestPlayer = null;
                float closestDistance = float.MaxValue;
                foreach (GameObject player in objectList.defensePlayers)
                {
                    float distance = Vector3.Distance(player.transform.position, destinationPosition);
                    if (distance < closestDistance && distance < 2f)
                    {
                        closestDistance = distance;
                        closestPlayer = player;
                    }
                }

                if (closestPlayer != null)
                {
                    AIDestinationSetter aiDestinationSetter = closestPlayer.GetComponent<AIDestinationSetter>();
                    if (aiDestinationSetter != null && aiDestinationSetter.enabled &&
                        (closestPlayer.GetComponent<PlayerInterface>()?.isMoving ?? false))
                    {
                        destinationPosition = aiDestinationSetter.target.position;
                    }
                    else
                    {
                        destinationPosition = closestPlayer.transform.position;
                    }
                }
            }
            
            GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
            if (gm.isHost && !hI.isVR)
            {
                RPC_SetAnimController("Dribbling");
            }
            
            if (!hI.isVR)
            {
                StartCoroutine(LookTowards(destinationPosition, "GroundPassFast"));
                StartCoroutine(this.GetComponent<HumanInterface>().KickDebounce());
            }

            ballPositionAtPassTime = soccerBall.transform.position;
            SetMoveBallValues(destinationPosition, 0, strongPassForce);
        
            if (hI.isVR)
            {
                MoveBall();
            }
        }
    }

    /// <summary>
    /// Performs aerial pass with specified trajectory height
    /// </summary>
    public void AirPass(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("Dribbling");
        StartCoroutine(LookTowards(destinationPosition, "AirPass"));

        SetMoveBallValues(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }

    /// <summary>
    /// Performs left-footed chip shot
    /// </summary>
    public void ChipLeft(Vector3 destinationPosition, string ballProjectileHeight)
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("Dribbling");
        selfPlayer.GetComponent<Animator>().SetTrigger("ChipLeft");

        SetMoveBallValues(destinationPosition, VerticalForce(ballProjectileHeight), chipForce);
    }

    /// <summary>
    /// Performs right-footed chip shot
    /// </summary>
    public void ChipRight(Vector3 destinationPosition, string ballProjectileHeight)
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("Dribbling");
        selfPlayer.GetComponent<Animator>().SetTrigger("ChipRight");

        SetMoveBallValues(destinationPosition, VerticalForce(ballProjectileHeight), chipForce);
    }

    /// <summary>
    /// Performs forward chip shot
    /// </summary>
    public void ChipFront(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("Dribbling");
        StartCoroutine(LookTowards(destinationPosition, "ChipFront"));

        SetMoveBallValues(destinationPosition, VerticalForce(ballProjectileHeight), chipForce);
    }

    /// <summary>
    /// Shoots ball towards goal with zone-based targeting system
    /// </summary>
    /// <param name="destinationPosition">Base goal position</param>
    /// <param name="destinationZone">Target zone within goal (e.g., "left-top", "center-middle")</param>
    public void Shoot(Vector3 destinationPosition, string destinationZone)
    {
        stopMovement = true;
        if (this.GetComponent<Animator>().enabled == false)
        {
            return;
        }
        if (this.GetComponent<PlayerInterface>().ballPossession == false && this.GetComponent<PlayerInterface>().canKickBall == false)
        {
            return;
        }
        SetAnimController("Dribbling");
        
        StartCoroutine(LookTowards(destinationPosition, "Shoot"));
        StartCoroutine(this.GetComponent<PlayerInterface>().KickDebounce());

        string ballProjectileHeight = "low";
        float horizontalOffset = (goalWidth / 6.0f);

        // Adjust position and height based on target zone
        switch (destinationZone)
        {
            case "left-top":
                ballProjectileHeight = "medium";
                destinationPosition -= new Vector3(2.0f * horizontalOffset, 0f, 0f);
                break;
            case "left-middle":
                ballProjectileHeight = "low";
                destinationPosition -= new Vector3(2.0f * horizontalOffset, 0f, 0f);
                break;
            case "left-bottom":
                ballProjectileHeight = "grounded";
                destinationPosition -= new Vector3(2.0f * horizontalOffset, 0f, 0f);
                break;
            case "center-top":
                ballProjectileHeight = "medium";
                break;
            case "center-middle":
                ballProjectileHeight = "low";
                break;
            case "center-bottom":
                ballProjectileHeight = "grounded";
                break;
            case "right-top":
                ballProjectileHeight = "medium";
                destinationPosition += new Vector3(2.0f * horizontalOffset, 0f, 0f);
                break;
            case "right-middle":
                ballProjectileHeight = "low";
                destinationPosition += new Vector3(2.0f * horizontalOffset, 0f, 0f);
                break;
            case "right-bottom":
                ballProjectileHeight = "grounded";
                destinationPosition += new Vector3(2.0f * horizontalOffset, 0f, 0f);
                break;
            case "empty":
                ballProjectileHeight = "grounded";
                break;
        }

        Debug.Log("destination position: " + destinationPosition);
        SetMoveBallValues(destinationPosition, VerticalForce(ballProjectileHeight), shootForce);
    }

    /// <summary>
    /// Performs throw-in with specified trajectory
    /// </summary>
    public void BallThrow(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("Movement");
        StartCoroutine(LookTowards(destinationPosition, "BallThrow"));

        SetMoveBallValues(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }
    #endregion

    #region Goalkeeper Singleton Methods
    /// <summary>
    /// Goalkeeper idle state while holding ball
    /// </summary>
    public void IdleWithBallInHand()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetFloat("positionY", 1f);
    }

    /// <summary>
    /// Goalkeeper body block to the left
    /// </summary>
    public void BodyBlockLeftSide()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("BodyBlockLeftSide");
    }

    /// <summary>
    /// Goalkeeper body block to the right
    /// </summary>
    public void BodyBlockRightSide()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("BodyBlockRightSide");
    }

    /// <summary>
    /// Goalkeeper catches ground ball
    /// </summary>
    public void CatchGroundBall()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchGroundBall");
    }

    /// <summary>
    /// Goalkeeper catches ball in the air
    /// </summary>
    public void CatchBallInTheAir()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchBallInTheAir");
    }

    /// <summary>
    /// Goalkeeper catches slow-moving ball
    /// </summary>
    public void CatchSlowBall()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchSlowBall");
    }

    /// <summary>
    /// Goalkeeper performs drop kick
    /// </summary>
    public void DropKickShot(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("GoalKeeper");
        StartCoroutine(LookTowards(destinationPosition, "DropKick"));

        SetMoveBallValues(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }

    /// <summary>
    /// Goalkeeper performs overhand throw
    /// </summary>
    public void OverHandThrow(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("GoalKeeper");
        StartCoroutine(LookTowards(destinationPosition, "OverHandThrow"));

        SetMoveBallValues(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }

    /// <summary>
    /// Goalkeeper performs rolling ball pass along ground
    /// </summary>
    public void RollingBallPass(Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController("GoalKeeper");
        StartCoroutine(LookTowards(destinationPosition, "RollingBallPass"));

        SetMoveBallValues(destinationPosition, 0, weakPassForce);
    }

    /// <summary>
    /// Goalkeeper places ball and performs long pass
    /// </summary>
    public void PlacingAndLongPass(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("GoalKeeper");
        StartCoroutine(LookTowards(destinationPosition, "PlacingAndLongPass"));

        SetMoveBallValues(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }

    /// <summary>
    /// Goalkeeper places ball and performs short pass
    /// </summary>
    public void PlacingAndShortPass(Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController("GoalKeeper");
        StartCoroutine(LookTowards(destinationPosition, "PlacingAndShortPass"));
        
        SetMoveBallValues(destinationPosition, 0, weakPassForce);
    }
    #endregion

    #endregion

    #region Helper Coroutines
    /// <summary>
    /// Main movement helper that handles AI pathfinding
    /// </summary>
    IEnumerator MoveToPosHelper(Vector3 destinationPosition, bool lookAt)
    {
        GameObject selfPlayer = this.gameObject;
        selfPlayer.GetComponent<RichAI>().maxSpeed = playerRunningSpeed;

        yield return new WaitForSeconds(0.1f);
        
        AIDestinationSetter dest = selfPlayer.GetComponent<AIDestinationSetter>();
        RichAI aiNav = selfPlayer.GetComponent<RichAI>();

        if (!lookAt)
        {
            StartCoroutine(Move2(dest, aiNav, destinationPosition));
        }
        else if (lookAt)
        {
            StartCoroutine(Move3(dest, aiNav, destinationPosition));
        }

        yield return null;
        
        if (stopMovement)
        {
            dest.target.localPosition = Vector3.zero;
            stopMovement = false;
            selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 0);
            selfPlayer.GetComponent<Animator>().SetFloat("VelX", 0);
        }
    }
    
    /// <summary>
    /// Standard movement without looking at destination
    /// </summary>
    IEnumerator Move2(AIDestinationSetter destSetter, RichAI aiNav, Vector3 Destiny)
    {
        GameObject selfPlayer = this.gameObject;
        destSetter.target.position = Destiny;

        if (this.gameObject.CompareTag("human"))
        {
            HumanInterface hI = this.gameObject.GetComponent<HumanInterface>();
            while (destSetter.target.position != this.gameObject.transform.position)
            {
                StartCoroutine(hI.SetIsMoving(true));
                float velz = aiNav.velocity.magnitude;
                selfPlayer.GetComponent<Animator>().SetFloat("VelZ", velz);
                yield return null;
            }
            StartCoroutine(hI.SetIsMoving(false));
        }
        else
        {
            PlayerInterface pI = this.gameObject.GetComponent<PlayerInterface>();
            while (destSetter.target.position != this.gameObject.transform.position)
            {
                StartCoroutine(pI.SetIsMoving(true));
                float velz = aiNav.velocity.magnitude;
                selfPlayer.GetComponent<Animator>().SetFloat("VelZ", velz);
                yield return null;
            }
            StartCoroutine(pI.SetIsMoving(false));
        }
    }
    
    /// <summary>
    /// Movement while looking at ball
    /// </summary>
    IEnumerator Move3(AIDestinationSetter destSetter, RichAI aiNav, Vector3 Destiny)
    {
        aiNav.updateRotation = false;
        if (this.gameObject.CompareTag("human"))
        {
            Transform ball = this.GetComponent<HumanInterface>().ball.transform;
            Vector3 targetPos = new Vector3(ball.transform.position.x, this.transform.position.y,
                ball.transform.position.z);
            transform.LookAt(targetPos);
        }
        else
        {
            Transform ball = this.GetComponent<PlayerInterface>().ball.transform;
            Vector3 targetPos = new Vector3(this.transform.position.x, ball.transform.position.y,
                this.transform.position.z);
            transform.LookAt(targetPos);
        }
        GameObject selfPlayer = this.gameObject;
        destSetter.target.position = Destiny;

        if (this.gameObject.CompareTag("human"))
        {
            HumanInterface hI = this.gameObject.GetComponent<HumanInterface>();
            while (destSetter.target.position != this.gameObject.transform.position)
            {
                StartCoroutine(hI.SetIsMoving(true));
                float velz = aiNav.velocity.magnitude;
                selfPlayer.GetComponent<Animator>().SetFloat("VelZ", velz);
                yield return null;
            }
            StartCoroutine(hI.SetIsMoving(false));
        }
        else
        {
            PlayerInterface pI = this.gameObject.GetComponent<PlayerInterface>();
            while (destSetter.target.position != this.gameObject.transform.position)
            {
                StartCoroutine(pI.SetIsMoving(true));
                float velz = aiNav.velocity.magnitude;
                selfPlayer.GetComponent<Animator>().SetFloat("VelZ", velz);
                yield return null;
            }
            StartCoroutine(pI.SetIsMoving(false));
        }
        aiNav.updateRotation = true;
        yield return null;
    }
    
    /// <summary>
    /// Movement with constant forward velocity (legacy method)
    /// </summary>
    IEnumerator Move4(AIDestinationSetter destSetter, RichAI aiNav, Vector3 Destiny)
    {
        GameObject selfPlayer = this.gameObject;
        destSetter.target.position = Destiny;
        while (destSetter.target.position != this.gameObject.transform.position)
        {
            float velz = aiNav.velocity.magnitude / playerRunningSpeed * 2;
            selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 1);
            yield return null;
        }
    }
    
    /// <summary>
    /// Legacy movement method with parabolic velocity curves
    /// </summary>
    private IEnumerator MovementLerp2(Vector3 final)
    {
        GameObject selfPlayer = this.gameObject;
        Vector3 init = selfPlayer.transform.position;
        Vector2 unitVector = new(selfPlayer.transform.forward.x, selfPlayer.transform.forward.z);

        final.y = init.y;
        float timeElapsed = 0;
        float distance = Vector3.Distance(init, final);

        timeDuration = 2f * distance / playerRunningSpeed;

        float quadrant = RelativeQuadrant(init, final, unitVector);

        int xSign = 0;
        int zSign = 0;

        // Determine movement direction based on relative quadrant
        switch (quadrant)
        {
            case 0.5f: xSign = 1; zSign = 0; break;
            case 1.0f: xSign = 1; zSign = 1; break;
            case 1.5f: xSign = 0; zSign = 1; break;
            case 2.0f: xSign = -1; zSign = 1; break;
            case 2.5f: xSign = -1; zSign = 0; break;
            case 3.0f: xSign = -1; zSign = -1; break;
            case 3.5f: xSign = 0; zSign = -1; break;
            case 4.0f: xSign = 1; zSign = -1; break;
        }

        Debug.Log(xSign + ", " + zSign);

        float distanceX = Mathf.Abs(init.x - final.x);
        float distanceZ = Mathf.Abs(init.z - final.z);

        while (timeElapsed < timeDuration)
        {
            float UpdatedDistanceX = Mathf.Abs(init.x - transform.position.x);
            float UpdatedDistanceZ = Mathf.Abs(init.z - transform.position.z);

            // Calculate parabolic velocity curves
            float transitionValueZ = zSign;
            if (transitionValueZ != 0)
                transitionValueZ *= (8f * (distanceZ * (UpdatedDistanceZ) - Mathf.Pow(UpdatedDistanceZ, 2)) /
                                     Mathf.Pow(distanceZ, 2));
            float transitionValueX = xSign;
            if (transitionValueX != 0)
                transitionValueX *= (8f * (distanceX * (UpdatedDistanceX) - Mathf.Pow(UpdatedDistanceX, 2)) /
                                     Mathf.Pow(distanceX, 2));

            selfPlayer.GetComponent<Animator>().SetFloat("VelX", transitionValueX);
            selfPlayer.GetComponent<Animator>().SetFloat("VelZ", transitionValueZ);

            timeElapsed += Time.deltaTime;
            yield return null;

            if (stopMovement)
            {
                stopMovement = false;
                selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 0);
                selfPlayer.GetComponent<Animator>().SetFloat("VelX", 0);
                StopCoroutine(MovementLerp2(final));
            }
        }

        yield return null;
    }

    /// <summary>
    /// Legacy movement with optional look-at functionality
    /// </summary>
    private IEnumerator MovementLerp(Vector3 final, bool lookAt)
    {
        GameObject selfPlayer = this.gameObject;
        Vector3 init = selfPlayer.transform.position;
        Vector2 unitVector = new(selfPlayer.transform.forward.x, selfPlayer.transform.forward.z);

        final.y = init.y;
        float timeElapsed = 0;
        float distance = Vector3.Distance(init, final);

        timeDuration = 2f * distance / playerRunningSpeed;

        if (!lookAt)
        {
            float quadrant = RelativeQuadrant(init, final, unitVector);

            int xSign = 0;
            int zSign = 0;

            switch (quadrant)
            {
                case 0.5f: xSign = 1; zSign = 0; break;
                case 1.0f: xSign = 1; zSign = 1; break;
                case 1.5f: xSign = 0; zSign = 1; break;
                case 2.0f: xSign = -1; zSign = 1; break;
                case 2.5f: xSign = -1; zSign = 0; break;
                case 3.0f: xSign = -1; zSign = -1; break;
                case 3.5f: xSign = 0; zSign = -1; break;
                case 4.0f: xSign = 1; zSign = -1; break;
            }

            Debug.Log(xSign + ", " + zSign);

            float distanceX = Mathf.Abs(init.x - final.x);
            float distanceZ = Mathf.Abs(init.z - final.z);

            while (timeElapsed < timeDuration)
            {
                float t = timeElapsed / timeDuration;
                t = t * t * (3f - 2f * t);

                float UpdatedDistanceX = Mathf.Abs(init.x - transform.position.x);
                float UpdatedDistanceZ = Mathf.Abs(init.z - transform.position.z);

                float transitionValueZ = zSign;
                if (transitionValueZ != 0)
                    transitionValueZ *= (8f * (distanceZ * (UpdatedDistanceZ) - Mathf.Pow(UpdatedDistanceZ, 2)) / Mathf.Pow(distanceZ, 2));
                float transitionValueX = xSign;
                if (transitionValueX != 0)
                    transitionValueX *= (8f * (distanceX * (UpdatedDistanceX) - Mathf.Pow(UpdatedDistanceX, 2)) / Mathf.Pow(distanceX, 2));

                selfPlayer.GetComponent<Animator>().SetFloat("VelX", transitionValueX);
                selfPlayer.GetComponent<Animator>().SetFloat("VelZ", transitionValueZ);

                transform.position = Vector3.Lerp(init, final, t);
                timeElapsed += Time.deltaTime;
                yield return null;

                if (stopMovement)
                {
                    stopMovement = false;
                    selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 0);
                    selfPlayer.GetComponent<Animator>().SetFloat("VelX", 0);
                    StopCoroutine(MovementLerp(final, lookAt));
                }
            }
        }
        else
        {
            StartCoroutine(LookTowards(final, null));

            while (timeElapsed < timeDuration)
            {
                float t = timeElapsed / timeDuration;
                t = t * t * (3f - 2f * t);

                float UpdatedDistance = Vector3.Distance(init,transform.position);

                float transitionValue = (8f * (distance * (UpdatedDistance) - Mathf.Pow(UpdatedDistance, 2)) / Mathf.Pow(distance, 2));

                selfPlayer.GetComponent<Animator>().SetFloat("VelZ", transitionValue);

                transform.position = Vector3.Lerp(init, final, t);
                timeElapsed += Time.deltaTime;
                yield return null;

                if (stopMovement)
                {
                    stopMovement = false;
                    selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 0);
                    selfPlayer.GetComponent<Animator>().SetFloat("VelX", 0);
                    StopCoroutine(MovementLerp(final, lookAt));
                }
            }
        }
        yield return null;
    }

    /// <summary>
    /// Dribbling movement with ball control
    /// </summary>
    private IEnumerator DribbleLerp(Vector3 final)
    {
        GameObject selfPlayer = this.gameObject;
        Vector3 init = selfPlayer.transform.position;
        final.y = init.y;
        StartCoroutine(LookTowards(final, null));

        float timeElapsed = 0;
        float distance = Vector3.Distance(init, final);
        timeDuration = 2f * distance / playerRunningSpeed;

        while (timeElapsed < timeDuration)
        {
            float t = timeElapsed / timeDuration;
            t = t * t * (3f - 2f * t);
            float UpdatedDistance = Vector3.Distance(init, transform.position);

            float transitionValue = (8f * (distance * (UpdatedDistance) - Mathf.Pow(UpdatedDistance, 2)) / Mathf.Pow(distance, 2));

            selfPlayer.GetComponent<Animator>().SetFloat("VelZ", transitionValue);

            transform.position = Vector3.Lerp(init, final, t);
            timeElapsed += Time.deltaTime;
            yield return null;

            if (stopMovement)
            {
                selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 0);
                StopCoroutine(DribbleLerp(final));
            }
        }
    }

    /// <summary>
    /// Header animation with ball trajectory calculation
    /// </summary>
    private IEnumerator BallHeader(Vector3 final, float aerialOffset)
    {
        GameObject selfPlayer = this.gameObject;
        Vector3 init = selfPlayer.transform.position;
        Vector2 unitVector = new(selfPlayer.transform.forward.x, selfPlayer.transform.forward.z);

        Vector2 objectAPosition = new(init.x, init.z);
        Vector2 objectBPosition = new(final.x, final.z);

        Vector2 shiftedObjectBPosition = objectBPosition - objectAPosition;

        float angle = Mathf.Atan2(unitVector.x, unitVector.y);

        // Rotate object B relative to object A
        float rotatedXB = shiftedObjectBPosition.x * Mathf.Cos(angle) - shiftedObjectBPosition.y * Mathf.Sin(angle);
        float rotatedYB = shiftedObjectBPosition.x * Mathf.Sin(angle) + shiftedObjectBPosition.y * Mathf.Cos(angle);

        float newAngle = Mathf.Atan2(rotatedXB, rotatedYB);
        float velZ = Mathf.Cos(newAngle);
        float velX = Mathf.Sin(newAngle);

        // Set animation parameters
        selfPlayer.GetComponent<Animator>().SetFloat("VelZ", velZ);
        selfPlayer.GetComponent<Animator>().SetFloat("VelX", velX);
        
        SetMoveBallValues(final, aerialOffset, airPassForce);

        yield return new WaitForSeconds(WaitTime());

        selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 0);
        selfPlayer.GetComponent<Animator>().SetFloat("VelX", 0);
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Sets parameters for ball movement (called by animation events)
    /// </summary>
    void SetMoveBallValues(Vector3 finalPos, float aerialOffset, float forceMagnitude)
    {
        this.finalPos = finalPos;
        this.aerialOffset = aerialOffset;
        this.forceMagnitude = forceMagnitude;
    }
    
    /// <summary>
    /// Network RPC for setting ball movement values across all clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetMoveBallValues(Vector3 finalPos, float aerialOffset, float forceMagnitude)
    {
        this.finalPos = finalPos;
        this.aerialOffset = aerialOffset;
        this.forceMagnitude = forceMagnitude;
    }

    /// <summary>
    /// Calculates wait time based on current animation length and speed
    /// </summary>
    private float WaitTime()
    {
        float animationLength = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        float animationSpeed = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).speed;
        float delay = animationLength * animationSpeed;
        return delay;
    }

    /// <summary>
    /// Coroutine that waits for current animation to complete
    /// </summary>
    IEnumerator WaitForAnimation()
    {
        yield return new WaitForSeconds(WaitTime());
    }

    /// <summary>
    /// Sets the animator controller for the player
    /// </summary>
    public void SetAnimController(string controllerHashCode)
    {
        GameObject selfPlayer = this.gameObject;
        string currAnimationController = selfPlayer.GetComponent<Animator>().runtimeAnimatorController.name;
        if (currAnimationController != controllerHashCode)
        {
            RuntimeAnimatorController newController = Resources.Load("Animation/" + controllerHashCode, typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            selfPlayer.GetComponent<Animator>().runtimeAnimatorController = newController;
        }
    }

    /// <summary>
    /// Network RPC for setting animator controller across all clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetAnimController(string controllerHashCode)
    {
        if (this.GetComponent<HumanInterface>() == true)
        {
            HumanInterface hI = this.GetComponent<HumanInterface>();
            if (hI.isVR == true)
            {
                return;
            }
        }
        GameObject selfPlayer = this.gameObject;
        string currAnimationController = selfPlayer.GetComponent<Animator>().runtimeAnimatorController.name;
        if (currAnimationController != controllerHashCode)
        {
            RuntimeAnimatorController newController = Resources.Load("Animation/" + controllerHashCode, typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            selfPlayer.GetComponent<Animator>().runtimeAnimatorController = newController;
        }
    }

    /// <summary>
    /// Calculates which quadrant the destination is relative to the player's forward direction
    /// </summary>
    private float RelativeQuadrant(Vector3 init, Vector3 final, Vector2 unitVector)
    {
        Vector2 objectAPosition = new(init.x, init.z);
        Vector2 objectBPosition = new(final.x, final.z);

        Vector2 shiftedObjectBPosition = objectBPosition - objectAPosition;

        float angle = Mathf.Atan2(unitVector.x, unitVector.y);

        // Rotate object B relative to object A
        float rotatedXB = shiftedObjectBPosition.x * Mathf.Cos(angle) - shiftedObjectBPosition.y * Mathf.Sin(angle);
        float rotatedYB = shiftedObjectBPosition.x * Mathf.Sin(angle) + shiftedObjectBPosition.y * Mathf.Cos(angle);

        // Determine quadrant
        float quadrant = 0.5f;
        if (rotatedXB > 0 && rotatedYB == 0)
            quadrant = 0.5f;
        else if (rotatedXB == 0 && rotatedYB > 0)
            quadrant = 1.5f;
        else if (rotatedXB < 0 && rotatedYB == 0)
            quadrant = 2.5f;
        else if (rotatedXB == 0 && rotatedYB < 0)
            quadrant = 3.5f;
        else if (rotatedXB > 0 && rotatedYB > 0)
            quadrant = 1.0f;
        else if (rotatedXB < 0 && rotatedYB > 0)
            quadrant = 2.0f;
        else if (rotatedXB < 0 && rotatedYB < 0)
            quadrant = 3.0f;
        else
            quadrant = 4.0f;

        return quadrant;
    }

    /// <summary>
    /// Applies physics force to move the ball (called from animation events)
    /// Handles ball possession transfer and creates annotations for AI analysis
    /// </summary>
    public void MoveBall()
    {
        if (Runner.IsClient)
        {
            return;
        }
        PlayerInterface pI = GetComponent<PlayerInterface>();
        HumanInterface hI = GetComponent<HumanInterface>();

        if (pI)
        {
            if (pI.ballPossession == false)
            {
                return;
            }
        } else if (hI)
        {
            if (hI.ballPossession == false)
            {
                return;
            }
        }
        
        // Release ball possession
        if (pI)
        {
            pI.LosePossession();
        } else if (hI)
        {
            hI.LosePossession();
        }
        
        GameObject ball = GameObject.FindGameObjectWithTag("ball");
        ball.GetComponent<SoccerBall>().SetDestination(finalPos);
        
        Vector3 ballMotionVector = finalPos - soccerBall.transform.position;
        Vector3 forceDirection = new(ballMotionVector.x, aerialOffset, ballMotionVector.z);
        Debug.Log("force vector: " + forceDirection);
        
        // Apply extra force for short passes to compensate for reduced power
        if (forceDirection.magnitude < 5f)
        {
            soccerBall.GetComponent<Rigidbody>().AddForce(forceDirection * forceMagnitude * forceFactor * 1.5f);
        }
        else
        {
            soccerBall.GetComponent<Rigidbody>().AddForce(forceDirection * forceMagnitude * forceFactor);
        }
        Debug.Log("in moveball");
        Debug.Log("force:" + forceDirection * forceMagnitude * forceFactor);
        
        // Create pass annotations for AI analysis (non-human players only)
        if (pI)
        {
            GameObject closestPlayer = FindClosestPlayerToFinalPos(finalPos);
            if (closestPlayer != null)
            {
                AnnotationManager annotationManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<AnnotationManager>();
                annotationManager.CreatePassAnnotation(this.gameObject, closestPlayer);
            } else if (closestPlayer == null)
            {
                AnnotationManager annotationManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<AnnotationManager>();
                annotationManager.CreateThroughPassAnnotation(this.gameObject, finalPos);
            }
        }
    }

    /// <summary>
    /// Finds the closest player to the ball's destination position for pass annotation
    /// </summary>
    public GameObject FindClosestPlayerToFinalPos(Vector3 pos)
    {
        ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        List<GameObject> players = objectList.scenicPlayers.Concat(objectList.humanPlayers).ToList();
        GameObject bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        foreach (GameObject player in players)
        {
            Vector3 playerPos = player.transform.position;
            
            if (player.GetComponent<HumanInterface>() && player.GetComponent<HumanInterface>().isVR)
            {
                playerPos = player.GetComponent<HumanInterface>().vrTransform.position;
            }
            
            Vector3 directionToTarget = playerPos - pos;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = player;
            }
        }
        
        // Only return player if they're very close to the destination (successful pass)
        if (closestDistanceSqr < 0.1f)
        {
            return bestTarget;
        }
        else
        {
            return null;
        }
    }
    
    /// <summary>
    /// Sets animation flag to true (called from animation events)
    /// </summary>
    public void SetAlreadyInAnimationTrue()
    {
        alreadyInAnimation = true;
    }
    
    /// <summary>
    /// Sets animation flag to false (called from animation events)
    /// </summary>
    public void SetAlreadyInAnimationFalse()
    {
        alreadyInAnimation = false;
    }

    /// <summary>
    /// Temporary disable of ball trigger collider to prevent immediate re-possession
    /// </summary>
    private IEnumerator BallTriggerColliderDebounce(GameObject ballTriggerCollider)
    {
        yield return new WaitForSeconds(3f);
        ballTriggerCollider.SetActive(true);
    }

    /// <summary>
    /// Smoothly rotates player to look at destination, then triggers specified animation
    /// </summary>
    private IEnumerator LookTowards(Vector3 destinationPosition, string keyCode)
    {
        GameObject selfPlayer = this.gameObject;
        Vector3 fromVector = selfPlayer.transform.forward;
        Vector3 toVector = destinationPosition - selfPlayer.transform.position;
        
        // Calculate angle and rotation direction
        float angle = Vector3.Angle(fromVector, toVector);
        Vector3 normal = Vector3.Cross(fromVector, toVector);
        float rotationDirection = Mathf.Sign(Vector3.Dot(normal, Vector3.up));

        yield return StartCoroutine(RotateCoroutine(angle, rotationDirection, rotationDuration / 2f));

        // Trigger animation after rotation completes
        if (keyCode != null)
        {
            selfPlayer.GetComponent<Animator>().SetTrigger(keyCode);
        }
    }
    
    /// <summary>
    /// Smoothly rotates player over specified duration
    /// </summary>
    private IEnumerator RotateCoroutine(float angle, float rotationDirection, float duration)
    {
        GameObject selfPlayer = this.gameObject;
        float currentRotation = 0.0f;
        float rotationPerSecond = angle / duration;

        while(currentRotation < angle)
        {
            float rotationIncrement = rotationPerSecond * Time.deltaTime;
            currentRotation += rotationIncrement;

            if (rotationDirection == -1)
                selfPlayer.transform.Rotate(Vector3.down, rotationIncrement);
            else if (rotationDirection == 1)
                selfPlayer.transform.Rotate(Vector3.up, rotationIncrement);
            
            yield return null;
        }

        yield return null;
    }

    /// <summary>
    /// Legacy method for delayed ball movement (no longer used)
    /// </summary>
    IEnumerator MoveBallAfterAnimationhalfway(Vector3 destinationPosition, string ballProjectileHeight,float shootForce)
    {
        yield return new WaitForSeconds(1f);
        SetMoveBallValues(destinationPosition, VerticalForce(ballProjectileHeight), shootForce);
    }

    /// <summary>
    /// Converts string height description to vertical force value
    /// </summary>
    private float VerticalForce(string ballProjectileHeight)
    {
        float verticalForce = 0.0f;
        float verticalForceFactor = 3f;
        if (ballProjectileHeight == "grounded") verticalForce = 0.0f;
        else if (ballProjectileHeight == "low") verticalForce = 1.5f;
        else if (ballProjectileHeight == "medium") verticalForce = 2.0f;
        else if (ballProjectileHeight == "high") verticalForce = 3.0f;

        return verticalForce * verticalForceFactor;
    }
    #endregion
}