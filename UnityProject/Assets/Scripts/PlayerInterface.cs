using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;

// TODO: Rename script, this is the player logic script
public class PlayerInterface : MonoBehaviour
{
    public bool enemy;
    public bool ally;
    public Renderer shirt;
    public float speed;
    public Transform ball;
    public Vector3 ballOnTheGround;
    public Transform mainCamera;
    public Rig animationRigging;
    private Animator anim;
    //private Vector3 correctPosition;
    public Vector3 targetPosition;
    public float force;
    //public bool getBall;
    //public bool travelWithBall;
    public bool turn;
    //public Vector3 travelEndPos;
    public float distToBall;
    public float distToPos;
    public Transform goal;
    public Action action;
    public bool move;
    public Vector3 movePos;

    private float kickDebounce;
    
    private int localTick;  // NOTE: This is not the true tick and is what we will use to internally record a timestep.

    public ActionAPI actionAPI;

    public enum Action
    {
        NOT_TAKING_ACTION,
        TAKING_ACTION,
        TAKING_RECURRING_ACTION
    };

    private void Start()
    {
        if (enemy)
        {
            shirt.material.SetColor("_Color", Color.red);
        }
        if (ally)
        {
            shirt.material.SetColor("_Color", Color.blue);
        }
        // ballOnTheGround.x = ball.transform.position.x;
        // ballOnTheGround.y = 0;
        // ballOnTheGround.z = ball.transform.position.z;
        // transform.LookAt(ballOnTheGround);
        // correctPosition = calculateCorrectPosition(transform.position, ballOnTheGround);
        localTick = -1;
        anim = GetComponent<Animator>();
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("ball").transform;
        }
        if (goal == null)
        {
            goal = GameObject.FindGameObjectWithTag("goal").transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("ball").transform;
        }
        if (goal == null)
        {
            goal = GameObject.FindGameObjectWithTag("goal").transform;
        }
        ballOnTheGround.x = ball.transform.position.x;
        ballOnTheGround.y = 0;
        ballOnTheGround.z = ball.transform.position.z;
        distToBall = Vector3.Distance(transform.position, ballOnTheGround);
    }

    public void ApplyMovement(ScenicMovementData data)
    {
        localTick += 1;
        // Debug.Log(GetComponent<Rigidbody>().velocity);
        if (localTick < 4)
        {
            return;
        }
        if (data.actionFunc != null)
        {
            Type type = actionAPI.GetType();
            MethodInfo method = type.GetMethod(data.actionFunc);
            // Debug.Log("here12");
            // Debug.Log(data.actionFunc);
            // Debug.Log(data.actionArgs.ToArray().Length);
            // foreach (var v in data.actionArgs.ToArray())
            // {
            //     Debug.Log(v);
            // }
            method.Invoke(actionAPI, data.actionArgs.ToArray());
        }
        else //idle
        {
            actionAPI.stopMovement = true;
        }
    }
}
