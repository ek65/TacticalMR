using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using Pathfinding;

// TODO: Rename script, this is the player logic script
public class PlayerInterface : MonoBehaviour
{
    public bool enemy;
    public bool ally;
    public bool self;
    public Renderer shirt;
    public float speed;
    public GameObject ball;
    public Vector3 ballOnTheGround;
    public Vector3 targetPosition;
    public float force;
    public float distToBall;
    public GameObject goal;
    public bool ballPossession;
    public Transform ballPosition;

    public Vector3 currVelocity => this.GetComponent<RichAI>().velocity;

    private bool canPossessBall = true;
    public bool canKickBall = true;
    
    private int localTick;  // NOTE: This is not the true tick and is what we will use to internally record a timestep.

    public ActionAPI actionAPI;
    public FloatingText floatingText;
    public string behavior = "Idle";
    public string currAction = "No Action"; // just for debugging to see what actions function is being called
    
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
        if (self)
        {
            shirt.material.SetColor("_Color", Color.yellow);
        }
        localTick = -1;
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("ball");
        }
        if (goal == null)
        {
            goal = GameObject.FindGameObjectWithTag("goal");
        }

        ballPossession = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("ball");
        }
        if (goal == null)
        {
            goal = GameObject.FindGameObjectWithTag("goal");
        }

        ballOnTheGround.x = ball.transform.position.x;
        ballOnTheGround.y = ball.transform.position.y;
        ballOnTheGround.z = ball.transform.position.z;
        distToBall = Vector3.Distance(transform.position, ballOnTheGround);
        
        if (actionAPI.stopMovement == true)
        {
            actionAPI.Idle();
        }
    }
    
    // For ball possession
    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("ball") && canPossessBall)
        {
            GainPossession(other);
        }
    }
    
    private void GainPossession(Collision other)
    {
        int layerIgnoreBallCollision = LayerMask.NameToLayer("PlayerBall");
        other.gameObject.layer = layerIgnoreBallCollision;
        
        ball.transform.position = ballPosition.position;
        ball.transform.SetParent(ballPosition);
        ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        ballPossession = true;
        this.GetComponentInParent<ActionAPI>().ReceiveBall(other.transform.position);
    }
    
    public void LosePossession()
    {
        StartCoroutine(PossessionDebounce());
        ball.transform.SetParent(null);
        ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        ballPossession = false;
    }
    
    private IEnumerator PossessionDebounce()
    {
        canPossessBall = false;
        yield return new WaitForSeconds(1f);
        canPossessBall = true;
        ball.gameObject.layer = LayerMask.NameToLayer("Default");
    }
    
    public IEnumerator KickDebounce()
    {
        canKickBall = false;
        yield return new WaitForSeconds(2.5f);
        canKickBall = true;
    }

    public void ApplyMovement(ScenicMovementData data)
    {
        localTick += 1;
        // Debug.Log(GetComponent<Rigidbody>().velocity);
        if (localTick < 4)
        {
            return;
        }
        
        Debug.LogError(data.behavior);

        if (data.behavior == " " || data.behavior == "" || data.behavior == "Idle")
        {
            behavior = "Idle";
            floatingText.SetText("Idle");
        }
        else if (data.behavior != "" || data.behavior != null)
        {
            behavior = data.behavior;
            floatingText.SetText(data.behavior);
        }
        else
        {
            behavior = "Idle";
            floatingText.SetText("Idle");
        }

        if (behavior == "Idle")
        {
            currAction = "No Action";
        }
        
        if (data.actionFunc != null)
        {
            currAction = data.actionFunc;
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
