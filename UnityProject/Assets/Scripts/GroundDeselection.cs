using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GroundDeselection : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // if (eventData.button == PointerEventData.InputButton.Right)
        Destroy(this.gameObject);
    }
    
}
