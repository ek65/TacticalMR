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

    bool stopMovement = false;
    string transitionTo = "t";

    float forceFactor = 7f;
    float weakPassForce = 1f;
    float strongPassForce = 2f;
    float airPassForce = 3f;
    float chipForce = 1.5f;
    float shootForce = 4f;

    float rotationDuration = 0.4f;



    #region API Methods for BlendTrees
    public void MoveTo(Vector3 destinationPosition, bool lookAt = true)
    {
        // Debug.Log("here");

        GameObject selfPlayer = this.gameObject;

        selfPlayer.GetComponent<NavMeshAgent>().speed = playerRunningSpeed;

        //SetAnimController(selfPlayer, "Humanoid"); // Update Animator
        if (selfPlayer.GetComponentInChildren<NavMeshObstacle>().enabled)
            selfPlayer.GetComponentInChildren<NavMeshObstacle>().enabled = false;

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
        StartCoroutine(Move(selfPlayer, agent,character, destinationPosition));

        // Debug.Log("Here 3");
        
        //SetAnimController(selfPlayer, "Movement");
        //StartCoroutine(MovementLerp(selfPlayer, destinationPosition, lookAt));
    }
    public void DribbleFromOnePositionToAnother(GameObject selfPlayer, Vector3 destinationPosition)
    {
        SetAnimController(selfPlayer, "Dribbling");
        StartCoroutine(DribbleLerp(selfPlayer, destinationPosition));
    }
    public void BallHeaderShoot(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight)
    {
        SetAnimController(selfPlayer, "Headers");
        StartCoroutine(BallHeader(selfPlayer, destinationPosition, VerticalForce(ballProjectileHeight)));
    }
    #endregion

    #region API Methods for Singleton Animations

    #region PlayersSingletonMethods
    public void ReceiveBall(GameObject selfPlayer, Vector3 receiveFrom)
    {
        SetAnimController(selfPlayer, "Movement");
        stopMovement = true;
        StartCoroutine(LookTowards(selfPlayer, receiveFrom, "Receive"));
    }

    public void TackleBall(GameObject selfPlayer, Vector3 tackleFrom)
    {
        SetAnimController(selfPlayer, "Movement");
        stopMovement = true;
        StartCoroutine(LookTowards(selfPlayer, tackleFrom, "Receive"));
    }

    public void GroundPassSlow(GameObject selfPlayer, Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "Dribbling");
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "GroundPassSlow"));
        
        MoveBall(destinationPosition, 0, weakPassForce);
    }

    public void GroundPassFast(GameObject selfPlayer, Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "Dribbling");
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "GroundPassFast"));
        
        MoveBall(destinationPosition, 0, strongPassForce);
    }

    public void AirPass(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "Dribbling");
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "AirPass"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }

    public void ChipLeft(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "Dribbling");
        selfPlayer.GetComponent<Animator>().SetTrigger("ChipLeft");

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), chipForce);
    }

    public void ChipRight(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "Dribbling");
        selfPlayer.GetComponent<Animator>().SetTrigger("ChipRight");

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), chipForce);
    }

    public void ChipFront(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "Dribbling");
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "ChipFront"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), chipForce);
    }

    public void Shoot(GameObject selfPlayer, Vector3 destinationPosition, string destinationZone)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "Dribbling");
        
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "Shoot"));

        
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

    public void BallThrow(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "Dribbling");
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "BallThrow"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }
    #endregion

    #region GoalKeeper SingletonMethods

    public void IdleWithBallInHand(GameObject selfPlayer)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetFloat("positionY", 1f);
    }

    public void BodyBlockLeftSide(GameObject selfPlayer)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("BodyBlockLeftSide");
    }
    public void BodyBlockRightSide(GameObject selfPlayer)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("BodyBlockRightSide");
    }
    public void CatchGroundBall(GameObject selfPlayer)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchGroundBall");
    }
    public void CatchBallInTheAir(GameObject selfPlayer)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchBallInTheAir");
    }

    //methods for Goalkeeper movement (lefty or right)

    public void CatchSlowBall(GameObject selfPlayer)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchSlowBall");
        //-> Ball Interation 
        //StartCoroutine(Trigger(transitionTo + "CatchSlowBall"));

    }
    public void DropKickShot(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "DropKick"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }
    public void OverHandThrow(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "OverHandThrow"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }
    public void RollingBallPass(GameObject selfPlayer, Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "RollingBallPass"));

        MoveBall(destinationPosition, 0, weakPassForce);
    }
    public void PlacingAndLongPass(GameObject selfPlayer, Vector3 destinationPosition, string ballProjectileHeight)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "PlacingAndLongPass"));

        MoveBall(destinationPosition, VerticalForce(ballProjectileHeight), airPassForce);
    }
    public void PlacingAndShortPass(GameObject selfPlayer, Vector3 destinationPosition)
    {
        stopMovement = true;
        SetAnimController(selfPlayer, "GoalKeeper");
        StartCoroutine(LookTowards(selfPlayer, destinationPosition, "PlacingAndShortPass"));
        
        MoveBall(destinationPosition, 0, weakPassForce);
    }
    #endregion

    #endregion

    #region Helper Coroutines


    IEnumerator Move(GameObject selfPlayer,NavMeshAgent agent, AINavigation character, Vector3 Destiny)
    {
        while (agent.SetDestination(Destiny))
        {
            if (agent.remainingDistance > agent.stoppingDistance)
                character.Move(agent.desiredVelocity, false, false);
            else
                character.Move(Vector3.zero, false, false);
            yield return null;
        }

        selfPlayer.GetComponent<NavMeshAgent>().enabled = false; // Deactivate Agent 
        selfPlayer.GetComponentInChildren<NavMeshObstacle>().enabled = true;
    }

    private IEnumerator MovementLerp(GameObject selfPlayer, Vector3 final, bool lookAt)
    {
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
                    StopCoroutine(MovementLerp(selfPlayer, final, lookAt));
                }
            }
        }
        else
        {
            StartCoroutine(LookTowards(selfPlayer, final, null));

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
                    StopCoroutine(MovementLerp(selfPlayer, final, lookAt));
                }
            }
        }
        yield return null;
    }

    private IEnumerator DribbleLerp(GameObject selfPlayer, Vector3 final)
    {
        Vector3 init = selfPlayer.transform.position;
        final.y = init.y; // Don't Update Y Coordinate
        StartCoroutine(LookTowards(selfPlayer, final, null));

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
                StopCoroutine(DribbleLerp(selfPlayer, final));
            }
        }
    }

    private IEnumerator BallHeader(GameObject selfPlayer, Vector3 final, float aerialOffset)
    {
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

    private void SetAnimController(GameObject selfPlayer, string controllerHashCode)
    {
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

    private void MoveBall(Vector3 xzDir, float yDir, float forceMagnitude)
    {
        Vector3 ballMotionVector = xzDir - soccerBall.transform.position;
        Vector3 forceDirection = new(ballMotionVector.x, yDir, ballMotionVector.z);
        Debug.Log("force vector: " + forceDirection);
        soccerBall.GetComponent<Rigidbody>().AddForce(forceDirection * forceMagnitude * forceFactor);
    }

    private IEnumerator LookTowards(GameObject selfPlayer, Vector3 destinationPosition, string keyCode)
    {
        Vector3 init = selfPlayer.transform.position;
        Vector2 finalFacingDirection = new(destinationPosition.x - init.x, destinationPosition.z - init.z);
        Vector2 initialFacingDirection = new(selfPlayer.transform.forward.x, selfPlayer.transform.forward.z);

        float angle = Vector2.Angle(initialFacingDirection, finalFacingDirection);
        float rotationDirection = Mathf.Sign(Vector3.Dot(initialFacingDirection, finalFacingDirection));

        yield return StartCoroutine(RotateCoroutine(selfPlayer, angle, rotationDirection, rotationDuration));

        // play animation after rotating
        if (keyCode != null) selfPlayer.GetComponent<Animator>().SetTrigger(keyCode);
    }

    private IEnumerator RotateCoroutine(GameObject selfPlayer, float angle, float rotationDirection, float duration)
    {
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