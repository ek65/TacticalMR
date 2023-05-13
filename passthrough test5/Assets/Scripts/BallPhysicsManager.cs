using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

/*
 * https://docs.unity3d.com/ScriptReference/Rigidbody.html
*/

public class BallPhysicsManager : MonoBehaviour
{
    Rigidbody rigidbody;
    const float accelerationGravity = 9.8f;

    Vector3 initialVelocity;
    Vector3 currentVelocity;
    Vector3 acceleration;
    Vector3 initialForce;

    float mass;

    Vector3 initialPosition;
    Vector3 currentPosition;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        mass = rigidbody.mass;

        acceleration = new Vector3(0, -accelerationGravity, 0);
        initialVelocity = Vector3.zero;
        currentVelocity = Vector3.zero;

        initialForce = mass * acceleration;

        initialPosition = gameObject.transform.position;
    }
    float totalTime = 0;
    void FixedUpdate()
    {
        // gravity
        rigidbody.AddForce(initialForce, ForceMode.Force);

        // keeping track of ball velocity at all times
        currentVelocity = rigidbody.velocity;
    }

    // TODO: Motion without angular drag
    
    // API for motion in xz plane
        // Pass - slow and fast
        // dribbling - kick with less force, happenning continuously
    // API for projectile motion

    // TODO: Motion with angular drag - implementing magnus effect

    // TODO: rotate ball upon motion
}
