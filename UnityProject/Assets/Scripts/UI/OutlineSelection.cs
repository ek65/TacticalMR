using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;

public class OutlineSelection : NetworkBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;
    public bool stayOutlined = false;
    private KeyboardInput keyboardInput;
    private ProgramSynthesisManager programSynthesisManager;
    
    private void Start()
    {
        outline = gameObject.GetComponent<Outline>();
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (programSynthesisManager.canClick)
        {
            // stayOutlined = !stayOutlined;
            keyboardInput.HandleAnnotationClick();
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        outline.enabled = true;
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (stayOutlined == false)
        {
            outline.enabled = false;
        }
    }
    
#if UNITY_ANDROID
    public void OnRayClick()
    {
        if (programSynthesisManager.canClick)
        {
            // CLICKED
            keyboardInput.HandleAnnotationClick();
        }
    }
    
    public void OnRayEnter()
    {
        // outline.enabled = true;
        // RPC_RayEnter();
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RayEnter()
    {
        outline.enabled = true;
    }
    
    public void OnRayExit()
    {
        // if (stayOutlined == false)
        // {
        //     outline.enabled = false;
        // }
        RPC_RayExit();
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RayExit()
    {
        if (stayOutlined == false)
        {
            outline.enabled = false;
        }
    }
#endif
    
}
