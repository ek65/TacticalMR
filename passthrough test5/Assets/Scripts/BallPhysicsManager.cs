using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;

/*
 * https://docs.unity3d.com/ScriptReference/Rigidbody.html
*/

public class BallPhysicsManager : MonoBehaviour
{
    //Rigidbody rigidbody;
    //float dynamicFriction;
    //public PhysicMaterial ballMaterial;

    //public Vector3 force = new Vector3(0, 0, 0);
    //public Vector3 targetPosition = new Vector3(0, 0.22f, 0);

    //public float singletonForce = 50f;

    void Start()
    {
        //rigidbody = GetComponent<Rigidbody>();
        //dynamicFriction = gameObject.GetComponent<SphereCollider>().material.dynamicFriction;
    }

    void FixedUpdate()
    {
        //if (Input.GetKeyDown(KeyCode.UpArrow))
        //{
        //    rigidbody.AddForce(new Vector3(0, 0, singletonForce));
        //}
        //if (Input.GetKeyDown(KeyCode.DownArrow))
        //{
        //    rigidbody.AddForce(new Vector3(0, 0, -singletonForce));
        //}
        //if (Input.GetKeyDown(KeyCode.RightArrow))
        //{
        //    rigidbody.AddForce(new Vector3(singletonForce, 0, 0));
        //}
        //if (Input.GetKeyDown(KeyCode.LeftArrow))
        //{
        //    rigidbody.AddForce(new Vector3(-singletonForce, 0, 0));
        //}
        //if (Input.GetKeyDown(KeyCode.RightShift))
        //{
        //    rigidbody.AddForce(new Vector3(0, 100, 0));
        //    // TODO: How to decide the magnitude of vertical force
        //}
    }

    //public void ImpartMotionBasedOnTarget(bool isProjectile = false)
    //{
    //    Vector3 direction = targetPosition - rigidbody.position;
    //    Vector3 normalizedDirection = direction.normalized;

    //    float distance = direction.magnitude;
    //    float gravity = 9.81f;
    //    float initialVelocity_sq = 2*dynamicFriction*gravity*distance;
    //    float initalVelocity = Mathf.Sqrt(initialVelocity_sq);

    //    float force_magnitude = (initalVelocity * rigidbody.mass) / Time.fixedDeltaTime;

    //    force = force_magnitude * normalizedDirection;

    //    if (isProjectile)
    //    {
    //        force = new Vector3(force.x, 100, force.y);
    //    }

    //    // TODO: How to decide the magnitude of vertical force

    //    rigidbody.AddForce(force);
    //}

    // TODO: Motion with angular drag - implementing magnus effect
}