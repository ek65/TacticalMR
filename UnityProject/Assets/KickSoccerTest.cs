using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MxM;
public class KickSoccerTest : MonoBehaviour
{
    [SerializeField] MxMEventDefinition kickEvent;
    MxMAnimator animator; 
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<MxMAnimator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            animator.BeginEvent(kickEvent);
        }
    }
}
