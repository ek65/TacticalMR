using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;

public class MoveToSoccerBallAndTurn : MonoBehaviour
{
    public float speed;
    public Transform ball;
    private Vector3 ballOnTheGround;
    public Transform mainCamera;
    public Rig animationRigging;
    private Animator anim;
    private Collider cl;
    private Vector3 correctPosition;
    public Vector3 targetPosition;
    public float force;
    public bool getBall;
    public bool travelWithBall;
    public bool turn;
    public Vector3 travelEndPos;
    public bool waitBall;
    public Vector3 runEndPos;
    public bool runToPos;


    private void Start()
    {
        anim = GetComponent<Animator>();
        cl = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        anim.ResetTrigger("reachedBall");
        cl.enabled = false;
        if (getBall)
        {
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, correctPosition, step);
        }

        if (transform.position == correctPosition && getBall && travelWithBall)
        {
            TravelWithBall();
        }

        if (transform.position == travelEndPos)
        {
            travelWithBall = false;
        }
        if (transform.position == correctPosition && getBall && !travelWithBall)
        {
            cl.enabled = true;
            TurnAround();
            anim.SetTrigger("reachedBall");
        }
        
        
    }

    public void RunToBall()
    {
        ballOnTheGround.x = ball.transform.position.x;
        ballOnTheGround.y = 0;
        ballOnTheGround.z = ball.transform.position.z;
        transform.LookAt(ballOnTheGround);
        correctPosition = calculateCorrectPosition(transform.position, ballOnTheGround);
    }

    Vector3 calculateCorrectPosition(Vector3 pos1, Vector3 pos2)
    {
        float xPosDiff = pos2.x - pos1.x;
        float zPosDiff = pos2.z - pos1.z;
        float distance = Mathf.Pow(Mathf.Pow(xPosDiff, 2) + Mathf.Pow(zPosDiff, 2), (float)0.5) + (float)0.2;
        Vector3 normalizedDirection = Vector3.Normalize(Vector3.MoveTowards(transform.position, ballOnTheGround, 1));
        return normalizedDirection * (distance);
    }
  
    // Turn towards main player's camera.
    public void TurnAround()
    {
        animationRigging.weight = 1;
    }

    public void TravelWithBall()
    {
        cl.enabled = true;
        correctPosition = new Vector3(ball.transform.position.x, 0, ball.transform.position.z);
        if (turn)
        {
            transform.LookAt(mainCamera);
            //TurnAround();
            turn = false;
        }
        force = 3;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "soccer_ball")
        { 
            //targetPosition = Vector3.forward;
            targetPosition = (mainCamera.transform.position - transform.position).normalized;
            ball.GetComponent<Rigidbody>().AddForce(targetPosition * force, ForceMode.Impulse);
        }
    }
}
