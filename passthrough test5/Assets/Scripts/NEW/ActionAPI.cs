using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.ShaderData;

#region For Mathematical Reference
// parabola Equation if used (x-5)^2 = -4(25/8)(y-2) => y = -2/25(x^2 - 10x)  (in this Max Distance 10)
// parabola Equation if used (x-(d/2))^2 = -4((d/2)^2/8)(y-2)) => y = (8 (d x - x^2))/d^2
#endregion

public class ActionAPI : MonoBehaviour
{
    // [SerializeField] Animator playerAnimator;
    [SerializeField] float playerRunningSpeed = 2f;
    [SerializeField] float timeDuration = 5f;

    [SerializeField] GameObject soccerBall;

    bool stopMovement = false;
    string transitionTo = "t";

    float forceFactor = 1f;
    float weakPassForce = 1f;
    float strongPassForce = 2f;
    float airPassForce = 3f;
    float chipForce = 1.5f;
    float shootForce = 4f;

    float rotationDuration = 0.8f;

    private void Start()
    {
        // playerAnimator = this.GetComponent<Animator>();

        //if (gameObject.tag == "Goalkeeper") SetAnimController("GoalKeeper");

    }
    //void UnitTestMovement(bool lookAt)
    //{
    //    Vector2 unitVector = new Vector2(0, 0);

    //    unitVector.x = Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.Deg2Rad);
    //    unitVector.y = Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.Deg2Rad);

    //    MoveFromOnePositionToAnother(init.gameObject, final.gameObject, unitVector, lookAt);
    //}

    #region API Methods for BlendTrees
    public void MoveFromOnePositionToAnother(GameObject selfPlayer, Vector3 destinationPosition, bool lookAt)
    {
        SetAnimController(selfPlayer, "Movement");
        StartCoroutine(MovementLerp(selfPlayer, destinationPosition, lookAt));
    }

    public void DribbleFromOnePositionToAnother(GameObject selfPlayer, Vector3 destinationPosition)
    {
        SetAnimController(selfPlayer, "Dribbling");
        StartCoroutine(DribbleLerp(selfPlayer, destinationPosition));
    }

    public void BallHeaderShoot(GameObject selfPlayer, GameObject final, float aerialOffset)
    {
        SetAnimController(selfPlayer, "Headers");
        StartCoroutine(BallHeader(selfPlayer, final, aerialOffset));
    }

    #endregion

    #region API Methods for Singleton Animations

    #region PlayersSingletonMethods
    void ReceiveBall(GameObject selfPlayer, Vector3 receiveFrom)
    {
        stopMovement = true;
        LookTowards(selfPlayer, receiveFrom, "Receive");
    }

    void TackleBall(GameObject selfPlayer, Vector3 tackleFrom)
    {
        stopMovement = true;
        LookTowards(selfPlayer, tackleFrom, "Receive");
    }

    void GroundPassSlow(GameObject selfPlayer, Vector3 destinationPosition)
    {
        stopMovement = true;
        LookTowards(selfPlayer, destinationPosition, "GroundPassSlow");
        
        //MoveBall(destinationPosition, 0, weakPassForce);
    }

    void GroundPassFast(GameObject selfPlayer, Vector3 destinationPosition)
    {
        stopMovement = true;
        LookTowards(selfPlayer, destinationPosition, "GroundPassFast");
        
        //MoveBall(destinationPosition, 0, strongPassForce);
    }

    void AirPass(GameObject selfPlayer, Vector3 destinationPosition, float aerialOffset)
    {
        stopMovement = true;

        LookTowards(selfPlayer, destinationPosition, "AirPass");

        //MoveBall(destinationPosition, aerialOffset, airPassForce);
    }

    void ChipLeft(GameObject selfPlayer, Vector3 destinationPosition, float aerialOffset)
    {
        stopMovement = true;
        selfPlayer.GetComponent<Animator>().SetTrigger("ChipLeft");

        //MoveBall(destinationPosition, aerialOffset, chipForce);
    }

    void ChipRight(GameObject selfPlayer, Vector3 destinationPosition, float aerialOffset)
    {
        stopMovement = true;
        selfPlayer.GetComponent<Animator>().SetTrigger("ChipRight");

        //MoveBall(destinationPosition, aerialOffset, chipForce);
    }

    void ChipFront(GameObject selfPlayer, Vector3 destinationPosition, float aerialOffset)
    {
        stopMovement = true;
        LookTowards(selfPlayer, destinationPosition, "ChipFront");

        //MoveBall(destinationPosition, aerialOffset, chipForce);
    }

    void Shoot(GameObject selfPlayer, Vector3 destinationPosition, float aerialOffset = 0)
    {
        stopMovement = true;
        LookTowards(selfPlayer, destinationPosition, "Shoot");

        //MoveBall(destinationPosition, aerialOffset, shootForce);
    }

    void BallThrow(GameObject selfPlayer, Vector3 destinationPosition, float aerialOffset)
    {
        stopMovement = true;
        LookTowards(selfPlayer, destinationPosition, "BallThrow");

        //MoveBall(destinationPosition, aerialOffset, airPassForce);
    }
    #endregion

    #region GoalKeeper SingletonMethods

    void BodyBlockLeftSide(GameObject selfPlayer)
    {
        stopMovement = true;
        selfPlayer.GetComponent<Animator>().SetTrigger("BodyBlockLeftSide");
    }
    void BodyBlockRightSide(GameObject selfPlayer)
    {
        stopMovement = true;
        selfPlayer.GetComponent<Animator>().SetTrigger("BodyBlockRightSide");
    }
    void CatchFastBall(GameObject selfPlayer)
    {
        stopMovement = true;
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchFastBall");
    }
    void CatchStraightUpBall(GameObject selfPlayer)
    {
        stopMovement = true;
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchStraightUpBall");
    }

    //methods for Goalkeeper movvement (lefty or right)

    void CatchSlowBall(GameObject selfPlayer)
    {
        stopMovement = true;
        selfPlayer.GetComponent<Animator>().SetTrigger("CatchSlowBall");
        //-> Ball Interation 
        //StartCoroutine(Trigger(transitionTo + "CatchSlowBall"));

    }
    void DropKickShot(GameObject selfPlayer, Vector3 destinationPosition, float aerialOffset)
    {
        stopMovement = true;
        LookTowards(selfPlayer, destinationPosition, "DropKick");

        //MoveBall(destinationPosition, aerialOffset, airPassForce);
    }
    void OverHandThrow(GameObject selfPlayer, Vector3 destinationPosition, float aerialOffset)
    {
        stopMovement = true;
        LookTowards(selfPlayer, destinationPosition, "OverHandThrow");

        //MoveBall(destinationPosition, aerialOffset, airPassForce);
    }
    void RollingBallPass(GameObject selfPlayer, Vector3 destinationPosition)
    {
        stopMovement = true;
        LookTowards(selfPlayer, destinationPosition, "RollingBallPass");

        //MoveBall(destinationPosition, 0, weakPassForce);
    }
    void PlacingAndLongPass(GameObject selfPlayer, Vector3 destinationPosition, float aerialOffset)
    {
        stopMovement = true;
        LookTowards(selfPlayer, destinationPosition, "PlacingAndLongPass");

        //MoveBall(destinationPosition, aerialOffset, airPassForce);
    }
    void PlacingAndShortPass(GameObject selfPlayer, Vector3 destinationPosition)
    {
        stopMovement = true;
        LookTowards(selfPlayer, destinationPosition, "PlacingAndShortPass");
        
        //MoveBall(destinationPosition, 0, weakPassForce);
    }
    #endregion

    #endregion

    #region Helper Coroutines

    IEnumerator MovementLerp(GameObject selfPlayer, Vector3 final, bool lookAt)
    {
        Vector3 init = selfPlayer.transform.position;
        Vector2 unitVector = new Vector2(selfPlayer.transform.forward.x, selfPlayer.transform.forward.z);

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
            LookTowards(selfPlayer, final, null);

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

    /// <summary> Lerp motion Coroutine that allows player to move from one point to another </summary>
    /// <param name="init">Initial point</param>
    /// <param name="final">Final point</param>
    IEnumerator DribbleLerp(GameObject selfPlayer, Vector3 final)
    {
        Vector3 init = selfPlayer.transform.position;
        // Vector3 final = finalObj.transform.position;
        final.y = init.y; // Don't Update Y Coordinate
        LookTowards(selfPlayer, final, null);

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

    IEnumerator BallHeader(GameObject selfPlayer, GameObject finalObj, float aerialOffset)
    {
        Vector3 init = selfPlayer.transform.position;
        Vector3 final = finalObj.transform.position;
        Vector2 unitVector = new Vector2(selfPlayer.transform.forward.x, selfPlayer.transform.forward.z);

        Vector2 objectAPosition = new Vector2(init.x, init.z); ;
        Vector2 objectBPosition = new Vector2(final.x, final.z); ;

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
        //MoveBall(final, aerialOffset, airPassForce);

        yield return new WaitForSeconds(WaitTime() - 1f);

        selfPlayer.GetComponent<Animator>().SetFloat("VelZ", 0);
        selfPlayer.GetComponent<Animator>().SetFloat("VelX", 0);

    }

    ///// <summary> Triggering Singleton Animations </summary>
    ///// <param name="keyCodeHash">Animation name</param>
    //IEnumerator Trigger(string keyCodeHash)
    //{
    //    keyCodeHash = keyCodeHash.Substring(1);

    //    selfPlayer.GetComponent<Animator>().SetBool(keyCodeHash, true);
    //    yield return new WaitForSeconds(WaitTime());
    //    selfPlayer.GetComponent<Animator>().SetBool(keyCodeHash, false);

    //    stopMovement = false;
    //}

    #endregion

    #region Helper Methods

    float WaitTime()
    {
        float animationLength = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        float animationSpeed = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).speed;
        float delay = animationLength * animationSpeed;
        return delay;
    }

    void SetAnimController(GameObject selfPlayer, string controllerHashCode)
    {
        string currAnimationController = selfPlayer.GetComponent<Animator>().runtimeAnimatorController.name;
        if (currAnimationController != controllerHashCode)
        {
            RuntimeAnimatorController newController = Resources.Load("Animation/" + controllerHashCode, typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            selfPlayer.GetComponent<Animator>().runtimeAnimatorController = newController;
        }
    }

    float RelativeQuadrant(Vector3 init, Vector3 final, Vector2 unitVector)
    {
        // Shift the origin to object A
        Vector2 objectAPosition = new Vector2(init.x, init.z); ;
        Vector2 objectBPosition = new Vector2(final.x, final.z); ;

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

    void MoveBall(Vector3 xzDir, float yDir, float forceMagnitude)
    {
        Vector3 forceDirection = new Vector3(xzDir.x, yDir, xzDir.z);
        soccerBall.GetComponent<Rigidbody>().AddForce(forceDirection * forceMagnitude * forceFactor);
    }

    IEnumerator LookTowards(GameObject selfPlayer, Vector3 destinationPosition, string keyCode)
    {
        Vector3 init = selfPlayer.transform.position;
        Vector2 finalFacingDirection = new Vector2(destinationPosition.x - init.x, destinationPosition.z - init.z);
        Vector2 initialFacingDirection = new Vector2(selfPlayer.transform.forward.x, selfPlayer.transform.forward.z);

        float angle = Vector2.Angle(initialFacingDirection, finalFacingDirection);
        float rotationDirection = Mathf.Sign(Vector3.Dot(initialFacingDirection, finalFacingDirection));

        yield return StartCoroutine(RotateCoroutine(selfPlayer, angle, rotationDirection, rotationDuration));

        // play animation after rotating
        if (keyCode != null) selfPlayer.GetComponent<Animator>().SetTrigger(keyCode);
    }

    IEnumerator RotateCoroutine(GameObject selfPlayer, float angle, float rotationDirection, float duration)
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
    #endregion
}