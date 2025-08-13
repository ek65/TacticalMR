using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class SoccerBall : MonoBehaviour
{
    public Vector3 destination;
    [Tooltip("How close (in meters) before we consider ourselves 'there'")]
    public float stopDistance = 0.3f;
    [Tooltip("Minimum velocity threshold - below this, the ball stops")]
    public float minVelocityThreshold = 0.1f;
    [Tooltip("Force applied to guide ball toward destination")]
    public float guidanceForce = 3f;
    [Tooltip("How often to apply guidance correction (in seconds)")]
    public float guidanceInterval = 0.1f;
    [Tooltip("Maximum distance before applying strong correction")]
    public float maxDeviationDistance = 2f;
    [Tooltip("Ground layer mask for ground detection")]
    public LayerMask groundLayerMask = 1; // Default layer
    [Tooltip("Height above ground to maintain")]
    public float groundOffset = 0.25f;
    [Tooltip("Maximum upward velocity allowed")]
    public float maxUpwardVelocity = 2f;

    Rigidbody _rb;
    float _stopDistanceSqr;
    Vector3 _lastDestination;
    bool _hasDestination;
    float _lastGuidanceTime;
    Vector3 _initialDirection;
    Vector3 _lastValidPosition;
    float _groundHeight;
    bool _isGrounded;
    BallOwnership _ballOwnership;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _ballOwnership = GetComponent<BallOwnership>();
        _stopDistanceSqr = stopDistance * stopDistance;
        FindGroundHeight();
    }

    void FixedUpdate()
    {
        // Always manage ground position and bouncing
        ManageGroundPosition();

        // Check if someone has ball possession - if so, stop destination seeking
        if (_ballOwnership != null && _ballOwnership.ballOwner != null)
        {
            // Someone has possession - clear destination and stop guidance
            if (_hasDestination)
            {
                destination = Vector3.zero;
                _hasDestination = false;
                Debug.Log($"Ball possession gained by {_ballOwnership.ballOwner.name} - destination cleared");
            }
            return; // Don't process destination logic
        }

        // Check if destination has changed or been set for the first time
        if (destination != _lastDestination)
        {
            if (destination != Vector3.zero)
            {
                _hasDestination = true;
                _lastDestination = destination;
                _initialDirection = (destination - transform.position).normalized;
                _lastValidPosition = transform.position;
            }
            else
            {
                _hasDestination = false;
            }
        }

        // if no destination set, bail
        if (!_hasDestination) return;

        // horizontal delta (ignore Y for stopping calculation)
        Vector3 delta = destination - transform.position;
        Vector3 horizontalDelta = new Vector3(delta.x, 0f, delta.z);
        float distanceToDestination = horizontalDelta.magnitude;

        // Get current horizontal velocity
        Vector3 horizontalVelocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Check if we should stop
        bool shouldStop = CheckIfShouldStop(horizontalDelta, currentSpeed, distanceToDestination);
        
        // Also check if we've overshot the destination
        bool hasOvershot = CheckIfOvershot(horizontalDelta, currentSpeed, distanceToDestination);
        
        if (shouldStop || hasOvershot)
        {
            ForceStopAtDestination();
            return;
        }

        // Apply gentle guidance to keep ball on track (less aggressive)
        ApplyGentleGuidance(horizontalDelta, distanceToDestination);
    }

    void ManageGroundPosition()
    {
        FindGroundHeight();
        
        // Only intervene if ball is bouncing upward significantly
        if (_rb.velocity.y > maxUpwardVelocity)
        {
            Vector3 vel = _rb.velocity;
            vel.y = Mathf.Min(vel.y, maxUpwardVelocity);
            _rb.velocity = vel;
        }

        // Check if we're significantly above ground
        float currentHeight = transform.position.y;
        float targetHeight = _groundHeight + groundOffset;
        float heightDifference = currentHeight - targetHeight;
        
        // Only snap to ground if we're way too high or if we're falling and close to ground
        if (heightDifference > groundOffset * 2f && _rb.velocity.y <= 0)
        {
            // Ball is falling from high - let physics handle it but prepare to stop bounce
            _isGrounded = false;
        }
        else if (heightDifference < -0.05f || (heightDifference > 0.05f && heightDifference < 0.15f && _rb.velocity.y <= 0))
        {
            // Gently adjust position only if needed - don't constantly override
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, targetHeight, Time.fixedDeltaTime * 5f); // Gentle lerp instead of snap
            transform.position = pos;
            
            // Only stop upward velocity, preserve horizontal motion for rolling
            if (_rb.velocity.y > 0.1f)
            {
                Vector3 vel = _rb.velocity;
                vel.y = 0f;
                _rb.velocity = vel;
            }
            
            _isGrounded = true;
        }
        else if (Mathf.Abs(heightDifference) <= 0.05f)
        {
            _isGrounded = true;
        }
    }

    void FindGroundHeight()
    {
        // Raycast down to find ground height
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, 10f, groundLayerMask))
        {
            _groundHeight = hit.point.y;
        }
        else
        {
            // Fallback to current Y position if no ground found
            _groundHeight = transform.position.y - groundOffset;
        }
    }

    bool CheckIfShouldStop(Vector3 horizontalDelta, float currentSpeed, float distanceToDestination)
    {
        // Stop if very close to destination
        if (distanceToDestination <= stopDistance)
        {
            return true;
        }

        // Stop if moving very slowly and reasonably close
        if (currentSpeed < minVelocityThreshold && distanceToDestination <= stopDistance * 2f)
        {
            return true;
        }

        // Predict overshoot - if we'll be closer to destination after moving backwards
        if (currentSpeed > 0.1f)
        {
            Vector3 nextFramePosition = transform.position + new Vector3(_rb.velocity.x, 0f, _rb.velocity.z) * Time.fixedDeltaTime;
            Vector3 nextFrameDelta = destination - nextFramePosition;
            nextFrameDelta.y = 0f;
            
            // If next frame we'll be farther from destination, stop now
            if (nextFrameDelta.magnitude > distanceToDestination && distanceToDestination <= stopDistance * 1.5f)
            {
                return true;
            }
        }

        return false;
    }

    bool CheckIfOvershot(Vector3 horizontalDelta, float currentSpeed, float distanceToDestination)
    {
        // Only check for overshoot if we're moving and reasonably close to destination
        if (currentSpeed < 0.1f || distanceToDestination > stopDistance * 3f)
        {
            return false;
        }

        // Get horizontal velocity
        Vector3 horizontalVelocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        
        // Check if we're moving away from the destination
        // Dot product will be negative if velocity is pointing away from destination
        float dotProduct = Vector3.Dot(horizontalVelocity.normalized, horizontalDelta.normalized);
        
        // If we're moving away from destination and we're close, we've overshot
        if (dotProduct < -0.3f && distanceToDestination <= stopDistance * 2f)
        {
            Debug.Log($"Ball overshot destination! Distance: {distanceToDestination:F3}, Dot: {dotProduct:F3}");
            return true;
        }

        // Additional check: if we're very close and moving fast in any direction, stop
        if (distanceToDestination <= stopDistance * 0.5f && currentSpeed > minVelocityThreshold * 2f)
        {
            Debug.Log($"Ball very close to destination with high speed - stopping to prevent overshoot");
            return true;
        }

        return false;
    }

    void ApplyGentleGuidance(Vector3 horizontalDelta, float distanceToDestination)
    {
        // Only apply guidance at intervals and when grounded to avoid over-correction
        if (Time.time - _lastGuidanceTime < guidanceInterval || !_isGrounded)
            return;

        _lastGuidanceTime = Time.time;

        // Calculate desired direction
        Vector3 desiredDirection = horizontalDelta.normalized;
        
        // Current horizontal velocity direction
        Vector3 currentHorizontalVelocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        Vector3 currentDirection = currentHorizontalVelocity.normalized;

        // Only apply gentle guidance - much less aggressive than before
        float gentleForce = guidanceForce * 0.3f; // Reduced force
        
        // If we're far from destination, apply slightly more force
        if (distanceToDestination > maxDeviationDistance)
        {
            gentleForce = guidanceForce * 0.6f;
        }

        // Apply gentle guidance force toward destination (horizontal only)
        Vector3 guidanceForceVector = desiredDirection * gentleForce;
        
        // If we're moving in wrong direction, apply gentle correction
        if (Vector3.Dot(currentDirection, desiredDirection) < 0f && currentHorizontalVelocity.magnitude > 1f)
        {
            // Apply gentle braking force
            Vector3 brakingForce = -currentHorizontalVelocity.normalized * gentleForce;
            _rb.AddForce(brakingForce, ForceMode.Force);
        }
        
        // Apply guidance toward destination - use ForceMode.Force to preserve natural physics
        _rb.AddForce(guidanceForceVector, ForceMode.Force);
        
        // Don't interfere with angular velocity - let natural rolling physics work
    }

    void ForceStopAtDestination()
    {
        // Snap to destination (keep ground height)
        Vector3 pos = transform.position;
        pos.x = destination.x;
        pos.z = destination.z;
        pos.y = _groundHeight + groundOffset;
        transform.position = pos;

        // Kill all motion completely - ensure no residual velocity
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        
        // Briefly make kinematic to prevent any physics interference, then restore
        StartCoroutine(TemporaryKinematic());

        // Clear destination
        destination = Vector3.zero;
        _hasDestination = false;

        Debug.Log($"Ball forced to stop at destination: {pos}");
    }
    
    // Temporarily make the ball kinematic to prevent any physics from moving it
    IEnumerator TemporaryKinematic()
    {
        bool wasKinematic = _rb.isKinematic;
        _rb.isKinematic = true;
        
        // Wait a few physics frames to ensure everything settles
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        
        // Restore original kinematic state
        _rb.isKinematic = wasKinematic;
        
        // Ensure velocity is still zero after restoring physics
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    // Handle collisions to prevent bouncing
    void OnCollisionEnter(Collision collision)
    {
        if (!_hasDestination) return;

        // Check if this is a ground collision
        bool isGroundCollision = false;
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.7f) // Surface is mostly horizontal
            {
                isGroundCollision = true;
                break;
            }
        }

        if (isGroundCollision)
        {
            // Stop vertical velocity to prevent bouncing but keep horizontal movement and spin
            Vector3 vel = _rb.velocity;
            vel.y = 0f;
            _rb.velocity = vel;
            // Don't touch angular velocity - let the ball spin naturally!
            _isGrounded = true;
            
            return;
        }

        // For other collisions (walls, players, etc.), apply gentle correction only if close to destination
        Vector3 horizontalDelta = new Vector3(destination.x - transform.position.x, 0f, destination.z - transform.position.z);
        if (horizontalDelta.magnitude <= stopDistance * 2f)
        {
            ForceStopAtDestination();
            return;
        }

        // Reduce velocity after non-ground collision but maintain some spin
        _rb.velocity *= 0.8f;
        _rb.angularVelocity *= 0.9f; // Slight reduction but keep spinning
    }

    // Public method to set destination and reset tracking
    public void SetDestination(Vector3 newDestination)
    {
        // Don't set destination if someone already has ball possession
        if (_ballOwnership != null && _ballOwnership.ballOwner != null)
        {
            Debug.Log($"Cannot set destination - ball is owned by {_ballOwnership.ballOwner.name}");
            return;
        }

        destination = newDestination;
        _hasDestination = true;
        _lastDestination = newDestination;
        _initialDirection = (destination - transform.position).normalized;
        _lastValidPosition = transform.position;
        _lastGuidanceTime = 0f; // Allow immediate guidance
        FindGroundHeight(); // Update ground height for new destination
        
        // Add natural rolling angular velocity based on horizontal movement
        StartCoroutine(AddRollingMotion());
    }
    
    // Add realistic rolling motion to the ball
    IEnumerator AddRollingMotion()
    {
        // Wait a frame to let the initial force be applied
        yield return new WaitForFixedUpdate();
        
        // Calculate rolling angular velocity based on horizontal movement
        Vector3 horizontalVelocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        if (horizontalVelocity.magnitude > 0.1f)
        {
            // Calculate proper rolling angular velocity (v = ωr, so ω = v/r)
            float ballRadius = GetComponent<SphereCollider>().radius * transform.localScale.x;
            float angularSpeed = horizontalVelocity.magnitude / ballRadius;
            
            // Direction of rotation (perpendicular to movement direction)
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, horizontalVelocity.normalized);
            Vector3 angularVelocity = rotationAxis * angularSpeed;
            
            // Apply the angular velocity
            _rb.angularVelocity = angularVelocity;
        }
    }

    // Public method to clear destination
    public void ClearDestination()
    {
        destination = Vector3.zero;
        _hasDestination = false;
    }

    // Public method to check if ball has an owner
    public bool HasOwner()
    {
        return _ballOwnership != null && _ballOwnership.ballOwner != null;
    }

    // Public method to get current ball owner
    public GameObject GetBallOwner()
    {
        return _ballOwnership != null ? _ballOwnership.ballOwner : null;
    }

    void OnDrawGizmosSelected()
    {
        if (_hasDestination)
        {
            // Draw destination
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(destination, stopDistance);
            
            // Draw line to destination
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destination);
            
            // Draw ground level
            Gizmos.color = Color.green;
            Vector3 groundPos = transform.position;
            groundPos.y = _groundHeight;
            Gizmos.DrawWireCube(groundPos, new Vector3(1f, 0.02f, 1f));
        }
    }
}