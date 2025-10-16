using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UltimateReplay.Formatters;

/// <summary>
/// Manages floating text UI elements that follow game objects and adjust their orientation based on viewing mode.
/// Handles different display behaviors for VR (billboard style) vs desktop (fixed orientation) viewing.
/// Automatically positions text relative to parent objects and manages visibility in VR mode.
/// </summary>
public class FloatingText : MonoBehaviour
{
    [Header("Positioning Configuration")]
    [Tooltip("Offset from parent object position for text placement")]
    public Vector3 offset = new Vector3(0, 2, 1);
    
    /// <summary>
    /// Update text rotation and position based on viewing mode and parent object
    /// Handles VR billboard rotation vs fixed desktop orientation
    /// </summary>
    void Update()
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        
        // Handle rotation based on viewing mode
        if (gm.isHost && !gm.laptopMode) // VR VIEW: Billboard rotation to face camera
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
        else // DESKTOP VIEW: Fixed rotation
        {
            transform.localRotation = Quaternion.Euler(90, 0, 0);
            transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        // Handle visibility and positioning for VR users
        HumanInterface hI = transform.parent.GetComponent<HumanInterface>();
        if (gm.isHost && hI != null && hI.isVR)
        {
            // Hide text mesh for VR users to prevent visual clutter
            this.gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
        
        // Position text relative to parent object
        transform.position = this.transform.parent.position + offset;
        
        // Use VR transform position if available for more accurate placement
        if (hI != null && hI.isVR)
        {
            transform.position = hI.vrTransform.position + offset;
        }
    }
    
    /// <summary>
    /// Set the displayed text with bracket formatting
    /// </summary>
    /// <param name="text">Text content to display</param>
    public void SetText(string text)
    {
        GetComponent<TextMesh>().text = "[ " + text + " ]";
    }
    
    /// <summary>
    /// Set the displayed text without additional formatting
    /// </summary>
    /// <param name="text">Text content to display</param>
    public void SetText2(string text)
    {
        GetComponent<TextMesh>().text = text;
    }
}