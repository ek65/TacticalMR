using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Disable : MonoBehaviour
{
    Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
   
    }

    private void Update()
    {
        animator.SetBool("receive_from_left_and_pass_to_left", false);
    }

}
