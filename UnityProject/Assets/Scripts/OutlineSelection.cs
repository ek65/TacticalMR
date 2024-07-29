using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OutlineSelection : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;
    public bool stayOutlined = false;
    
    private void Awake()
    {
        outline = gameObject.GetComponent<Outline>();
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        stayOutlined = !stayOutlined;
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
