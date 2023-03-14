using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;

public class MoveToSoccerBallAndTurn : MonoBehaviour
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
    public Collider cl;
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
        anim = GetComponent<Animator>();
        cl = GetComponent<Collider>();
        cl.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        ballOnTheGround.x = ball.transform.position.x;
        ballOnTheGround.y = 0;
        ballOnTheGround.z = ball.transform.position.z;
        distToBall = Vector3.Distance(transform.position, ballOnTheGround);
        // anim.ResetTrigger("reachedBall");
        // cl.enabled = false;
        // float step = speed * Time.deltaTime;
        // transform.position = Vector3.MoveTowards(transform.position, correctPosition, step);
        // if (transform.position == correctPosition && getBall && travelWithBall)
        // {
        //     TravelWithBall();
        // }
        //
        // if (transform.position == travelEndPos)
        // {
        //     travelWithBall = false;
        // }
        // if (transform.position == correctPosition && getBall && !travelWithBall)
        // {
        //     cl.enabled = true;
        //     if (turn)
        //     {
        //         TurnAround();
        //         turn = false;
        //     }
        //     anim.SetTrigger("reachedBall");
        // }
    }

    public IEnumerator IdleForSec(int sec, System.Action<bool, bool> callback)
    {
        //Debug.Log("in here");
        yield return new WaitForSeconds(sec);
        callback(false, true);
    }
    
    public IEnumerator IdleForSec(float sec)
    {
        action = Action.TAKING_ACTION;
        //Debug.Log("in here");
        yield return new WaitForSeconds(sec);
        action = Action.NOT_TAKING_ACTION;
    }

    Vector3 calculateCorrectPosition(Vector3 pos1, Vector3 pos2)
    {
        float xPosDiff = pos2.x - pos1.x;
        float zPosDiff = pos2.z - pos1.z;
        distToBall = Mathf.Pow(Mathf.Pow(xPosDiff, 2) + Mathf.Pow(zPosDiff, 2), (float)0.5) + (float)0.2;
        Vector3 normalizedDirection = Vector3.Normalize(Vector3.MoveTowards(transform.position, ballOnTheGround, 1));
        return normalizedDirection * (distToBall);
    }

    public void MoveToBallThenLook(Vector3 lookAtPos)
    {
        action = Action.TAKING_RECURRING_ACTION;
        anim.Play("Running");
        transform.LookAt(ballOnTheGround);
        //correctPosition = calculateCorrectPosition(transform.position, ballOnTheGround);

        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, ballOnTheGround, step);
        if (distToBall == 0)
        {
            anim.Play("Idle");
            transform.LookAt(lookAtPos);
            action = Action.NOT_TAKING_ACTION;
        }
    }
    
    public void MoveToPosThenLook(Vector3 moveToPos, Vector3 lookAtPos)
    {
        action = Action.TAKING_RECURRING_ACTION;
        anim.Play("Running");
        transform.LookAt(moveToPos);
        distToPos = Vector3.Distance(transform.position, moveToPos);
        //correctPosition = calculateCorrectPosition(transform.position, ballOnTheGround);
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, moveToPos, step);
        if (distToPos == 0)
        {
            anim.Play("Idle");
            transform.LookAt(lookAtPos);
            action = Action.NOT_TAKING_ACTION;
        }
    }

    public void KickBall(Vector3 pos)
    {
        action = Action.TAKING_ACTION;
        targetPosition = pos;
        anim.Play("Kick Soccerball");
    }

    // called by animation event in "Kick Soccerball"
    void Kick()
    {

        ball.GetComponent<Rigidbody>().AddForce((targetPosition - transform.position).normalized * force, ForceMode.Impulse);
    }

    // called by animation event in "Kick Soccerball"
    private void AfterKick()
    {
        //Debug.Log("test");
        anim.Play("Idle");
        action = Action.NOT_TAKING_ACTION;
    }

    public void WaitForBall()
    {
        action = Action.TAKING_RECURRING_ACTION;
        if (distToBall <= 0.1f)
        {
            StopBall();
            action = Action.NOT_TAKING_ACTION;
        }
    }

    public void StopBall()
    {
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        Vector3 temp = transform.position;
        temp.y = transform.position.y + 0.1f;
        temp.x = transform.position.x;
        temp.z = transform.position.z;
        ball.transform.position = temp;
        
    }
  
    // Turn towards main player's camera.
    void TurnAround()
    {
        animationRigging.weight = 1;
    }

    public void TravelWithBall(Vector3 moveToPos, Vector3 lookAtPos)
    {
        //cl.enabled = true;
        //correctPosition = new Vector3(ball.transform.position.x, 0, ball.transform.position.z);
        // if (turn)
        // {
        //     transform.LookAt(mainCamera);
        //     //TurnAround();
        //     turn = false;
        // }
        action = Action.TAKING_RECURRING_ACTION;
        anim.Play("Running");
        transform.LookAt(moveToPos);
        distToPos = Vector3.Distance(transform.position, moveToPos);
        
        targetPosition = moveToPos;

        if (distToBall <= 0.1f)
        {
            float tempForce = 1f;
            ball.GetComponent<Rigidbody>().AddForce((targetPosition - transform.position).normalized * tempForce, ForceMode.Impulse);
        }

        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, moveToPos, step);
        if (distToPos == 0)
        {
            anim.Play("Idle");
            StopBall();
            transform.LookAt(lookAtPos);
            action = Action.NOT_TAKING_ACTION;
        }
    }

    // private void OnCollisionEnter(Collision collision)
    // {
    //     if (collision.gameObject.name == "soccer_ball")
    //     { 
    //         //targetPosition = Vector3.forward;
    //         //targetPosition = (mainCamera.transform.position - transform.position).normalized;
    //         ball.GetComponent<Rigidbody>().AddForce((targetPosition - transform.position).normalized * force, ForceMode.Impulse);
    //     }
    // }
}
