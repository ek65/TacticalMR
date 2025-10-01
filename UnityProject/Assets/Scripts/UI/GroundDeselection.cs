using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GroundDeselection : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // if (eventData.button == PointerEventData.InputButton.Right)
        // clicking on the X again should remove it
        GroundSelection groundSelection = GameObject.FindGameObjectWithTag("Ground")
            .GetComponent<GroundSelection>();
        groundSelection.ClearGroundHighlights();
        // Destroy(this.gameObject);
    }
    
    public void OnRayClick()
    {
        // clicking on the X again should remove it
        GroundSelection groundSelection = GameObject.FindGameObjectWithTag("Ground")
            .GetComponent<GroundSelection>();
        groundSelection.ClearGroundHighlights();
        // Destroy(this.gameObject);
    }

    // public void OnTriggerEnter(Collider other)
    // {
    //     if (other.gameObject.CompareTag("human") && !this.gameObject.CompareTag("GroundHover"))
    //     {
    //         Destroy(this.gameObject);
    //     }
    // }
}
