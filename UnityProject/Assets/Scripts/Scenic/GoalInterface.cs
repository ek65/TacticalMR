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

// TODO: Rename script, this is the player logic script
public class GoalInterface : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(UpdateGameObjectName))]
    public NetworkString<_32> ObjName { get; set; }
    
    private void UpdateGameObjectName()
    {
        gameObject.name = ObjName.ToString();
    }
    
    public void SetObjectName(string newName)
    {
        if (Object.HasStateAuthority) // Only the host or owner should update this
        {
            ObjName = newName; // This will trigger OnNameChanged() on all clients
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_InstantiateValues()
    {
        ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        objectList.goalObject = this.gameObject;
        objectList.scenicObjects.Add(this.gameObject);
    }
}