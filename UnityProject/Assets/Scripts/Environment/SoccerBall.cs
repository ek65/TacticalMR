using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoccerBall : MonoBehaviour
{
    public Vector3 destination;
    private void Update()
    {
        GameObject human = GameObject.FindGameObjectWithTag("human");
        bool isHumanShooting = human.GetComponent<HumanInterface>().actionAPI.isHumanShootGoal;

        if (isHumanShooting)
        {
            return;
        }
        if (destination == Vector3.zero)
        {
            return;
        }
        // as ball reaches destination (ignoring the y axis), slow down the ball and completely stop it at the destination
        if (Mathf.Abs(transform.position.x - destination.x) < 0.1f && Mathf.Abs(transform.position.z - destination.z) < 0.1f)
        {
            // slow down the ball very slightly as it approaches the destination
            // this is to prevent the ball from overshooting the destination
            Vector3 direction = (destination - transform.position).normalized;
            GetComponent<Rigidbody>().velocity = direction * Mathf.Max(0, GetComponent<Rigidbody>().velocity.magnitude - 0.1f);
            // stop moving the ball
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }
}