using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SoccerBall : MonoBehaviour
{
    public Vector3 destination;
    [Tooltip("How close (in meters) before we consider ourselves 'there'")]
    public float stopDistance = 0.1f;

    Rigidbody _rb;
    float _stopDistanceSqr;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _stopDistanceSqr = stopDistance * stopDistance;
    }

    void FixedUpdate()
    {
        // if no destination set, bail
        if (destination == Vector3.zero) return;

        // horizontal delta (ignore Y)
        Vector3 delta = destination - transform.position;
        delta.y = 0f;

        // if we're inside the stop radius...
        if (delta.sqrMagnitude <= _stopDistanceSqr)
        {
            // snap exactly
            Vector3 pos = transform.position;
            pos.x = destination.x;
            pos.z = destination.z;
            transform.position = pos;

            // kill all motion
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            // optional: disable further physics so it won't drift
            _rb.isKinematic = true;
            // or: _rb.Sleep();

            // and clear the dest so we don't re‐snap every frame
            destination = Vector3.zero;
        }
        else
        {
            // OPTIONAL: gently brake as you approach
            // float speed = _rb.velocity.magnitude;
            // Vector3 dir = delta.normalized;
            // _rb.velocity = dir * Mathf.Max(0f, speed - 0.1f);
        }
    }
}
