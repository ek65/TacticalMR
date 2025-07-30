using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoccerBall : MonoBehaviour
{
    public Vector3 destination;
    private void Update()
    {
        // as ball reaches destination (ignoring the y axis), slow down the ball and completely stop it at the destination
        if (Mathf.Abs(transform.position.x - destination.x) < 0.1f && Mathf.Abs(transform.position.z - destination.z) < 0.1f)
        {
            // slow down the ball as it approaches the destination
            Vector3 direction = (destination - transform.position).normalized;
            GetComponent<Rigidbody>().velocity = direction * Mathf.Max(0, GetComponent<Rigidbody>().velocity.magnitude - Time.deltaTime);
            // stop moving the ball
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }
}