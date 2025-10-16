using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Positions objects relative to VR camera for optimal VR interaction and visibility.
/// Maintains consistent positioning in front of the VR user at a fixed height and distance.
/// Only operates for host players to prevent conflicts in multiplayer scenarios.
/// Automatically tracks VR camera movement and updates object position accordingly.
/// </summary>
public class BallPosition : MonoBehaviour
{
    [Header("VR Camera Configuration")]
    [Tooltip("Reference to the VR center eye camera")]
    public Transform vrCamera; 
    
    [Header("Positioning Settings")]
    [Tooltip("Distance forward from camera to position object")]
    public float forwardDistance = 0.34f;
    
    [Tooltip("Fixed Y-axis height for object placement")]
    public float fixedY = 0.18f;
    
    [Header("System References")]
    private GameManager gm;

    #region Initialization

    /// <summary>
    /// Initialize VR camera reference and game manager
    /// </summary>
    private void Start()
    {
        vrCamera = GameObject.Find("CenterEyeAnchor").transform;
        gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    #endregion

    #region Position Updates

    /// <summary>
    /// Update object position relative to VR camera every frame
    /// Calculates forward position from camera with fixed height
    /// Only executes for host players to prevent multiplayer conflicts
    /// </summary>
    void Update()
    {
        // Validate camera reference and host status
        if (vrCamera == null || !gm.isHost)
        {
            return;
        }

        // Calculate target position forward from VR camera
        Vector3 forwardPosition = vrCamera.position + vrCamera.forward * forwardDistance;
        
        // Override Y position with fixed height
        forwardPosition.y = fixedY;

        // Apply calculated position to this GameObject
        transform.position = forwardPosition;
    }

    #endregion
}