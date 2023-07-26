using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

#region For Mathematical Reference
// parabola Equation if used (x-5)^2 = -4(25/8)(y-2) => y = -2/25(x^2 - 10x)  (in this Max Distance 10)
// parabola Equation if used (x-(d/2))^2 = -4((d/2)^2/8)(y-2)) => y = (8 (d x - x^2))/d^2
#endregion

public class ActionAPI : MonoBehaviour
{
    [SerializeField] float playerRunningSpeed = 2f;
    [SerializeField] float timeDuration = 5f;
    [SerializeField] float goalWidth = 7.44f;
    [SerializeField] GameObject soccerBall;

    public bool stopMovement = false;
    string transitionTo = "t";

    float forceFactor = 7f;
    float weakPassForce = 1f;
    float strongPassForce = 2f;
    float airPassForce = 3f;
    float chipForce = 1.5f;
    float shootForce = 4f;

    float rotationDuration = 0.4f;

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
    }


    #region API Methods for BlendTrees
    public void MoveToPos(Vector3 destinationPosition, bool lookAt = true)
    {
        // TODO: replace this anim controller with movement, need to merge humanoid AI movement with the movement controller
        //SetAnimController("Humanoid");
        StartCoroutine(MoveToPosHelper(destinationPosition, lookAt));
    }
    
    public void DribbleFromOnePositionToAnother(Vector3 destinationPosition)
    {
        SetAnimController("Dribbling");
        StartCoroutine(DribbleLerp(destinationPosition));
    }
    public void BallHeaderShoot(Vector3 destinationPosition, string ballProjectileHeight)
    {
        SetAnimController("Headers");
        StartCoroutine(BallHeader(destinationPosition, VerticalForce(ballProjectileHeight)));
    }
    #endregion

    #region API Methods for Singleton Animations

    #region PlayersSingletonMethods
    public void ReceiveBall(Vector3 receiveFrom)
    {
        SetAnimController("Movement");
        stopMovement = true;
        StartCoroutine(LookTowards(receiveFrom, "Receive"));
    }

    public void TackleBall(Vector3 tackleFrom)
    {
        SetAnimController("Movement");
        stopMovement = true;
        StartCoroutine(LookTowards(tackleFrom, "StrongTackle"));
    }

    public void GroundPassSlow(Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController("Dribbling");
        StartCoroutine(LookTowards(destinationPosition, "GroundPassSlow"));
        
        MoveBall(destinationPosition, 0, weakPassForce);
    }

    public void GroundPassFast(Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController("Dribbling");
        StartCoroutine(LookTowards(destinationPosition, "GroundPassFast"));
        
        MoveBall(destinationPosition, 0, strongPassForce);
    }

    public void AirPass(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("Dribbling");
        StartCoroutine(LookTowards(destinationPosition, "AirPass"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }

    public void ChipLeft(Vector3 destinationPosition, string ballProjectileHeight)
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("Dribbling");
        selfPlayer.GetComponent<Animator>().SetTrigger("ChipLeft");

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), chipForce);
    }

    public void ChipRight(Vector3 destinationPosition, string ballProjectileHeight)
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("Dribbling");
        selfPlayer.GetComponent<Animator>().SetTrigger("ChipRight");

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), chipForce);
    }

    public void ChipFront(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("Dribbling");
        StartCoroutine(LookTowards(destinationPosition, "ChipFront"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), chipForce);
    }

    public void Shoot(Vector3 destinationPosition, string destinationZone)
    {
        stopMovement = true;
        SetAnimController("Dribbling");
        
        StartCoroutine(LookTowards(destinationPosition, "Shoot"));

        
        string ballProjectileHeight = "low";
        float horizontalOffset = (goalWidth / 6.0f);

        
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

        // Debug.Log("ballmotionvector: " + ballMotionVector);
        StartCoroutine(MoveBallAfterAnimationhalfway(destinationPosition, ballProjectileHeight, shootForce));

        //
    }

    public void BallThrow(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("Dribbling");
        StartCoroutine(LookTowards(destinationPosition, "BallThrow"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }
    #endregion

    #region GoalKeeper SingletonMethods

    public void IdleWithBallInHand()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetFloat("positionY", 1f);
    }

    public void BodyBlockLeftSide()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("BodyBlockLeftSide");
    }
    public void BodyBlockRightSide()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("BodyBlockRightSide");
    }
    public void CatchGroundBall()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchGroundBall");
    }
    public void CatchBallInTheAir()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchBallInTheAir");
    }

    //methods for Goalkeeper movement (lefty or right)

    public void CatchSlowBall()
    {
        GameObject selfPlayer = this.gameObject;
        stopMovement = true;
        SetAnimController("GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchSlowBall");
        //-> Ball Interation 
        //StartCoroutine(Trigger(transitionTo + "CatchSlowBall"));

    }
    public void DropKickShot(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("GoalKeeper");
        StartCoroutine(LookTowards(destinationPosition, "DropKick"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }
    public void OverHandThrow(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("GoalKeeper");
        StartCoroutine(LookTowards(destinationPosition, "OverHandThrow"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }
    public void RollingBallPass(Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController("GoalKeeper");
        StartCoroutine(LookTowards(destinationPosition, "RollingBallPass"));

        MoveBall(destinationPosition, 0, weakPassForce);
    }
    public void PlacingAndLongPass(Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController("GoalKeeper");
        StartCoroutine(LookTowards(destinationPosition, "PlacingAndLongPass"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }
    public void PlacingAndShortPass(Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController("GoalKeeper");
        StartCoroutine(LookTowards(destinationPosition, "PlacingAndShortPass"));
        
        MoveBall(destinationPosition, 0, weakPassForce);
    }
    #endregion

    #endregion

    #region Helper Coroutines
    IEnumerator MoveToPosHelper(Vector3 destinationPosition, bool lookAt = true)
    {
        // Debug.Log("here");

        GameObject selfPlayer = this.gameObject;

        selfPlayer.GetComponent<NavMeshAgent>().speed = playerRunningSpeed;

        //SetAnimController(selfPlayer, "Humanoid"); // Update Animator
        if (selfPlayer.GetComponentInChildren<NavMeshObstacle>().enabled)
            selfPlayer.GetComponentInChildren<NavMeshObstacle>().enabled = false;

        yield return new WaitForSeconds(0.1f);

        // Assign Agent
        NavMeshAgent agent; 
        if (selfPlayer.GetComponent<NavMeshAgent>() != null)
        {
            selfPlayer.GetComponent<NavMeshAgent>().enabled = true;
            agent = selfPlayer.GetComponent<NavMeshAgent>();
        }
        else agent = selfPlayer.AddComponent<NavMeshAgent>();

        //Take Character reference 
        AINavigation character;
        if (selfPlayer.GetComponent<AINavigation>() != null)
            character = selfPlayer.GetComponent<AINavigation>();
        else
            character = selfPlayer.AddComponent<AINavigation>();

        // Set Destination 
        agent.updateRotation = false;
        agent.SetDestination(destinationPosition);
        StartCoroutine(Move(agent, character, destinationPosition));

        // Debug.Log("Here 3");
        
        //SetAnimController(selfPlayer, "Movement");
        //StartCoroutine(MovementLerp(selfPlayer, destinationPosition, lookAt));
        yield return null;
        
        if (stopMovement)
        {
            Debug.Log("in here123");
            stopMovement = false;
            // selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 0);
            // selfPlayer.GetComponent<Animator>().SetFloat("VelX", 0);
            selfPlayer.GetComponent<Animator>().SetFloat("Forward", 0);
            // selfPlayer.GetComponent<NavMeshAgent>().enabled = false; // Deactivate Agent 
            // selfPlayer.GetComponentInChildren<NavMeshObstacle>().enabled = true;
            StopCoroutine(Move(agent, character, destinationPosition));
            StopCoroutine(MoveToPosHelper(destinationPosition, lookAt));
        }
    }
    
    IEnumerator Move(NavMeshAgent agent, AINavigation character, Vector3 Destiny)
    {
        while (agent.SetDestination(Destiny))
        {
            if (agent.remainingDistance > agent.stoppingDistance)
                character.Move(agent.desiredVelocity, false, false);
            else
                character.Move(Vector3.zero, false, false);
            yield return null;
        }

        GameObject selfPlayer = this.gameObject;

        selfPlayer.GetComponent<NavMeshAgent>().enabled = false; // Deactivate Agent 
        selfPlayer.GetComponentInChildren<NavMeshObstacle>().enabled = true;
    }
    
    IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

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

                //Condition to Stop Continuous Movement
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

                //Condition to Stop Continuous Movement
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

    private IEnumerator DribbleLerp(Vector3 final)
    {
        GameObject selfPlayer = this.gameObject;
        Vector3 init = selfPlayer.transform.position;
        final.y = init.y; // Don't Update Y Coordinate
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

            //Condition to Stop Continuous Movement
            if (stopMovement)
            {
                // stopMovement = false;
                selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 0);
                StopCoroutine(DribbleLerp(final));
            }
        }
    }

    private IEnumerator BallHeader(Vector3 final, float aerialOffset)
    {
        GameObject selfPlayer = this.gameObject;
        Vector3 init = selfPlayer.transform.position;
        //Vector3 final = finalObj.transform.position;
        Vector2 unitVector = new(selfPlayer.transform.forward.x, selfPlayer.transform.forward.z);

        Vector2 objectAPosition = new(init.x, init.z); ;
        Vector2 objectBPosition = new(final.x, final.z); ;

        Vector2 shiftedObjectBPosition = objectBPosition - objectAPosition;

        float angle = Mathf.Atan2(unitVector.x, unitVector.y);

        // rotate object B wrt object A
        float rotatedXB = shiftedObjectBPosition.x * Mathf.Cos(angle) - shiftedObjectBPosition.y * Mathf.Sin(angle);
        float rotatedYB = shiftedObjectBPosition.x * Mathf.Sin(angle) + shiftedObjectBPosition.y * Mathf.Cos(angle);

        float newAngle = Mathf.Atan2(rotatedXB, rotatedYB);
        float velZ = Mathf.Cos(newAngle);
        float velX = Mathf.Sin(newAngle);

        selfPlayer.GetComponent<Animator>().SetFloat("VelZ", velZ);
        selfPlayer.GetComponent<Animator>().SetFloat("VelX", velX);

        yield return new WaitForSeconds(1.1f);

        // move ball
        MoveBall(final, aerialOffset, airPassForce);

        yield return new WaitForSeconds(WaitTime() - 1f);

        selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 0);
        selfPlayer.GetComponent<Animator>().SetFloat("VelX", 0);

    }

    #endregion

    #region Helper Methods

    private float WaitTime()
    {
        float animationLength = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        float animationSpeed = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).speed;
        float delay = animationLength * animationSpeed;
        return delay;
    }

    private void SetAnimController(string controllerHashCode)
    {
        GameObject selfPlayer = this.gameObject;
        string currAnimationController = selfPlayer.GetComponent<Animator>().runtimeAnimatorController.name;
        if (currAnimationController != controllerHashCode)
        {
            RuntimeAnimatorController newController = Resources.Load("Animation/" + controllerHashCode, typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            selfPlayer.GetComponent<Animator>().runtimeAnimatorController = newController;
        }
    }

    private float RelativeQuadrant(Vector3 init, Vector3 final, Vector2 unitVector)
    {
        // Shift the origin to object A
        Vector2 objectAPosition = new(init.x, init.z); ;
        Vector2 objectBPosition = new(final.x, final.z); ;

        Vector2 shiftedObjectBPosition = objectBPosition - objectAPosition;

        float angle = Mathf.Atan2(unitVector.x, unitVector.y);

        // rotate object B wrt object A
        float rotatedXB = shiftedObjectBPosition.x * Mathf.Cos(angle) - shiftedObjectBPosition.y * Mathf.Sin(angle);
        float rotatedYB = shiftedObjectBPosition.x * Mathf.Sin(angle) + shiftedObjectBPosition.y * Mathf.Cos(angle);

        // Determine the quadrant of object B
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

    // TODO: should be an animation event for passes/kicks/shoots
    private void MoveBall(Vector3 xzDir, float yDir, float forceMagnitude)
    {
        Vector3 ballMotionVector = xzDir - soccerBall.transform.position;
        Vector3 forceDirection = new(ballMotionVector.x, yDir, ballMotionVector.z);
        Debug.Log("force vector: " + forceDirection);
        GameObject ballTriggerCollider = GameObject.FindGameObjectWithTag("BallTrigger");
        ballTriggerCollider.SetActive(false);
        soccerBall.GetComponent<BallInteraction>().InRangeofPlayer = false;
        // soccerBall.GetComponent<Rigidbody>().AddForce(forceDirection * forceMagnitude * forceFactor);
        soccerBall.GetComponent<Rigidbody>().AddForce(forceDirection * 10f * forceFactor);
        Debug.Log("in moveball");
        Debug.Log("force:" + forceDirection * forceMagnitude * forceFactor);
    }

    private IEnumerator LookTowards(Vector3 destinationPosition, string keyCode)
    {
        GameObject selfPlayer = this.gameObject;
        Vector3 init = selfPlayer.transform.position;
        Vector2 finalFacingDirection = new(destinationPosition.x - init.x, destinationPosition.z - init.z);
        Vector2 initialFacingDirection = new(selfPlayer.transform.forward.x, selfPlayer.transform.forward.z);

        float angle = Vector2.Angle(initialFacingDirection, finalFacingDirection);
        float rotationDirection = Mathf.Sign(Vector3.Dot(initialFacingDirection, finalFacingDirection));

        yield return StartCoroutine(RotateCoroutine(angle, rotationDirection, rotationDuration));

        // play animation after rotating
        if (keyCode != null)
        {
            selfPlayer.GetComponent<Animator>().SetTrigger(keyCode);
        }
    }

    private IEnumerator RotateCoroutine(float angle, float rotationDirection, float duration)
    {
        GameObject selfPlayer = this.gameObject;
        float currentRotation = 0.0f;
        float rotationPerSecond = angle / duration;

        while(currentRotation < angle)
        {
            // Calculate the incremental rotation for this frame
            float rotationIncrement = rotationPerSecond * Time.deltaTime;
            currentRotation += rotationIncrement;

            if (rotationDirection == -1)
                selfPlayer.transform.Rotate(Vector3.up, rotationIncrement);
            else if (rotationDirection == 1)
                selfPlayer.transform.Rotate(Vector3.down, rotationIncrement);

            yield return null;
        }

        yield return null;
    }

    IEnumerator MoveBallAfterAnimationhalfway(Vector3 destinationPosition, string ballProjectileHeight,float shootForce)
    {
        yield return new WaitForSeconds(1f);
        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), shootForce);
    }

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