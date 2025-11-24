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

public class BoxInterface : NetworkBehaviour
{
    #region Network Properties
    [Networked, OnChangedRender(nameof(UpdateGameObjectName))]
    public NetworkString<_32> ObjName { get; set; }
    
    [Networked] public NetworkBool isPackaged { get; set; }
    [Networked] public NetworkBool isPossessed { get; set; }
    #endregion

    private void Start()
    {
        RegisterObject();
    }
    
    private void RegisterObject()
    {
        ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        if (this.gameObject != null && !objectList.scenicObjects.Contains(this.gameObject))
        {
            objectList.scenicObjects.Add(this.gameObject);
        }
        
    }
    
    #region Network Methods
    private void UpdateGameObjectName()
    {
        gameObject.name = ObjName.ToString();
    }
    
    public void SetObjectName(string newName)
    {
        if (Object.HasStateAuthority)
        {
            ObjName = newName;
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_InstantiateValues()
    {
        ObjectsList objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        objectList.ballObject = this.gameObject;
        objectList.scenicObjects.Add(this.gameObject);
    }
    #endregion
}