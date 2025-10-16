using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using Fusion;
using Pathfinding;

/// <summary>
/// Network interface for the soccer ball object in the multiplayer simulation.
/// Handles network synchronization of ball name and registration with the object management system.
/// </summary>
public class BallInterface : NetworkBehaviour
{
    #region Network Properties
    [Networked, OnChangedRender(nameof(UpdateGameObjectName))]
    public NetworkString<_32> ObjName { get; set; }
    #endregion
    
    #region Network Methods
    /// <summary>
    /// Updates the GameObject name when the networked name changes
    /// </summary>
    private void UpdateGameObjectName()
    {
        gameObject.name = ObjName.ToString();
    }
    
    /// <summary>
    /// Sets the object name (only accessible by state authority)
    /// </summary>
    /// <param name="newName">New name for the ball object</param>
    public void SetObjectName(string newName)
    {
        if (Object.HasStateAuthority)
        {
            ObjName = newName;
        }
    }
    
    /// <summary>
    /// Network RPC to register ball in the global object list
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_InstantiateValues()
    {
        ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        objectList.ballObject = this.gameObject;
        objectList.scenicObjects.Add(this.gameObject);
    }
    #endregion
}