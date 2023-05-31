using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    front,
    back,
    left,
    right
}

/// <summary>
/// front, back, left, right
/// front and right
/// front and left
/// back and right
/// back and left
/// </summary>

public class ActionAPI : MonoBehaviour
{
    [SerializeField] Animator playerAnimator;
    [SerializeField] float playerRunningSpeed = 2f;
    [SerializeField] float timeDuration = 5f;

    [SerializeField] Transform init;
    [SerializeField] Transform final;


    bool stopMovement = false;

    private void Start()
    {
        playerAnimator = this.GetComponent<Animator>();

        init = transform;

        StartCoroutine(LerpDiagonally(init.position, final.position));

        //Vector3 currPos = this.transform.position;
        //Vector3 finalPos = new Vector3(currPos.x + 10f, currPos.y, currPos.z + 10f);
        //DribbleFromOnePositionToAnother(currPos, finalPos);
    }

    #region Methods 

    /// <summary>
    /// Method/API to Move player from One Position to another
    /// </summary>
    /// <param name="init">Initial Position</param>
    /// <param name="final">Final Position</param>
    public void MoveFromOnePositionToAnother(Vector3 init, Vector3 final)
    {
        SetAnimController("Movement");
        StartCoroutine(Lerp(init, final));
    }

    /// <summary>
    /// Straf Jogging Method along Either Direction left or Right
    /// </summary>
    /// <param name="init">Initial Position</param>
    /// <param name="final">Final Position</param>
    /// <param name="dir">Direction Left or Right</param>
    void StrafJog(Vector3 init, Vector3 final, Direction dir)
    {
        SetAnimController("Movement");
        StartCoroutine(Lerp(init, final, dir));
    }

    /// <summary>
    /// Jog Backward rectilinear Movement
    /// </summary>
    /// <param name="init"></param>
    /// <param name="final"></param>
    /// <param name="dir"></param>
    void MoveBack(Vector3 init, Vector3 final)
    {
        Direction dir = Direction.back;
        SetAnimController("Movement");
        StartCoroutine(Lerp(init, final, dir));
    }

    /// <summary>
    /// Dribbling between two points 
    /// </summary>
    /// <param name="init">Initial(Starting) position</param>
    /// <param name="final">Final(End) position</param>
    public void DribbleFromOnePositionToAnother(Vector3 init, Vector3 final)
    {
        SetAnimController("Dribbling");
        StartCoroutine(Lerp(init, final));
    }

    void ReceiveBall()
    {
        stopMovement = true;
        StartCoroutine(Trigger("Receive"));
    }

    void TackleBall()
    {
        stopMovement = true;
        StartCoroutine(Trigger("StrongTackle"));
    }

    void GroundPassSlow()
    {
        stopMovement = true;
        StartCoroutine(Trigger("GroundPassSlow"));
    }

    void GroundPassFast()
    {
        stopMovement = true;
        StartCoroutine(Trigger("GroundPassFast"));
    }

    void AirPass()
    {
        stopMovement = true;
        StartCoroutine(Trigger("AirPass"));
    }

    void ChipSideways()
    {
        stopMovement = true;
        StartCoroutine(Trigger("ChipSideways"));
    }

    void ChipFront()
    {
        stopMovement = true;
        StartCoroutine(Trigger("ChipFront"));
    }

    void Kick()
    {
        stopMovement = true;
        StartCoroutine(Trigger("Kick"));
    }

    #endregion

    #region Coroutine

    /// <summary>
    /// Lerp motion Coroutine that allows player to move from one point to another
    /// </summary>
    /// <param name="init">Initial point</param>
    /// <param name="final">Final point</param>
    /// <returns></returns>
    IEnumerator Lerp(Vector3 init, Vector3 final)
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
                StopCoroutine(Lerp(init,transform.position));
            }
        }
    }

    /// <summary>
    /// This motion is along the strafe LEft or right. 
    /// Lerp motion Coroutine that allows player to move from one point to another.
    /// </summary>
    /// <param name="init">Initial Point</param>
    /// <param name="final">Final Point</param>
    /// <param name="dir">Direction (Left or Right)</param>
    /// <returns></returns>
    IEnumerator Lerp(Vector3 init, Vector3 final,Direction dir)
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


            // Strafe jogging along left
            if (dir == Direction.left) playerAnimator.SetFloat("VelX", -2);
            
            // Strafe jogging along Right
            if (dir == Direction.right) playerAnimator.SetFloat("VelX", 2);

            // Jog Backward 
            if (dir == Direction.back) playerAnimator.SetFloat("VelZ", -2);

            transform.position = Vector3.Lerp(init, final, t);
            timeElapsed += Time.deltaTime;
            yield return null;

            //Condition to Stop Continuous Movement
            if (stopMovement)
            {
                stopMovement = false;
                playerAnimator.SetFloat("VelZ", 0);
                StopCoroutine(Lerp(init, transform.position));
            }
        }
    }

    /// <summary>
    /// Digonal Lerping - Incomplete
    /// </summary>
    /// <param name="init"></param>
    /// <param name="final"></param>
    /// <returns></returns>
    IEnumerator LerpDiagonally(Vector3 init, Vector3 final)
    {
        final.y = init.y; // Don't Update Y Coordinate

        //transform.LookAt(final);  // for Look At final position

        float timeElapsed = 0;
        
        float distance = Vector3.Distance(init, final);
        
        float distanceX = Mathf.Abs(init.x - final.x);
        float distanceZ  = Mathf.Abs(init.z - final.z);
        Debug.Log("Distance wrt Axis - " + distanceX + " : " + distanceZ);

        timeDuration = 2f * distance / playerRunningSpeed;

        while (timeElapsed < timeDuration)
        {
            float t = timeElapsed / timeDuration;
            t = t * t * (3f - 2f * t);
            
            //float UpdatedDistance = Vector3.Distance(init, transform.position);

            float UpdatedDistanceX = Mathf.Abs(init.x - transform.position.x);
            float UpdatedDistanceZ = Mathf.Abs(init.z - transform.position.z);

            float transitionValueZ;
            if (distanceZ != 0)
                transitionValueZ = (8f * (distanceZ * (UpdatedDistanceZ) - Mathf.Pow(UpdatedDistanceZ, 2)) / Mathf.Pow(distanceZ, 2));
            else
                transitionValueZ = 0;

            float transitionValueX;
            if (distanceX != 0)
                transitionValueX = (8f * (distanceX * (UpdatedDistanceX) - Mathf.Pow(UpdatedDistanceX, 2)) / Mathf.Pow(distanceX, 2));
            else
                transitionValueX = 0;

            Debug.Log("TRansition Values -  X : " + transitionValueX + " :: Z : " + transitionValueZ);


            if (final.z > init.z) 
                playerAnimator.SetFloat("VelZ", transitionValueX);
            else
                playerAnimator.SetFloat("VelZ", -transitionValueX);

            if (final.x > init.x) 
                playerAnimator.SetFloat("VelX", transitionValueZ);
            else
                playerAnimator.SetFloat("VelX", -transitionValueZ);

            transform.position = Vector3.Lerp(init, final, t);
            timeElapsed += Time.deltaTime;
            yield return null;

            //Condition to Stop Continuous Movement
            if (stopMovement)
            {
                stopMovement = false;
                playerAnimator.SetFloat("VelZ", 0);
                StopCoroutine(Lerp(init, transform.position));
            }
        }
    }

    /// <summary>
    /// Triggering Singleton Animations 
    /// </summary>
    /// <param name="keyCodeHash"></param>
    /// <returns></returns>
    IEnumerator Trigger(string keyCodeHash)
    {
        playerAnimator.SetBool(keyCodeHash, true);
        yield return new WaitForSeconds(WaitTime());
        playerAnimator.SetBool(keyCodeHash, false);
        stopMovement = false;
    }

    #endregion

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

    // TODO: Generate test cases to test all the APIs
}
