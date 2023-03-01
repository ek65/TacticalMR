using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;
using UnityEngine;

public class MoveToSoccerBallAndTurn : MonoBehaviour
{
    public float speed;
    public Transform ball;
    public Vector3 ballOnTheGround;
    public Transform mainCamera;
    public Rig animationRigging;
    public Animator anim;
    public Collider cl;
    private Vector3 correctPosition;
    public float Force;

    private void Start()
    {
        ballOnTheGround.x = ball.transform.position.x;
        ballOnTheGround.y = 0;
        ballOnTheGround.z = ball.transform.position.z;
        transform.LookAt(ballOnTheGround);
        correctPosition = calculateCorrectPosition(transform.position, ballOnTheGround);
        anim = GetComponent<Animator>();
        cl = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, correctPosition, step);
        if (transform.position == correctPosition)
        {
            cl.enabled = true;
            TurnAround();
            anim.SetTrigger("reachedBall");
        }
    }

    Vector3 calculateCorrectPosition(Vector3 pos1, Vector3 pos2)
    {
        float xPosDiff = pos2.x - pos1.x;
        float zPosDiff = pos2.z - pos1.z;
        float distance = Mathf.Pow(Mathf.Pow(xPosDiff, 2) + Mathf.Pow(zPosDiff, 2), (float)0.5) + (float)0.2;
        Vector3 normalizedDirection = Vector3.Normalize(Vector3.MoveTowards(transform.position, ballOnTheGround, 1));
        return normalizedDirection * (distance);
    }
  

    void TurnAround()
    {
        animationRigging.weight = 1;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "soccer_ball")
        {
            ball.GetComponent<Rigidbody>().AddForce(Vector3.forward * Force, ForceMode.Impulse);
        }
    }
}
