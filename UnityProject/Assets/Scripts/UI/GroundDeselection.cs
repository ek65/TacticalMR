using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles deselection interactions for ground highlight markers.
/// Provides click and ray-based interaction methods to clear ground position highlights.
/// Used on UI elements that allow users to remove position markers from the scene.
/// </summary>
public class GroundDeselection : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// Handle mouse click events to deselect ground highlights
    /// Clears all active ground position markers when clicked
    /// </summary>
    /// <param name="eventData">Event data containing click information</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        ClearGroundHighlights();
    }
    
    /// <summary>
    /// Handle VR ray-based click events to deselect ground highlights
    /// Provides the same functionality as mouse clicks but for VR interaction
    /// </summary>
    public void OnRayClick()
    {
        ClearGroundHighlights();
    }

    /// <summary>
    /// Clear all ground highlights by calling the GroundSelection manager
    /// Removes position markers and resets ground selection state
    /// </summary>
    private void ClearGroundHighlights()
    {
        // var groundSelections = GameObject.FindGameObjectsWithTag("Ground");
        
        // foreach (var gs in groundSelections)
        // {
        //     gs.GetComponent<GroundSelection>().ClearGroundHighlights();
        // }
        Destroy(this.gameObject);
    }
}