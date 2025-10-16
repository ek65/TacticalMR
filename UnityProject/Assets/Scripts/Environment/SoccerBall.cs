using UnityEngine;
using System.Collections;

/// <summary>
/// Advanced soccer ball physics system with intelligent destination seeking and realistic ground behavior.
/// Provides guided ball movement toward destinations while maintaining natural rolling physics.
/// Handles ground detection, bounce prevention, possession checking, and smooth stopping mechanics.
/// Integrates with ball ownership system to respect player possession and prevent conflicting movement.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SoccerBall : MonoBehaviour
{
    [Header("Destination Control")]
    [Tooltip("Target position for the ball to move toward")]
    public Vector3 destination;
    
    [Tooltip("Distance threshold for considering destination reached")]
    public float stopDistance = 0.3f;
    
    [Tooltip("Velocity threshold below which ball stops moving")]
    public float minVelocityThreshold = 0.1f;

    [Header("Movement Guidance")]
    [Tooltip("Force applied to guide ball toward destination")]
    public float guidanceForce = 3f;
    
    [Tooltip("Interval between guidance force applications")]
    public float guidanceInterval = 0.1f;
    
    [Tooltip("Maximum distance before applying strong correction")]
    public float maxDeviationDistance = 2f;

    [Header("Ground Interaction")]
    [Tooltip("Layer mask for ground detection raycasting")]
    public LayerMask groundLayerMask = 1;
    
    [Tooltip("Height above ground to maintain")]
    public float groundOffset = 0.25f;
    
    [Tooltip("Maximum upward velocity allowed (prevents excessive bouncing)")]
    public float maxUpwardVelocity = 2f;

    [Header("Internal State")]
    private Rigidbody _rb;
    private BallOwnership _ballOwnership;
    private float _stopDistanceSqr;
    private Vector3 _lastDestination;
    private bool _hasDestination;
    private float _lastGuidanceTime;
    private Vector3 _initialDirection;
    private Vector3 _lastValidPosition;
    private float _groundHeight;
    private bool _isGrounded;

    #region Initialization

    /// <summary>
    /// Initialize ball components and calculate derived values
    /// </summary>
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _ballOwnership = GetComponent<BallOwnership>();
        _stopDistanceSqr = stopDistance * stopDistance;
        FindGroundHeight();
    }

    #endregion

    #region Core Update Loop

    /// <summary>
    /// Main physics update loop handling ground management and destination seeking
    /// Processes ball movement, ownership checks, and guidance application
    /// </summary>
    void FixedUpdate()
    {
        // Always manage ground position and prevent excessive bouncing
        ManageGroundPosition();

        // Check for ball possession - halt destination seeking if owned
        if (_ballOwnership != null && _ballOwnership.ballOwner != null)
        {
            if (_hasDestination)
            {
                destination = Vector3.zero;
                _hasDestination = false;
                Debug.Log($"Ball possession gained by {_ballOwnership.ballOwner.name} - destination cleared");
            }
            return;
        }

        // Handle destination changes and updates
        HandleDestinationChanges();

        // Process destination seeking if active
        if (_hasDestination)
        {
            ProcessDestinationSeeking();
        }
    }

    #endregion

    #region Destination Management

    /// <summary>
    /// Handle destination updates and initialization
    /// </summary>
    private void HandleDestinationChanges()
    {
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
    }

    /// <summary>
    /// Process movement toward destination with intelligent stopping
    /// </summary>
    private void ProcessDestinationSeeking()
    {
        // Calculate horizontal distance to destination (ignore Y for stopping)
        Vector3 delta = destination - transform.position;
        Vector3 horizontalDelta = new Vector3(delta.x, 0f, delta.z);
        float distanceToDestination = horizontalDelta.magnitude;

        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Check stopping conditions
        bool shouldStop = CheckIfShouldStop(horizontalDelta, currentSpeed, distanceToDestination);
        bool hasOvershot = CheckIfOvershot(horizontalDelta, currentSpeed, distanceToDestination);
        
        if (shouldStop || hasOvershot)
        {
            ForceStopAtDestination();
            return;
        }

        // Apply guidance to maintain course
        ApplyGentleGuidance(horizontalDelta, distanceToDestination);
    }

    #endregion

    #region Ground Management

    /// <summary>
    /// Manage ball ground positioning and prevent excessive bouncing
    /// Maintains consistent height above ground while preserving rolling physics
    /// </summary>
    void ManageGroundPosition()
    {
        FindGroundHeight();
        
        // Limit excessive upward velocity
        if (_rb.linearVelocity.y > maxUpwardVelocity)
        {
            Vector3 vel = _rb.linearVelocity;
            vel.y = Mathf.Min(vel.y, maxUpwardVelocity);
            _rb.linearVelocity = vel;
        }

        // Calculate height adjustment needs
        float currentHeight = transform.position.y;
        float targetHeight = _groundHeight + groundOffset;
        float heightDifference = currentHeight - targetHeight;
        
        // Handle different height scenarios
        if (heightDifference > groundOffset * 2f && _rb.linearVelocity.y <= 0)
        {
            // Ball falling from high - let physics handle naturally
            _isGrounded = false;
        }
        else if (heightDifference < -0.05f || (heightDifference > 0.05f && heightDifference < 0.15f && _rb.linearVelocity.y <= 0))
        {
            // Gently adjust position to maintain proper ground height
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, targetHeight, Time.fixedDeltaTime * 5f);
            transform.position = pos;
            
            // Stop upward velocity while preserving horizontal motion
            if (_rb.linearVelocity.y > 0.1f)
            {
                Vector3 vel = _rb.linearVelocity;
                vel.y = 0f;
                _rb.linearVelocity = vel;
            }
            
            _isGrounded = true;
        }
        else if (Mathf.Abs(heightDifference) <= 0.05f)
        {
            _isGrounded = true;
        }
    }

    /// <summary>
    /// Raycast to find current ground height beneath the ball
    /// </summary>
    void FindGroundHeight()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, 10f, groundLayerMask))
        {
            _groundHeight = hit.point.y;
        }
        else
        {
            // Fallback if no ground detected
            _groundHeight = transform.position.y - groundOffset;
        }
    }

    #endregion

    #region Movement Decision Logic

    /// <summary>
    /// Determine if the ball should stop based on distance and velocity
    /// </summary>
    /// <param name="horizontalDelta">Horizontal vector to destination</param>
    /// <param name="currentSpeed">Current horizontal speed</param>
    /// <param name="distanceToDestination">Distance to destination</param>
    /// <returns>True if ball should stop</returns>
    bool CheckIfShouldStop(Vector3 horizontalDelta, float currentSpeed, float distanceToDestination)
    {
        // Stop if very close to destination
        if (distanceToDestination <= stopDistance)
        {
            return true;
        }

        // Stop if moving slowly and reasonably close
        if (currentSpeed < minVelocityThreshold && distanceToDestination <= stopDistance * 2f)
        {
            return true;
        }

        // Predict and prevent overshoot
        if (currentSpeed > 0.1f)
        {
            Vector3 nextFramePosition = transform.position + new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z) * Time.fixedDeltaTime;
            Vector3 nextFrameDelta = destination - nextFramePosition;
            nextFrameDelta.y = 0f;
            
            // Stop if next frame will be farther from destination
            if (nextFrameDelta.magnitude > distanceToDestination && distanceToDestination <= stopDistance * 1.5f)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if the ball has overshot its destination
    /// </summary>
    /// <param name="horizontalDelta">Horizontal vector to destination</param>
    /// <param name="currentSpeed">Current horizontal speed</param>
    /// <param name="distanceToDestination">Distance to destination</param>
    /// <returns>True if ball has overshot</returns>
    bool CheckIfOvershot(Vector3 horizontalDelta, float currentSpeed, float distanceToDestination)
    {
        // Only check for overshoot when moving and close to destination
        if (currentSpeed < 0.1f || distanceToDestination > stopDistance * 3f)
        {
            return false;
        }

        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        
        // Check if moving away from destination (negative dot product)
        float dotProduct = Vector3.Dot(horizontalVelocity.normalized, horizontalDelta.normalized);
        
        if (dotProduct < -0.3f && distanceToDestination <= stopDistance * 2f)
        {
            Debug.Log($"Ball overshot destination! Distance: {distanceToDestination:F3}, Dot: {dotProduct:F3}");
            return true;
        }

        // Additional check for high speed when very close
        if (distanceToDestination <= stopDistance * 0.5f && currentSpeed > minVelocityThreshold * 2f)
        {
            Debug.Log($"Ball very close to destination with high speed - stopping to prevent overshoot");
            return true;
        }

        return false;
    }

    #endregion

    #region Movement Forces

    /// <summary>
    /// Apply gentle guidance forces to keep ball on course toward destination
    /// Uses reduced force to maintain natural physics while providing directional guidance
    /// </summary>
    /// <param name="horizontalDelta">Vector toward destination</param>
    /// <param name="distanceToDestination">Distance to destination</param>
    void ApplyGentleGuidance(Vector3 horizontalDelta, float distanceToDestination)
    {
        // Only apply guidance at intervals and when grounded
        if (Time.time - _lastGuidanceTime < guidanceInterval || !_isGrounded)
            return;

        _lastGuidanceTime = Time.time;

        Vector3 desiredDirection = horizontalDelta.normalized;
        Vector3 currentHorizontalVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        Vector3 currentDirection = currentHorizontalVelocity.normalized;

        // Calculate gentle guidance force
        float gentleForce = guidanceForce * 0.3f; // Reduced for subtlety
        
        // Increase force if far from destination
        if (distanceToDestination > maxDeviationDistance)
        {
            gentleForce = guidanceForce * 0.6f;
        }

        Vector3 guidanceForceVector = desiredDirection * gentleForce;
        
        // Apply gentle braking if moving in wrong direction
        if (Vector3.Dot(currentDirection, desiredDirection) < 0f && currentHorizontalVelocity.magnitude > 1f)
        {
            Vector3 brakingForce = -currentHorizontalVelocity.normalized * gentleForce;
            _rb.AddForce(brakingForce, ForceMode.Force);
        }
        
        // Apply directional guidance
        _rb.AddForce(guidanceForceVector, ForceMode.Force);
    }

    #endregion

    #region Stopping Mechanics

    /// <summary>
    /// Force ball to stop at destination with precise positioning
    /// Snaps to destination and eliminates all motion to prevent drift
    /// </summary>
    void ForceStopAtDestination()
    {
        // Snap to destination position
        Vector3 pos = transform.position;
        pos.x = destination.x;
        pos.z = destination.z;
        pos.y = _groundHeight + groundOffset;
        transform.position = pos;

        // Eliminate all motion
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        
        // Temporarily make kinematic to prevent physics interference
        StartCoroutine(TemporaryKinematic());

        // Clear destination
        destination = Vector3.zero;
        _hasDestination = false;

        Debug.Log($"Ball forced to stop at destination: {pos}");
    }
    
    /// <summary>
    /// Temporarily disable physics to ensure clean stopping
    /// </summary>
    /// <returns>Coroutine enumerator</returns>
    IEnumerator TemporaryKinematic()
    {
        bool wasKinematic = _rb.isKinematic;
        _rb.isKinematic = true;
        
        // Wait for physics to settle
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        
        // Restore physics state
        _rb.isKinematic = wasKinematic;
        
        // Ensure complete stillness
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    #endregion

    #region Collision Handling

    /// <summary>
    /// Handle collisions to prevent bouncing while maintaining rolling physics
    /// Distinguishes between ground and obstacle collisions
    /// </summary>
    /// <param name="collision">Collision data</param>
    void OnCollisionEnter(Collision collision)
    {
        if (!_hasDestination) return;

        // Identify ground collisions
        bool isGroundCollision = false;
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.7f)
            {
                isGroundCollision = true;
                break;
            }
        }

        if (isGroundCollision)
        {
            // Stop vertical velocity but preserve horizontal movement
            Vector3 vel = _rb.linearVelocity;
            vel.y = 0f;
            _rb.linearVelocity = vel;
            _isGrounded = true;
            return;
        }

        // Handle obstacle collisions
        Vector3 horizontalDelta = new Vector3(destination.x - transform.position.x, 0f, destination.z - transform.position.z);
        if (horizontalDelta.magnitude <= stopDistance * 2f)
        {
            ForceStopAtDestination();
            return;
        }

        // Reduce velocity after non-ground collision
        _rb.linearVelocity *= 0.8f;
        _rb.angularVelocity *= 0.9f;
    }

    #endregion

    #region Public Interface

    /// <summary>
    /// Set new destination and initialize tracking parameters
    /// Adds natural rolling motion based on movement direction
    /// </summary>
    /// <param name="newDestination">Target position for ball movement</param>
    public void SetDestination(Vector3 newDestination)
    {
        // Prevent destination setting if ball is owned
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
        _lastGuidanceTime = 0f;
        FindGroundHeight();
        
        // Add realistic rolling motion
        StartCoroutine(AddRollingMotion());
    }
    
    /// <summary>
    /// Add realistic rolling angular velocity based on horizontal movement
    /// </summary>
    /// <returns>Coroutine enumerator</returns>
    IEnumerator AddRollingMotion()
    {
        yield return new WaitForFixedUpdate();
        
        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > 0.1f)
        {
            // Calculate proper rolling based on ball radius (v = ωr, so ω = v/r)
            float ballRadius = GetComponent<SphereCollider>().radius * transform.localScale.x;
            float angularSpeed = horizontalVelocity.magnitude / ballRadius;
            
            // Rotation axis perpendicular to movement
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, horizontalVelocity.normalized);
            Vector3 angularVelocity = rotationAxis * angularSpeed;
            
            _rb.angularVelocity = angularVelocity;
        }
    }

    /// <summary>
    /// Clear current destination and stop seeking behavior
    /// </summary>
    public void ClearDestination()
    {
        destination = Vector3.zero;
        _hasDestination = false;
    }

    /// <summary>
    /// Check if ball currently has an owner
    /// </summary>
    /// <returns>True if ball is owned by a player</returns>
    public bool HasOwner()
    {
        return _ballOwnership != null && _ballOwnership.ballOwner != null;
    }

    /// <summary>
    /// Get the current ball owner
    /// </summary>
    /// <returns>GameObject of ball owner, or null if unowned</returns>
    public GameObject GetBallOwner()
    {
        return _ballOwnership != null ? _ballOwnership.ballOwner : null;
    }

    #endregion

    #region Debug Visualization

    /// <summary>
    /// Draw debug gizmos for destination and ground detection
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (_hasDestination)
        {
            // Draw destination sphere
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(destination, stopDistance);
            
            // Draw line to destination
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destination);
            
            // Draw ground level indicator
            Gizmos.color = Color.green;
            Vector3 groundPos = transform.position;
            groundPos.y = _groundHeight;
            Gizmos.DrawWireCube(groundPos, new Vector3(1f, 0.02f, 1f));
        }
    }

    #endregion
}