using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTriggers : MonoBehaviour
{
    Animator characterAnimator;
    float forward;
    // Start is called before the first frame update
    void Start()
    {
        characterAnimator = gameObject.GetComponent<Animator>();
        if (characterAnimator != null)
            Debug.Log(characterAnimator);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetAxis("Vertical") != 0)
        {
            forward = Input.GetAxis("Vertical");
            Debug.Log("Pressed UpArrow : " + forward);

            characterAnimator.SetFloat("ForwardRunning", forward);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Pressed Q");
            characterAnimator.SetTrigger("Kick");
        }
        if (Input.GetKeyUp(KeyCode.Q))
        {
            Debug.Log("Released Q");
            characterAnimator.ResetTrigger("Kick");
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            characterAnimator.SetTrigger("LeftTurn");
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            characterAnimator.ResetTrigger("LeftTurn");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            characterAnimator.SetTrigger("LightKick");
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            characterAnimator.ResetTrigger("LightKick");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            characterAnimator.SetTrigger("RightTurn");
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            characterAnimator.ResetTrigger("RightTurn");
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            characterAnimator.SetTrigger("LightPass");
        }
        if (Input.GetKeyUp(KeyCode.T))
        {
            characterAnimator.ResetTrigger("LightPass");
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            characterAnimator.SetTrigger("RunningTurn180");
        }
        if (Input.GetKeyUp(KeyCode.Y))
        {
            characterAnimator.ResetTrigger("RunningTurn180");
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            characterAnimator.SetTrigger("RecieveBall");
        }
        if (Input.GetKeyUp(KeyCode.U))
        {
            characterAnimator.ResetTrigger("RecieveBall");
        }

    }
}
