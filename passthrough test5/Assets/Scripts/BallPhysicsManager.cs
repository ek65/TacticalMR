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
    Rigidbody rigidbody;
    float dynamicFriction;
    public PhysicMaterial ballMaterial;

    public Vector3 force = new Vector3(0, 0, 0);
    public Vector3 targetPosition = new Vector3(0, 0, 0);

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        dynamicFriction = gameObject.GetComponent<SphereCollider>().material.dynamicFriction;

    }

    // TODO: Motion without angular drag

    // API for motion in xz plane
    // Pass - slow and fast
    // dribbling - kick with less force, happenning continuously
    // API for projectile motion

    // TODO: Motion with angular drag - implementing magnus effect

    // TODO: rotate ball upon motion

    public void ImpartGroundedMotion()
    {
        Vector3 direction = targetPosition - rigidbody.position;
        Vector3 normalizedDirection = direction.normalized;

        float distance = direction.magnitude;
        float gravity = 9.81f;
        float initialVelocity_sq = 2*dynamicFriction*gravity*distance;
        float initalVelocity = Mathf.Sqrt(initialVelocity_sq);

        float force_magnitude = (initalVelocity * rigidbody.mass) / Time.fixedDeltaTime;

        force = force_magnitude * normalizedDirection;

        rigidbody.AddForce(force);
    }
}