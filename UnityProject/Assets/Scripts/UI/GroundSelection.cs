using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using OpenAI.Samples.Chat;

/// <summary>
/// Manages ground position selection and highlighting for both desktop and VR platforms.
/// Handles raycasting, visual feedback, and placement of position markers on the ground plane.
/// Supports different interaction methods: mouse clicks for desktop and ray interactions for VR.
/// Integrates with annotation and input systems for recording user interactions.
/// </summary>
public class GroundSelection : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Feedback Objects")]
    [Tooltip("Highlighter that follows mouse/ray cursor")]
    public GameObject groundHighlighter;
    
    [Tooltip("Template for new ground highlight markers")]
    public GameObject newGroundHighlighter;
    
    [Tooltip("Currently placed ground highlight marker")]
    public GameObject placedGroundHighlighter;

    [Header("Interaction Components")]
    private Camera cam;
    private RaycastHit raycastHit;
    
    [Header("System References")]
    private KeyboardInput keyboardInput;
    private ProgramSynthesisManager programSynthesisManager;
    private TimelineManager tlManager;

    /// <summary>
    /// Initialize component references and camera setup
    /// </summary>
    private void Start()
    {
        cam = Camera.main;
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();
    }

    /// <summary>
    /// Handle real-time ground highlighting and raycasting for different platforms
    /// Updates highlighter position based on mouse or VR ray intersection with ground
    /// </summary>
    private void Update()
    {
        // Ensure camera reference is valid
        if (cam == null)
        {
            cam = Camera.main;
        }

        #if UNITY_EDITOR
        HandleDesktopRaycasting();
        #endif
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        HandleVRRaycasting();
        #endif
    }

    #region Platform-Specific Raycasting

    /// <summary>
    /// Handle raycasting for desktop/editor mode using mouse position
    /// </summary>
    private void HandleDesktopRaycasting()
    {
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            
            // Temporarily disable highlighter collider to prevent self-intersection
            groundHighlighter.GetComponent<Collider>().enabled = false;
            
            if (Physics.Raycast(ray, out raycastHit))
            {
                if (raycastHit.transform.gameObject.CompareTag("Ground"))
                {
                    
                    if (groundHighlighter.TryGetComponent<Fusion.NetworkObject>(out var no) &&
                        groundHighlighter.TryGetComponent<Fusion.NetworkTransform>(out var nt))
                    {
                        if (no.HasStateAuthority)
                        {
                            // Updates both the Transform and the replicated state immediately,
                            // so NT won't overwrite you later this frame.
                            nt.Teleport(raycastHit.point, groundHighlighter.transform.rotation);
                        }
                        // else: don't move it here; send the point to the authority (RPC/Networked var)
                    }
                    else
                    {
                        // Non-networked fallback
                        groundHighlighter.transform.position = raycastHit.point;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handle raycasting for VR mode using hand ray interaction
    /// </summary>
    private void HandleVRRaycasting()
    {
        GameObject human = GameObject.FindGameObjectWithTag("human");
        if (human == null) return;

        // Only process ray for VR users who are not viewers
        if (human.GetComponent<HumanInterface>().isVR && !human.GetComponent<HumanInterface>().isViewer)
        {
            Ray ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
            
            // Temporarily disable highlighter collider to prevent self-intersection
            groundHighlighter.GetComponent<Collider>().enabled = false;
            
            if (Physics.Raycast(ray, out raycastHit))
            {
                if (raycastHit.transform.gameObject.CompareTag("Ground"))
                {
                    groundHighlighter.transform.position = raycastHit.point;
                }
            }
        }
    }

    #endregion

    #region Ground Highlight Management

    /// <summary>
    /// Clear all ground highlights and reset position markers
    /// Removes visual markers and resets human interface position references
    /// </summary>
    public void ClearGroundHighlights()
    {
        GameObject human = GameObject.FindGameObjectWithTag("human");
        if (human != null)
        {
            human.GetComponent<HumanInterface>().xMark = Vector3.zero;
        }
        
        if (placedGroundHighlighter != null)
        {
            Destroy(placedGroundHighlighter);
        }
    }

    #endregion

    #region Desktop Mouse Interaction Events

    /// <summary>
    /// Handle mouse click events for desktop ground selection
    /// Places position markers and triggers input handling
    /// </summary>
    /// <param name="eventData">Mouse event data</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        
        // Only process clicks for host users
        if (gm.isHost && programSynthesisManager.canClick)
        {
            PlaceGroundMarker();
            keyboardInput.HandlePositionClick();
        }
    }

    /// <summary>
    /// Handle mouse enter events to show ground highlighter
    /// </summary>
    /// <param name="eventData">Mouse event data</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            groundHighlighter.SetActive(true);
        }
    }
    
    /// <summary>
    /// Handle mouse exit events to hide ground highlighter
    /// </summary>
    /// <param name="eventData">Mouse event data</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            groundHighlighter.SetActive(false);
        }
    }

    #endregion

    #region VR Ray Interaction Events

    #if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Handle VR ray click events for ground selection
    /// Uses networking to spawn markers in multiplayer scenarios
    /// </summary>
    public void OnRayClick()
    {
        if (keyboardInput.canClick)
        {
            // Clear existing marker
            if (placedGroundHighlighter != null)
            {
                Destroy(placedGroundHighlighter);
            }
            
            // Spawn networked ground marker for VR
            NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
            NetworkObject temp = runner.Spawn(newGroundHighlighter, raycastHit.point, Quaternion.identity);
            
            GameObject go = temp.gameObject;
            placedGroundHighlighter = go;
            go.GetComponent<Collider>().enabled = true;
            
            RPC_RayClick(temp);
            keyboardInput.HandlePositionClick();
        }
    }
    
    /// <summary>
    /// Handle VR ray enter events to show ground highlighter
    /// </summary>
    public void OnRayEnter()
    {
        groundHighlighter.SetActive(true);
    }
    
    /// <summary>
    /// Handle VR ray exit events to hide ground highlighter
    /// </summary>
    public void OnRayExit()
    {
        groundHighlighter.SetActive(false);
    }
    #endif

    #endregion

    #region Helper Methods

    /// <summary>
    /// Place a ground marker at the current raycast hit point
    /// Used by desktop mouse interaction
    /// </summary>
    private void PlaceGroundMarker()
    {
        // Remove existing marker
        if (placedGroundHighlighter != null)
        {
            Destroy(placedGroundHighlighter);
        }
        
        // Create new marker at hit point
        GameObject go = Instantiate(newGroundHighlighter, raycastHit.point, Quaternion.identity);
        placedGroundHighlighter = go;
        go.GetComponent<Collider>().enabled = true;
        
        // record a hint click for the next mic submission
        ChatBehaviour.Instance?.RegisterClick(raycastHit.point);
    }

    /// <summary>
    /// Network RPC to configure spawned ground markers
    /// Ensures proper collider state across network clients
    /// </summary>
    /// <param name="obj">The networked object to configure</param>
    private void RPC_RayClick(NetworkObject obj)
    {
        GameObject go = obj.gameObject;
        go.GetComponent<Collider>().enabled = true;
    }

    #endregion
}