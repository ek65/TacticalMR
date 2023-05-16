using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonAnimationTriggers : MonoBehaviour
{
    public static SingletonAnimationTriggers instance;

    Animator animator;

    public void Awake()
    {
        instance = this;
    }
    Animator animator;
    public AnimationClip[] clips;

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
            StartCoroutine(Trigger(keyCodeHash, key));            
        }
        if (Input.GetKeyUp(key))
        {
            animator.SetBool(keyCodeHash, false);
        }
    }

    IEnumerator Trigger(string keyCodeHash, KeyCode key)
    {
        gameObject.GetComponent<MovementAnimationController>().isTranslationAllowed = false;
        animator.SetBool(keyCodeHash, true);

        yield return new WaitForSeconds(WaitTime(keyCodeHash));

        gameObject.GetComponent<MovementAnimationController>().isTranslationAllowed = true;
    }

    float WaitTime(string keyCodeHash)
    {
        int i = 0;
        switch (keyCodeHash)
        {
            case "Pass": i = 0; break;
            case "Receive": i = 1; break;
        }
        return clips[i].length;
    }
}
