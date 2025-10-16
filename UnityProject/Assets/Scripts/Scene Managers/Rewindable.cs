using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

/// <summary>
/// Provides pause/resume functionality for game objects during timeline control.
/// Manages physics, animation, and transform state preservation when the simulation is paused.
/// Essential for the rewind/replay system and synchronized pause functionality across multiplayer sessions.
/// </summary>
public class Rewindable : MonoBehaviour
{
    #region Inspector Fields
    /// <summary>
    /// Whether this object's transform should be affected by rewind operations
    /// </summary>
    [SerializeField] bool RewindTransform = true;
    
    /// <summary>
    /// Whether this object can be paused by the timeline manager
    /// </summary>
    [SerializeField] public bool Pausible = true;
    #endregion

    #region Private Fields
    /// <summary>
    /// Saved physics velocity for restoration after unpause
    /// </summary>
    private Vector3 savedVelocity;
    
    /// <summary>
    /// Saved position for restoration after unpause
    /// </summary>
    private Vector3 savedPosition;
    
    /// <summary>
    /// Saved angular velocity for restoration after unpause
    /// </summary>
    private Vector3 savedAngularVelocity;
    
    /// <summary>
    /// Saved rigidbody constraints for restoration after unpause
    /// </summary>
    private RigidbodyConstraints savedConstraints;
    
    /// <summary>
    /// Saved kinematic state for restoration after unpause
    /// </summary>
    private bool isKinematic;
    
    /// <summary>
    /// Current pause state of this object
    /// </summary>
    private bool Paused = false;
    #endregion

    #region Unity Lifecycle
    public void Update()
    {
        // Maintain position for paused player objects
        if (Paused)
        {
            if (this.GetComponent<PlayerInterface>() == true)
            {
                transform.position = savedPosition;
            }
        }
    }
    #endregion

    #region Timeline Control
    /// <summary>
    /// Applies a moment snippet for rewind/replay functionality
    /// </summary>
    /// <param name="snippet">Transform data to apply</param>
    public void ApplySnippet(MomentSnippet snippet)
    {
        transform.position = snippet.position;
        transform.rotation = snippet.rotation;
    }

    /// <summary>
    /// Freezes the object by stopping physics and animations
    /// </summary>
    public void Freeze()
    {
        FreezePhysics();
        FreezeAnimation();
        Paused = true;
    }

    /// <summary>
    /// Unfreezes the object by restoring physics and animations
    /// </summary>
    public void Unfreeze()
    {
        UnfreezePhysics();
        UnfreezeAnimation();
        Paused = false;
    }
    #endregion

    #region Physics Management
    /// <summary>
    /// Saves current physics state and freezes all rigidbody movement
    /// </summary>
    private void FreezePhysics()
    {
        if (GetComponent<Rigidbody>() == null)
        {
            return;
        }
        
        Rigidbody r = GetComponent<Rigidbody>();

        // Save current physics state
        savedVelocity = r.linearVelocity;
        savedAngularVelocity = r.angularVelocity;
        savedConstraints = r.constraints;
        isKinematic = r.isKinematic;
        savedPosition = transform.position;
        
        // Freeze physics
        r.linearVelocity = Vector3.zero;
        r.isKinematic = true;
        r.constraints = RigidbodyConstraints.FreezePosition;
    }

    /// <summary>
    /// Restores saved physics state and resumes rigidbody movement
    /// </summary>
    private void UnfreezePhysics()
    {
        if (GetComponent<Rigidbody>() == null)
        {
            return;
        }
        
        Rigidbody r = GetComponent<Rigidbody>();
        
        // Restore physics state
        r.isKinematic = isKinematic;
        r.constraints = savedConstraints;
        r.linearVelocity = savedVelocity;
        r.angularVelocity = savedAngularVelocity;
        
        r.WakeUp();
    }
    #endregion
    
    #region Animation Management
    /// <summary>
    /// Disables animator and pathfinding components to freeze animations
    /// </summary>
    private void FreezeAnimation()
    {
        if (GetComponent<Animator>() != null)
        {
            GetComponent<Animator>().enabled = false;
        }
        
        if (GetComponent<RichAI>() != null)
        {
            GetComponent<RichAI>().canMove = false;
            
            // Reset AI destination for ball-possessing players
            if (GetComponent<PlayerInterface>() != null && GetComponent<PlayerInterface>().ballPossession)
            {
                AIDestinationSetter dest = GetComponent<AIDestinationSetter>();
                dest.target.localPosition = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// Re-enables animator and pathfinding components to resume animations
    /// </summary>
    private void UnfreezeAnimation()
    {
        if (GetComponent<Animator>() != null)
        {
            GetComponent<Animator>().enabled = true;
        }

        if (GetComponent<RichAI>() != null)
        {
            GetComponent<RichAI>().canMove = true;
        }
    }
    #endregion
}