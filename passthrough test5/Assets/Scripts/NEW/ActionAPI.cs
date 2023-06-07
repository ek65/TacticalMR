using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.ShaderData;

/*
 * ToDo - 
 * 1. Add Input Arguments in Singleton Methods - Yash --------------------------------> DONE
 * 2. Update Movement Script wrt boolean based on LookAt() - Mihir
 * 3. Add Actons :- 1. Pass a Ball to Teammate by throwing 
 *               2. Blocking shots
 *               for third point we Need to Find the Required Animation and then Implement it 
 * 4. Blend Tree for Goolkeeper and APIs
 * 
 * 
 * NEW
 *  Test singleton animations with ball
 *  new function for look at, which is smoother
 */


public class ActionAPI : MonoBehaviour
{
    [SerializeField] Animator playerAnimator;
    [SerializeField] float playerRunningSpeed = 2f;
    [SerializeField] float timeDuration = 5f;

    [SerializeField] Transform init;
    [SerializeField] Transform final;
    [SerializeField] GameObject soccerBall;

    bool stopMovement = false;
    string transitionTo = "t";

    float forceFactor = 12.5f;
    float weakPassForce = 1f;
    float strongPassForce = 2f;
    float airPassForce = 3f;
    float chipForce = 1.5f;
    float shootForce = 4f;

    private void Start()
    {
        playerAnimator = this.GetComponent<Animator>();
        UnitTestHeader();
    }

    #region Unit Tests
    void UnitTestMovement()
    {
        Vector2 unitVector = new Vector2(0, 0);

        unitVector.x = Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.Deg2Rad);
        unitVector.y = Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.Deg2Rad);

        MoveFromOnePositionToAnother(init.position, final.position, unitVector);
    }

    void UnitTestDribble()
    {
        DribbleFromOnePositionToAnother(init.position, final.position);
    }

    void UnitTestHeader()
    {
        Debug.Log("testing header");
        Vector2 unitVector = new Vector2(0, 0);

        unitVector.x = Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.Deg2Rad);
        unitVector.y = Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.Deg2Rad);

        BallHeaderShoot(init.position, final.position, unitVector, 0);
    }

    #endregion

    #region API Methods for BlendTrees

    /// <summary> Method/API to Move player from One Position to another </summary>
    /// <param name="init">Initial Position</param>
    /// <param name="final">Final Position</param>
    /// <param name="unitVector">Unit Vector where the player is facing</param>
    public void MoveFromOnePositionToAnother(Vector3 init, Vector3 final, Vector2 unitVector)
    {
        SetAnimController("Movement");
        StartCoroutine(MovementLerp(init, final, unitVector));
    }

    public void DribbleFromOnePositionToAnother(Vector3 init, Vector3 final)
    {
        SetAnimController("Dribbling");
        StartCoroutine(DribbleLerp(init, final));
    }

    public void BallHeaderShoot(Vector3 init, Vector3 final, Vector2 unitVector, float aerialOffset)
    {
        SetAnimController("Headers");
        StartCoroutine(BallHeader(init, final, unitVector, aerialOffset));
    }

    #endregion

    #region API Methods for Singleton Animations

    void ReceiveBall()
    {
        stopMovement = true;
        StartCoroutine(Trigger(transitionTo + "Receive"));
    }

    void TackleBall()
    {
        stopMovement = true;
        StartCoroutine(Trigger(transitionTo + "StrongTackle"));
    }

    void GroundPassSlow(Vector3 passTo)
    {
        stopMovement = true;
        StartCoroutine(Trigger(transitionTo + "GroundPassSlow"));

        transform.LookAt(passTo);
        MoveBall(passTo, 0, weakPassForce);
    }

    void GroundPassFast(Vector3 passTo)
    {
        stopMovement = true;
        StartCoroutine(Trigger(transitionTo + "GroundPassFast"));

        transform.LookAt(passTo);
        MoveBall(passTo, 0, strongPassForce);
    }

    void AirPass(Vector3 passTo, float aerialOffset)
    {
        stopMovement = true;
        StartCoroutine(Trigger(transitionTo + "AirPass"));

        transform.LookAt(passTo);
        MoveBall(passTo, aerialOffset, airPassForce);
    }

    void ChipLeft(Vector3 passTo, float aerialOffset)
    {
        stopMovement = true;
        StartCoroutine(Trigger(transitionTo + "ChipLeft"));

        MoveBall(passTo, aerialOffset, chipForce);
    }

    void ChipRight(Vector3 passTo, float aerialOffset)
    {
        stopMovement = true;
        StartCoroutine(Trigger(transitionTo + "ChipRight"));

        MoveBall(passTo, aerialOffset, chipForce);
    }

    void ChipFront(Vector3 passTo, float aerialOffset)
    {
        stopMovement = true;
        StartCoroutine(Trigger(transitionTo + "ChipFront"));

        transform.LookAt(passTo);
        MoveBall(passTo, aerialOffset, chipForce);
    }

    void Shoot(Vector3 shootAt, float aerialOffset = 0)
    {
        stopMovement = true;
        StartCoroutine(Trigger(transitionTo + "Shoot"));

        transform.LookAt(shootAt);
        MoveBall(shootAt, aerialOffset, shootForce);
    }

    #endregion

    #region Helper Coroutines

    /// <summary> Lerp motion Coroutine that allows player to move from one point to another </summary>
    /// <param name="init">Initial point</param>
    /// <param name="final">Final point</param>
    /// <param name="unitVector">Unit Vector where the player is facing</param>
    IEnumerator MovementLerp(Vector3 init, Vector3 final, Vector2 unitVector)
    {
        float quadrant = RelativeQuadrant(init, final, unitVector);

        int xSign = 0;
        int zSign = 0;

        switch (quadrant)
        {
            case 0.5f : xSign = 1; zSign = 0; break;
            case 1.0f : xSign = 1; zSign = 1; break;
            case 1.5f : xSign = 0; zSign = 1; break;
            case 2.0f : xSign = -1; zSign = 1; break;
            case 2.5f : xSign = -1; zSign = 0; break;
            case 3.0f : xSign = -1; zSign = -1; break;
            case 3.5f : xSign = 0; zSign = -1; break;
            case 4.0f : xSign = 1; zSign = -1; break;
        }

        Debug.Log(xSign + ", " + zSign); 

        final.y = init.y;

        float timeElapsed = 0;

        float distance = Vector3.Distance(init, final);

        float distanceX = Mathf.Abs(init.x - final.x);
        float distanceZ = Mathf.Abs(init.z - final.z);

        timeDuration = 2f * distance / playerRunningSpeed;

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

            playerAnimator.SetFloat("VelX", transitionValueX);
            playerAnimator.SetFloat("VelZ", transitionValueZ);

            transform.position = Vector3.Lerp(init, final, t);
            timeElapsed += Time.deltaTime;
            yield return null;

            //Condition to Stop Continuous Movement
            if (stopMovement)
            {
                stopMovement = false;
                playerAnimator.SetFloat("VelZ", 0);
                playerAnimator.SetFloat("VelX", 0);
                StopCoroutine(MovementLerp(init, transform.position, unitVector));
            }
        }

        yield return null;
    }

    /// <summary> Lerp motion Coroutine that allows player to move from one point to another </summary>
    /// <param name="init">Initial point</param>
    /// <param name="final">Final point</param>
    IEnumerator DribbleLerp(Vector3 init, Vector3 final)
    {
        final.y = init.y; // Don't Update Y Coordinate
        transform.LookAt(final);  // for Look At final position

        float timeElapsed = 0;
        float distance = Vector3.Distance(init, final);
        timeDuration = 2f * distance / playerRunningSpeed;

        while (timeElapsed < timeDuration)
        {
            float t = timeElapsed / timeDuration;
            t = t * t * (3f - 2f * t);
            float UpdatedDistance = Vector3.Distance(init, transform.position);

            #region For Mathematical Reference
            // parabola Equation if used (x-5)^2 = -4(25/8)(y-2) => y = -2/25(x^2 - 10x)  (in this Max Distance 10)
            // parabola Equation if used (x-(d/2))^2 = -4((d/2)^2/8)(y-2)) => y = (8 (d x - x^2))/d^2
            #endregion

            float transitionValue = (8f * (distance * (UpdatedDistance) - Mathf.Pow(UpdatedDistance, 2)) / Mathf.Pow(distance, 2));

            playerAnimator.SetFloat("VelZ", transitionValue);

            transform.position = Vector3.Lerp(init, final, t);
            timeElapsed += Time.deltaTime;
            yield return null;

            //Condition to Stop Continuous Movement
            if (stopMovement)
            {
                // stopMovement = false;
                playerAnimator.SetFloat("VelZ", 0);
                StopCoroutine(DribbleLerp(init, transform.position));
            }
        }
    }

    IEnumerator BallHeader(Vector3 init, Vector3 final, Vector2 unitVector, float aerialOffset)
    {
        // play header animation
        float angle = Mathf.Atan2(unitVector.x, unitVector.y);
        float velZ = Mathf.Cos(angle);
        float velX = Mathf.Sin(angle);

        playerAnimator.SetFloat("VelZ", velZ);
        playerAnimator.SetFloat("VelX", velX);
        
        // move ball
        MoveBall(final, aerialOffset, airPassForce);

        yield return new WaitForSeconds(WaitTime());

        playerAnimator.SetFloat("VelZ", 0);
        playerAnimator.SetFloat("VelX", 0);

    }

    /// <summary> Triggering Singleton Animations </summary>
    /// <param name="keyCodeHash">Animation name</param>
    IEnumerator Trigger(string keyCodeHash)
    {
        keyCodeHash = keyCodeHash.Substring(1);

        playerAnimator.SetBool(keyCodeHash, true);
        yield return new WaitForSeconds(WaitTime());
        playerAnimator.SetBool(keyCodeHash, false);

        stopMovement = false;
    }

    #endregion

    #region Helper Methods

    float WaitTime()
    {
        float animationLength = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        float animationSpeed = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).speed;
        float delay = animationLength * animationSpeed;
        return delay;
    }

    void SetAnimController(string controllerHashCode)
    {
        string currAnimationController = playerAnimator.runtimeAnimatorController.name;
        if (currAnimationController != controllerHashCode)
        {
            RuntimeAnimatorController newController = Resources.Load("Animation/" + controllerHashCode, typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            playerAnimator.runtimeAnimatorController = newController;
        }
    }

    float RelativeQuadrant(Vector3 init, Vector3 final, Vector2 unitVector)
    {
        // Shift the origin to object A
        Vector2 objectAPosition = new Vector2(init.x, init.z); ;
        Vector2 objectBPosition = new Vector2(final.x, final.z); ;

        Vector2 shiftedObjectBPosition = objectBPosition - objectAPosition;

        float angle = Mathf.Atan2(unitVector.x, unitVector.y);

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

    #endregion
}