using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages screen fade effects for smooth transitions during teleportation and scene changes.
/// Provides fade-to-black effects that mask instant position changes to prevent disorientation in VR.
/// Automatically handles fade timing and integrates with human interface transform updates.
/// </summary>
public class Fade : MonoBehaviour
{
    [Header("Fade Configuration")]
    [SerializeField] 
    [Tooltip("UI Image component used for blackout overlay")]
    public Image blackout;
    
    [Tooltip("Whether fade effect is currently active")]
    public bool fade;
    
    [Tooltip("Speed of fade transition (higher = faster)")]
    public float fadeRate;
    
    #region Initialization

    /// <summary>
    /// Initialize fade settings and locate blackout overlay component
    /// </summary>
    void Start()
    {
        fade = false;
        fadeRate = 10.0f;
        
        // Find blackout image if not assigned
        if (blackout == null)
        {
            blackout = GameObject.FindGameObjectWithTag("MainCamera").GetComponentInChildren<Image>();
        }
    }

    /// <summary>
    /// Ensure blackout reference is maintained during runtime
    /// </summary>
    private void Update()
    {
        // Re-establish blackout reference if lost
        if (blackout == null)
        {
            blackout = GameObject.FindGameObjectWithTag("MainCamera").GetComponentInChildren<Image>();
        }
    }

    #endregion

    #region Fade Processing

    /// <summary>
    /// Process fade effect animation and handle fade completion
    /// Only operates on human-tagged objects to prevent interference with other components
    /// </summary>
    void LateUpdate()
    {
        // Only process fade for human objects
        if (!gameObject.CompareTag("human"))
        {
            return;
        }
        
        Color currentColor = blackout.color;
        
        if (fade)
        {
            // Fade to black (increase alpha)
            currentColor.a = Mathf.Lerp(currentColor.a, 1.0f, fadeRate * Time.deltaTime);
            
            // Complete fade when nearly opaque
            if (currentColor.a >= 0.999f)
            {
                fade = false;
                currentColor.a = 0.0f; // Reset to transparent
            }
        }
        
        blackout.color = currentColor;
    }

    #endregion

    #region Public Interface

    /// <summary>
    /// Start fade effect and move player to new position and rotation
    /// Provides smooth transition by masking instant position change with fade
    /// </summary>
    /// <param name="pos">Target position to move to</param>
    /// <param name="rot">Target rotation to apply</param>
    public void StartFadeAndMove(Vector3 pos, Quaternion rot)
    {
        fade = true;
        StartCoroutine(UpdateFade());
        
        HumanInterface humanInterface = GetComponent<HumanInterface>();
        humanInterface.SetTransform(pos, rot);
    }
    
    /// <summary>
    /// Start fade effect and move player relative to a target GameObject
    /// Alternative movement method using GameObject reference and offset position
    /// </summary>
    /// <param name="go">Target GameObject to move relative to</param>
    /// <param name="pos">Offset position from the target GameObject</param>
    public void StartFadeAndMove2(GameObject go, Vector3 pos)
    {
        fade = true;
        StartCoroutine(UpdateFade());
        
        HumanInterface humanInterface = GetComponent<HumanInterface>();
        humanInterface.SetTransform2(go, pos);
    }
    
    /// <summary>
    /// Coroutine to handle fade timing coordination
    /// Provides minimal delay for fade effect initialization
    /// </summary>
    /// <returns>Coroutine enumerator</returns>
    IEnumerator UpdateFade()
    {
        yield return new WaitForSeconds(0.1f);
    }

    #endregion
}