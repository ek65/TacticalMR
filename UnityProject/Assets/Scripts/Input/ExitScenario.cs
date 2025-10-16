using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;
using Unity.VisualScripting;
using Utilities.Extensions;

/// <summary>
/// Manages scenario termination and VR interaction controls.
/// Handles ending simulation scenarios, toggling VR ray interactions, and managing body visibility.
/// Communicates with Scenic system to signal scenario completion and trigger new simulations.
/// Provides utility functions for VR-specific interaction management.
/// </summary>
public class ExitScenario : MonoBehaviour
{
    [Header("VR Controller Configuration")]
    [Tooltip("Index trigger button for VR interactions")]
    public OVRInput.Button indexTrigger;
    
    [Tooltip("Right hand controller reference")]
    public OVRInput.Controller controllerRight;
    
    [Tooltip("Left hand controller reference")]
    public OVRInput.Controller controllerLeft;
    
    [Tooltip("X button on VR controller")]
    public OVRInput.Button buttonX;
    
    [Tooltip("Y button on VR controller")]
    public OVRInput.Button buttonY;
    
    [Header("System References")]
    private KeyboardInput keyboardInput;

    [Header("Scenario State")]
    [Tooltip("Flag indicating if scenario should end (read by Scenic system)")]
    public bool endScenario;

    #region Initialization

    /// <summary>
    /// Initialize scenario state and component references
    /// </summary>
    void Start()
    {
        endScenario = false;
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
    }

    #endregion

    #region Update Loop

    /// <summary>
    /// Update loop for handling VR controller input
    /// Currently contains placeholder logic for future VR control implementation
    /// </summary>
    void Update()
    {
        // VR controller input handling can be implemented here
        // Currently disabled but ready for future VR control features
    }

    #endregion

    #region VR Interaction Management

    /// <summary>
    /// Toggle the VR ray interactor on/off
    /// Allows users to enable or disable VR pointing/selection functionality
    /// </summary>
    public void ToggleRayInteractor()
    {
        if (GameObject.FindGameObjectWithTag("human") != null)
        {
            RayInteractor rayInteractor = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>();
            rayInteractor.enabled = !rayInteractor.enabled;
        }
    }
    
    /// <summary>
    /// Toggle visibility of the VR user's body representation
    /// Hides or shows the avatar body for the VR user
    /// </summary>
    public void HideBody()
    {
        if (GameObject.FindGameObjectWithTag("human") != null)
        {
            GameObject avatarBody = GameObject.FindGameObjectWithTag("human").transform
                .FindChildRecursive("ArmatureSkinningUpdateRetargetUser").gameObject;
            avatarBody.SetActive(!avatarBody.activeSelf);
        }
    }

    #endregion

    #region Scenario Management

    /// <summary>
    /// End the current scenario and signal for new simulation generation
    /// Sets the endScenario flag that Scenic reads to terminate and generate new simulations
    /// Handles cleanup of visual effects and prepares for scenario transition
    /// </summary>
    public void EndScenario()
    {
        // Stop any active particle effects
        if (GameObject.FindGameObjectWithTag("goal"))
        {
            GameObject.FindGameObjectWithTag("goal").GetComponent<ParticleSystem>().Stop();
        }
        
        // Signal scenario end to Scenic system
        endScenario = true;
        
        // Additional cleanup can be added here for scenario transitions
    }

    #endregion
}