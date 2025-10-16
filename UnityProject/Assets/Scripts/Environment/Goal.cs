using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles goal scoring detection and visual effects for soccer gameplay.
/// Triggers particle effects when objects with the "goal" tag enter the goal area.
/// Provides immediate visual feedback for successful goal scoring events.
/// </summary>
public class Goal : MonoBehaviour
{
    #region Initialization

    /// <summary>
    /// Initialize goal component - no setup required
    /// </summary>
    void Start()
    {
        // No initialization needed for basic goal detection
    }

    /// <summary>
    /// Update loop - currently unused
    /// </summary>
    void Update()
    {
        // Goal detection handled by collision events
    }

    #endregion

    #region Goal Detection

    /// <summary>
    /// Detect when objects enter the goal area and trigger celebration effects
    /// Activates particle systems on objects tagged as "goal" to provide visual feedback
    /// </summary>
    /// <param name="collision">The collider that entered the goal trigger</param>
    private void OnTriggerEnter(Collider collision)
    {
        // Check if the object has the goal tag (typically the ball)
        if (collision.gameObject.tag == "goal")
        {
            // Trigger particle system for visual celebration
            ParticleSystem particles = collision.gameObject.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                particles.Play();
            }
        }
    }

    #endregion
}