using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationProcess : MonoBehaviour
{
    public string stateName;
    private Animator anim;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void PlayAnimation(string state)
    {
        anim.Play(state);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayAnimation(stateName);
        }
    }
}
