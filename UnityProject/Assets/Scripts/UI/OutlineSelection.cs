using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OutlineSelection : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;
    public bool stayOutlined = false;
    private KeyboardInput keyboardInput;
    
    private void Start()
    {
        outline = gameObject.GetComponent<Outline>();
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (keyboardInput.canClick)
        {
            // stayOutlined = !stayOutlined;
            GameObject human = GameObject.FindGameObjectWithTag("human");
            if (human != null)
            {
                StartCoroutine(human.GetComponent<HumanInterface>().SetTriggerPass());
            }
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
        if (keyboardInput.canClick)
        {
            // CLICKED
            // keyboardInput.HandleAnnotationClick();
        }
    }
    
    public void OnRayEnter()
    {
        outline.enabled = true;
    }
    
    public void OnRayExit()
    {
        if (stayOutlined == false)
        {
            outline.enabled = false;
        }
    }
#endif
    
}
