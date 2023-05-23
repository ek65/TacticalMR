using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonAnimationTriggers : MonoBehaviour
{
    public static SingletonAnimationTriggers instance;
    public GameObject soccerBall;

    bool hasTriggered;
    public void Awake()
    {
        instance = this;
    }

    Animator animator;
    public AnimationClip[] clips;

    void Start()
    {
        animator = GetComponent<Animator>();
        hasTriggered = false;
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
            StartCoroutine(Trigger(keyCodeHash));            
        }
        //if (Input.GetKeyUp(key))
        //{
        //    animator.SetBool(keyCodeHash, false);
        //}
    }

    IEnumerator Trigger(string keyCodeHash)
    {
        Debug.Log("triggerring" + keyCodeHash);
        gameObject.GetComponent<MovementAnimationController>().isTranslationAllowed = false;
        animator.SetBool(keyCodeHash, true);

        yield return new WaitForSeconds(WaitTime(keyCodeHash));

        animator.SetBool(keyCodeHash, false);
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SoccerBall") && !hasTriggered)
        {
            StartCoroutine(Trigger("Receive"));
            soccerBall.GetComponent<Rigidbody>().isKinematic = true;
            soccerBall.GetComponent<Rigidbody>().isKinematic = false;
            hasTriggered = true;
        }
    }
}
