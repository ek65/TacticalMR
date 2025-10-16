using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Fusion;
using TMPro;

/// <summary>
/// Data structure for storing object state at a specific moment in time
/// </summary>
public class MomentSnippet
{
    #region Properties
    /// <summary>
    /// Position at the recorded moment
    /// </summary>
    public Vector3 position;
    
    /// <summary>
    /// Rotation at the recorded moment
    /// </summary>
    public Quaternion rotation;
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a moment snippet with position and rotation data
    /// </summary>
    /// <param name="pos">Position to record</param>
    /// <param name="quat">Rotation to record</param>
    public MomentSnippet(Vector3 pos, Quaternion quat)
    {
        position = pos;
        this.rotation = quat;
    }
    #endregion
}

/// <summary>
/// Stores time series data for rewindable objects (currently unused but preserved for future implementation)
/// </summary>
public class RewindableTimeSeries
{
    #region Private Fields
    private List<Vector3> positions = new List<Vector3>();
    private List<Quaternion> rotations = new List<Quaternion>();
    #endregion

    #region Public Methods
    /// <summary>
    /// Records the current transform state
    /// </summary>
    /// <param name="t">Transform to record</param>
    public void RecordTransform(Transform t)
    {
        positions.Add(t.position);
        rotations.Add(t.rotation);
    }

    /// <summary>
    /// Retrieves a moment snippet from a specific time index
    /// </summary>
    /// <param name="TimeIndex">Index in the time series</param>
    /// <returns>Moment snippet at the specified time</returns>
    public MomentSnippet GetMomentSnippet(int TimeIndex)
    {
        return new MomentSnippet(positions[TimeIndex], rotations[TimeIndex]);
    }
    #endregion
}

/// <summary>
/// Central manager for timeline control, pause/resume functionality, and rewind system.
/// Coordinates with the networking system to synchronize pause states across all clients.
/// Manages recording segments and integrates with the video recording system.
/// </summary>
public class TimelineManager : NetworkBehaviour
{
    #region Public Fields
    /// <summary>
    /// List of all objects that can be paused/rewound
    /// </summary>
    public List<Rewindable> rewindables;
    
    /// <summary>
    /// Whether the timeline is currently in rewind mode
    /// </summary>
    public bool rewinding = false;
    
    /// <summary>
    /// Whether the timeline is advancing through recorded data
    /// </summary>
    public bool advancing = false;
    
    /// <summary>
    /// Whether the simulation is currently paused
    /// </summary>
    public bool Paused = false;
    
    /// <summary>
    /// UI text for pause button
    /// </summary>
    public TextMeshProUGUI pauseBtnTxt;
    
    /// <summary>
    /// UI text for pause status display
    /// </summary>
    public TextMeshProUGUI pauseTxt;
    
    /// <summary>
    /// UI text for position display
    /// </summary>
    public TextMeshProUGUI posText;
    
    /// <summary>
    /// Main camera for input handling
    /// </summary>
    public Camera camera;
    
    /// <summary>
    /// Current time index in the simulation
    /// </summary>
    public int TimeIndex = 0;
    
    /// <summary>
    /// Global time index for synchronization
    /// </summary>
    public int GlobalTimeIndex;
    
    /// <summary>
    /// Current position in rewind timeline
    /// </summary>
    public int RewindTimeIndex = 0;
    
    /// <summary>
    /// Maximum rewind position available
    /// </summary>
    public int maxRewindTimeIndex = 0;
    
    /// <summary>
    /// Number of recording segments completed
    /// </summary>
    public int segmentCount = -1;
    
    /// <summary>
    /// Whether currently recording a segment
    /// </summary>
    public bool isRecordingSegment = false;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        InitializeTimeline();
        camera = Camera.main;
    }

    void Update()
    {
        HandleInput();
    }

    public void FixedUpdate()
    {
        UpdateTimelineState();
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the timeline system and subscribes to object creation events
    /// </summary>
    public void InitializeTimeline()
    {
        rewindables = new List<Rewindable>();
        InstantiateScenicObject.Publish += InitializeOnScenicAdd;
        
        rewindables = FindObjectsOfType<Rewindable>().ToList();
    }

    /// <summary>
    /// Event handler for when new scenic objects are added to the scene
    /// </summary>
    /// <param name="eventArg">Event arguments containing the new object</param>
    public void InitializeOnScenicAdd(ScenicObectAddEventArg eventArg)
    {
        Rewindable rewindable = eventArg.gameObject.GetComponent<Rewindable>();
        if (rewindable != null)
        {
            rewindables.Add(rewindable);
        }
    }
    #endregion

    #region Pause Control
    /// <summary>
    /// Handles external pause notification from Scenic system
    /// </summary>
    /// <param name="pause">Whether to pause or unpause</param>
    public void NotifyPauseStatus(bool pause)
    {
        if (pause && !Paused)
        {
            Pause();
        }
        else if (!pause && Paused)
        {
            Unpause();
        }
    }

    /// <summary>
    /// Toggles pause state via UI button
    /// </summary>
    public void ClickPause()
    {
        pauseBtnTxt.text = Paused ? "Pause" : "Unpause";
        if (Paused)
        {
            Unpause();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>
    /// Pauses all rewindable objects and updates UI
    /// </summary>
    public void Pause()
    {
        foreach (Rewindable r in rewindables)
        {
            if (r.Pausible)
            {
                r.Freeze();
            }
        }
        
        RPC_PauseText("PAUSED, Press A to Unpause");
        Paused = true;
        maxRewindTimeIndex = 0;
    }

    /// <summary>
    /// Unpauses all rewindable objects and resets timeline state
    /// </summary>
    public void Unpause()
    {
        foreach (Rewindable r in rewindables)
        {
            if (r.Pausible)
            {
                r.Unfreeze();
            }
        }
        
        // Reset timeline state
        InitializeTimeline();
        TimeIndex = 0;
        RewindTimeIndex = 0;
        
        RPC_PauseText(" ");
        Paused = false;
    }

    /// <summary>
    /// Network RPC to update pause text across all clients
    /// </summary>
    /// <param name="text">Text to display</param>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PauseText(string text)
    {
        pauseTxt.text = text;
        pauseTxt.color = Color.red;
    }
    #endregion

    #region Input Handling
    /// <summary>
    /// Handles user input during paused state
    /// </summary>
    private void HandleInput()
    {
        if (Paused)
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastClick();
            }
        }
    }

    /// <summary>
    /// Handles mouse clicks for object interaction during pause
    /// </summary>
    public void RaycastClick()
    {
        RaycastHit hit;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("Coach points at ? x: " + hit.transform.position.x + ", y: " + hit.transform.position.y);
        }
    }
    #endregion

    #region Timeline State Management
    /// <summary>
    /// Updates timeline state each fixed frame (50 FPS)
    /// </summary>
    private void UpdateTimelineState()
    {
        if (Paused)
        {
            HandlePausedState();
        }
        else
        {
            AdvanceTimeline();
        }
    }

    /// <summary>
    /// Handles state updates while paused (rewind/advance functionality)
    /// </summary>
    private void HandlePausedState()
    {
        if (maxRewindTimeIndex < RewindTimeIndex)
        {
            maxRewindTimeIndex = RewindTimeIndex;
        }

        if (rewinding)
        {
            HandleRewind();
        }
        else if (advancing)
        {
            HandleAdvance();
        }
    }

    /// <summary>
    /// Advances timeline during normal play
    /// </summary>
    private void AdvanceTimeline()
    {
        TimeIndex += 1;
        GlobalTimeIndex += 1;
        RewindTimeIndex += 1;
        RecordData();
    }

    /// <summary>
    /// Handles rewind functionality
    /// </summary>
    private void HandleRewind()
    {
        if (RewindTimeIndex <= 0)
        {
            return;
        }
        
        RewindTimeIndex -= 1;
        // Apply rewind state to objects (implementation depends on time series system)
    }

    /// <summary>
    /// Handles advance through recorded data
    /// </summary>
    private void HandleAdvance()
    {
        if (RewindTimeIndex >= maxRewindTimeIndex)
        {
            return;
        }
        
        RewindTimeIndex += 1;
        // Apply recorded state to objects (implementation depends on time series system)
    }
    #endregion

    #region Data Recording
    /// <summary>
    /// Records current state of all rewindable objects (placeholder for future implementation)
    /// </summary>
    public void RecordData()
    {
        // Implementation would record transform data for rewind functionality
        foreach (Rewindable r in rewindables)
        {
            // Record transform data to time series
        }
    }
    #endregion

    #region Rewind Control
    /// <summary>
    /// Starts rewind mode for all rewindable objects
    /// </summary>
    public void Rewind()
    {
        foreach (Rewindable r in rewindables)
        {
            if (r.Pausible)
            {
                r.Unfreeze();
            }
        }
        rewinding = true;
    }
    #endregion

    #region Reset and Cleanup
    /// <summary>
    /// Resets timeline state for new simulation runs
    /// </summary>
    public void Reset()
    {
        rewindables = new List<Rewindable>();
        segmentCount = -1;
        
        JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        jsonDirectory.ResetRecordingNum();
    }
    #endregion
}