using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;

/// <summary>
/// Interface for AI-controlled objects that receive movement data from Scenic simulation.
/// Handles the application of movement commands through reflection-based method invocation.
/// </summary>
public class AIInterface : MonoBehaviour
{
    #region Private Fields
    /// <summary>
    /// Internal tick counter (not synchronized with global tick system)
    /// </summary>
    private int localTick;

    /// <summary>
    /// Reference to the ActionAPI component for executing movement commands
    /// </summary>
    public ActionAPI actionAPI;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        
    }

    void Update()
    {
        
    }
    #endregion
    
    #region Public Methods
    /// <summary>
    /// Applies movement data received from Scenic simulation to the AI object.
    /// Uses reflection to dynamically invoke action methods on the ActionAPI component.
    /// </summary>
    /// <param name="data">Movement data containing action function name and parameters</param>
    public void ApplyMovement(ScenicMovementData data)
    {
        localTick += 1;
        
        // Skip first few ticks to allow proper initialization
        if (localTick < 4)
        {
            return;
        }

        if (data.actionFunc != null)
        {
            Type type = actionAPI.GetType();
            MethodInfo method = type.GetMethod(data.actionFunc);
            
            Debug.LogError("im in here");

            method.Invoke(actionAPI, data.actionArgs.ToArray());
        }
        else // Default to idle state
        {
            Debug.LogError("im in here2");
            actionAPI.stopMovement = true;
        }
    }
    #endregion
}