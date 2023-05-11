using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonAnimationTriggers : MonoBehaviour
{
    Animator animator; 
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        TriggerSingletonAnimation("Pass", KeyCode.P);
        TriggerSingletonAnimation("Receive", KeyCode.R);
    }

    void TriggerSingletonAnimation(string keyCodeHash, KeyCode key)
    {
        if (Input.GetKeyDown(key))
        {
            animator.SetBool(keyCodeHash, true);
        }
        if (Input.GetKeyUp(key))
        {
            animator.SetBool(keyCodeHash, false);
        }
    }
}
