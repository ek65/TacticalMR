using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionAPI : MonoBehaviour
{
    [SerializeField] Animator playerAnimator;
    [SerializeField] float playerRunningSpeed = 2f;
    [SerializeField] float timeDuration = 5f;

    private void Start()
    {
        playerAnimator = this.GetComponent<Animator>();
        
        //Vector3 currPos = this.transform.position;
        //Vector3 finalPos = new Vector3(currPos.x + 10f, currPos.y, currPos.z + 10f);
        //DribbleFromOnePositionToAnother(currPos, finalPos);
    }

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
        }
    }

    // TODO: movement in left only
    // TODO: movement in right only
    // TODO: movement in back direction only
    // TODO: movement with any value of velx and velz
    // (there can be a single function doing all this, or may not be)

    void ReceiveBall()
    {
        StartCoroutine(Trigger("Receive"));
    }

    void TackleBall()
    {
        StartCoroutine(Trigger("StrongTackle"));
    }

    void GroundPassSlow()
    {
        StartCoroutine(Trigger("GroundPassSlow"));
    }

    void GroundPassFast()
    {
        StartCoroutine(Trigger("GroundPassFast"));
    }

    void AirPass()
    {
        StartCoroutine(Trigger("AirPass"));
    }

    void ChipSideways()
    {
        StartCoroutine(Trigger("ChipSideways"));
    }
    void ChipFront()
    {
        StartCoroutine(Trigger("ChipFront"));
    }

    void Kick()
    {
        StartCoroutine(Trigger("Kick"));
    }

    IEnumerator Trigger(string keyCodeHash)
    {
        // gameObject.GetComponent<MovementAnimationController>().isTranslationAllowed = false;
        playerAnimator.SetBool(keyCodeHash, true);
        yield return new WaitForSeconds(WaitTime());
        playerAnimator.SetBool(keyCodeHash, false);
        //gameObject.GetComponent<MovementAnimationController>().isTranslationAllowed = true;
    }

    float WaitTime()
    {
        float animationLength = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        float animationSpeed = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).speed;
        float delay = animationLength * animationSpeed;
        return delay;
    }

    // TODO: stop or pause translation while movement and dribbling if any of the singleton animation is trigerred
    // TODO: Generate test cases to test all the APIs
}
