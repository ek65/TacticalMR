using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    back,
    left,
    right
}

public class ActionAPI : MonoBehaviour
{
    [SerializeField] Animator playerAnimator;
    [SerializeField] float playerRunningSpeed = 2f;
    [SerializeField] float timeDuration = 5f;
    
    bool stopMovement = false;

    private void Start()
    {
        playerAnimator = this.GetComponent<Animator>();
        
        //Vector3 currPos = this.transform.position;
        //Vector3 finalPos = new Vector3(currPos.x + 10f, currPos.y, currPos.z + 10f);
        //DribbleFromOnePositionToAnother(currPos, finalPos);
    }


    // Done - TODO: movement in left only
    // Done - TODO: movement in right only
    // Done - TODO: movement in back direction only
    // TODO: movement with any value of velx and velz
    // (there can be a single function doing all this, or may not be)

    #region Methods 

    /// <summary>
    /// Method/API to Move player from One Position to another
    /// </summary>
    /// <param name="init">Initial Position</param>
    /// <param name="final">Final Position</param>
    public void MoveFromOnePositionToAnother(Vector3 init, Vector3 final)
    {
        string currAnimationController = playerAnimator.runtimeAnimatorController.name;
        if (currAnimationController != "Movement")
        {
            RuntimeAnimatorController newController = Resources.Load("Animation/Movement", typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            playerAnimator.runtimeAnimatorController = newController;
        }
        StartCoroutine(Lerp(init, final));
    }

    /// <summary>
    /// Dribbling between two points 
    /// </summary>
    /// <param name="init">Initial(Starting) position</param>
    /// <param name="final">Final(End) position</param>
    public void DribbleFromOnePositionToAnother(Vector3 init, Vector3 final)
    {
        string currAnimationController = playerAnimator.runtimeAnimatorController.name;
        if (currAnimationController != "Dribbling")
        {
            RuntimeAnimatorController newController = Resources.Load("Animation/Dribbling", typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            playerAnimator.runtimeAnimatorController = newController;
        }
        StartCoroutine(Lerp(init, final));
    }

    /// <summary>
    /// Straf Jogging Method along Either Direction left or Right
    /// </summary>
    /// <param name="init">Initial Position</param>
    /// <param name="final">Final Position</param>
    /// <param name="dir">Direction Left or Right</param>
    void StrafJog(Vector3 init,Vector3 final,Direction dir)
    {
        string currAnimationController = playerAnimator.runtimeAnimatorController.name;
        if (currAnimationController != "Movement")
        {
            RuntimeAnimatorController newController = Resources.Load("Animation/Movement", typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            playerAnimator.runtimeAnimatorController = newController;
        }
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
        string currAnimationController = playerAnimator.runtimeAnimatorController.name;
        if (currAnimationController != "Movement")
        {
            RuntimeAnimatorController newController = Resources.Load("Animation/Movement", typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            playerAnimator.runtimeAnimatorController = newController;
        }
        StartCoroutine(Lerp(init, final, dir));
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

    float WaitTime()
    {
        float animationLength = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        float animationSpeed = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).speed;
        float delay = animationLength * animationSpeed;
        return delay;
    }

    // Done - TODO: stop or pause translation while movement and dribbling if any of the singleton animation is trigerred
    // TODO: Generate test cases to test all the APIs

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
                stopMovement = false;
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
    /// Coroutine that is responsible for Triggering actions 
    /// </summary>
    /// <param name="keyCodeHash"></param>
    /// <returns></returns>
    IEnumerator Trigger(string keyCodeHash)
    {
        // gameObject.GetComponent<MovementAnimationController>().isTranslationAllowed = false;
        playerAnimator.SetBool(keyCodeHash, true);
        yield return new WaitForSeconds(WaitTime());
        playerAnimator.SetBool(keyCodeHash, false);
        //gameObject.GetComponent<MovementAnimationController>().isTranslationAllowed = true;
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

            float transitionValueZ = (8f * (distance * (UpdatedDistance) - Mathf.Pow(UpdatedDistance, 2)) / Mathf.Pow(distance, 2));
            float transitionValueX = (8f * (distance * (UpdatedDistance) - Mathf.Pow(UpdatedDistance, 2)) / Mathf.Pow(distance, 2));

            playerAnimator.SetFloat("VelZ", transitionValueZ);
            playerAnimator.SetFloat("VelX", transitionValueX);

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

    #endregion

}
