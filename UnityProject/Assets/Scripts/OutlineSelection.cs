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
            stayOutlined = !stayOutlined;
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
    
}
