using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Central memory system that records and replays scenario creation.
/// Tracks all LLM responses with timestamps and manages scenario recording/playback.
/// Serves as the "memory" of what objects exist and what behaviors have been assigned.
/// </summary>
public class ScenarioMemory : MonoBehaviour
{
    [Header("Recording State")]
    [Tooltip("Whether we are currently recording the scenario")]
    public bool isRecording = false;
    
    [Tooltip("Whether we are currently in playback mode")]
    public bool isPlayback = false;

    [Header("Timeline Tracking")]
    [Tooltip("Current elapsed time during recording (excludes paused time)")]
    public float currentRecordingTime = 0f;
    
    [Tooltip("Time when playback started")]
    private float playbackStartTime = 0f;
    
    [Header("Recorded Events")]
    [Tooltip("All recorded scenario events with timestamps")]
    public List<ScenarioEvent> recordedEvents = new List<ScenarioEvent>();
    
    [Tooltip("Index of next event to execute during playback")]
    private int playbackIndex = 0;

    [Header("Current Scene State")]
    [Tooltip("All objects currently in the scene (name -> SceneObject)")]
    public Dictionary<string, SceneObject> sceneObjects = new Dictionary<string, SceneObject>(StringComparer.OrdinalIgnoreCase);

    [Header("System References")]
    private TimelineManager timelineManager;
    private ScenarioPlanExecutor executor;
    private GameManager gameManager;

    #region Initialization

    void Start()
    {
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        executor = GetComponent<ScenarioPlanExecutor>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        if (executor == null)
        {
            Debug.LogError("ScenarioPlanExecutor not found on same GameObject as ScenarioMemory!");
        }

        Debug.Log("ScenarioMemory initialized");
    }

    #endregion

    #region Recording Control

    void Update()
    {
        // Update recording time (only when not paused)
        if (isRecording && !timelineManager.Paused)
        {
            currentRecordingTime += Time.deltaTime;
        }

        // Handle spacebar to stop recording and start playback
        if (Input.GetKeyDown(KeyCode.Space) && isRecording)
        {
            StopRecordingAndStartPlayback();
        }

        // Handle playback
        if (isPlayback)
        {
            UpdatePlayback();
        }
    }

    /// <summary>
    /// Start recording scenario events.
    /// Called after the first LLM response/spawn.
    /// </summary>
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("Already recording!");
            return;
        }

        isRecording = true;
        currentRecordingTime = 0f;
        recordedEvents.Clear();
        
        Debug.Log("Started recording scenario");
    }

    /// <summary>
    /// Stop recording and begin playback from the start.
    /// Resets the scene and replays all recorded events.
    /// </summary>
    public void StopRecordingAndStartPlayback()
    {
        if (!isRecording)
        {
            Debug.LogWarning("Not currently recording!");
            return;
        }

        isRecording = false;
        
        Debug.Log($"Stopped recording. Total events: {recordedEvents.Count}");
        
        // Start playback
        StartPlayback();
    }

    #endregion

    #region Event Recording

    /// <summary>
    /// Record a new scenario plan from the LLM response.
    /// Stores the plan with the current timestamp and updates scene state.
    /// </summary>
    /// <param name="plan">The scenario plan returned by the LLM</param>
    public void RecordScenarioPlan(ScenarioPlan plan)
    {
        if (plan == null || plan.Objects == null || plan.Objects.Count == 0)
        {
            Debug.LogWarning("Attempted to record null or empty scenario plan");
            return;
        }

        // If this is the first event, start recording automatically
        if (!isRecording && recordedEvents.Count == 0)
        {
            StartRecording();
        }

        if (!isRecording)
        {
            Debug.LogWarning("Not recording, but received scenario plan. Call StartRecording() first.");
            return;
        }

        // Create event with current timestamp
        ScenarioEvent scenarioEvent = new ScenarioEvent
        {
            timestamp = currentRecordingTime,
            plan = plan,
            eventDescription = GenerateEventDescription(plan)
        };

        recordedEvents.Add(scenarioEvent);
        
        // Update our internal scene state
        UpdateSceneState(plan);

        Debug.Log($"Recorded event at t={currentRecordingTime:F2}s: {scenarioEvent.eventDescription}");
    }

    /// <summary>
    /// Update the internal scene state based on a scenario plan.
    /// Tracks what objects exist and their current behaviors/attributes.
    /// </summary>
    /// <param name="plan">The plan to integrate into scene state</param>
    private void UpdateSceneState(ScenarioPlan plan)
    {
        foreach (var obj in plan.Objects)
        {
            if (string.IsNullOrWhiteSpace(obj.Name))
            {
                Debug.LogWarning("SceneObject missing required Name field!");
                continue;
            }

            // Update or add object to scene state
            if (sceneObjects.ContainsKey(obj.Name))
            {
                // Object already exists - merge/update behaviors and attributes
                var existing = sceneObjects[obj.Name];
                
                // Add new behaviors (avoiding duplicates by name)
                if (obj.Behaviors != null)
                {
                    foreach (var behavior in obj.Behaviors)
                    {
                        var existingBehavior = existing.Behaviors.FirstOrDefault(b => 
                            string.Equals(b.Name, behavior.Name, StringComparison.OrdinalIgnoreCase));
                        
                        if (existingBehavior != null)
                        {
                            // Update existing behavior parameters
                            existingBehavior.Parameters = behavior.Parameters;
                        }
                        else
                        {
                            // Add new behavior
                            existing.Behaviors.Add(behavior);
                        }
                    }
                }

                // Update attributes
                if (obj.Attributes != null)
                {
                    foreach (var attr in obj.Attributes)
                    {
                        var existingAttr = existing.Attributes.FirstOrDefault(a => 
                            string.Equals(a.Key, attr.Key, StringComparison.OrdinalIgnoreCase));
                        
                        if (existingAttr != null)
                        {
                            existingAttr.Value = attr.Value;
                        }
                        else
                        {
                            existing.Attributes.Add(attr);
                        }
                    }
                }

                // Update actions
                if (obj.Actions != null && obj.Actions.Count > 0)
                {
                    existing.Actions.AddRange(obj.Actions);
                }
            }
            else
            {
                // New object - add to scene state
                sceneObjects[obj.Name] = obj;
            }
        }
    }

    /// <summary>
    /// Generate a human-readable description of what happened in this event.
    /// </summary>
    private string GenerateEventDescription(ScenarioPlan plan)
    {
        if (plan.Objects.Count == 1)
        {
            var obj = plan.Objects[0];
            if (!string.IsNullOrWhiteSpace(obj.Prefab))
            {
                return $"Spawned {obj.Name} ({obj.Prefab})";
            }
            else if (obj.Behaviors != null && obj.Behaviors.Count > 0)
            {
                return $"Added behaviors to {obj.Name}";
            }
            else if (obj.Actions != null && obj.Actions.Count > 0)
            {
                return $"Executed actions on {obj.Name}";
            }
        }
        
        return $"Updated {plan.Objects.Count} object(s)";
    }

    #endregion

    #region Playback

    /// <summary>
    /// Start playback of recorded scenario from the beginning.
    /// Clears the scene and prepares to replay all events.
    /// </summary>
    private void StartPlayback()
    {
        if (recordedEvents.Count == 0)
        {
            Debug.LogWarning("No events to playback!");
            return;
        }

        isPlayback = true;
        playbackIndex = 0;
        playbackStartTime = Time.time;
        
        // Clear the scene
        ClearScene();
        
        Debug.Log($"Starting playback of {recordedEvents.Count} events");
    }

    /// <summary>
    /// Update playback state each frame.
    /// Executes recorded events when their timestamp is reached.
    /// </summary>
    private void UpdatePlayback()
    {
        if (playbackIndex >= recordedEvents.Count)
        {
            // Playback complete
            isPlayback = false;
            Debug.Log("Playback complete");
            return;
        }

        float currentPlaybackTime = Time.time - playbackStartTime;

        // Execute all events whose timestamp has been reached
        while (playbackIndex < recordedEvents.Count)
        {
            var evt = recordedEvents[playbackIndex];
            
            if (currentPlaybackTime >= evt.timestamp)
            {
                Debug.Log($"Executing event {playbackIndex + 1}/{recordedEvents.Count} at t={evt.timestamp:F2}s: {evt.eventDescription}");
                
                // Execute the scenario plan
                executor.Apply(evt.plan);
                
                playbackIndex++;
            }
            else
            {
                // Haven't reached this event's timestamp yet
                break;
            }
        }
    }

    /// <summary>
    /// Stop playback.
    /// </summary>
    public void StopPlayback()
    {
        isPlayback = false;
        playbackIndex = 0;
        Debug.Log("Stopped playback");
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// Clear all spawned objects from the scene.
    /// Prepares the scene for playback.
    /// </summary>
    private void ClearScene()
    {
        // Find and destroy all spawned objects
        // This assumes spawned objects are tagged or parented in a specific way
        
        // Clear goals only if it exists
        var goals = GameObject.FindGameObjectsWithTag("goal");
        foreach (var goal in goals)
        {
            Destroy(goal);
        }

        // Clear balls
        var balls = GameObject.FindGameObjectsWithTag("ball");
        foreach (var ball in balls)
        {
            Destroy(ball);
        }

        // Clear players/opponents/teammates
        var players = GameObject.FindGameObjectsWithTag("player");
        players = players.Concat(GameObject.FindGameObjectsWithTag("human")).ToArray();
        foreach (var player in players)
        {
            // // Don't destroy the human player
            // if (player.CompareTag("human")) continue;
            Destroy(player);
        }

        Debug.Log("Scene cleared for playback");
    }

    /// <summary>
    /// Get all objects currently tracked in the scene.
    /// </summary>
    public List<SceneObject> GetAllSceneObjects()
    {
        return sceneObjects.Values.ToList();
    }

    /// <summary>
    /// Get a specific object by name.
    /// </summary>
    public SceneObject GetSceneObject(string name)
    {
        sceneObjects.TryGetValue(name, out var obj);
        return obj;
    }

    /// <summary>
    /// Check if an object with the given name exists.
    /// </summary>
    public bool HasObject(string name)
    {
        return sceneObjects.ContainsKey(name);
    }

    #endregion

    #region Debug and Utilities

    /// <summary>
    /// Print current recording state and all tracked objects.
    /// </summary>
    [ContextMenu("Debug: Print Scene State")]
    public void DebugPrintSceneState()
    {
        Debug.Log($"=== Scenario Memory State ===");
        Debug.Log($"Recording: {isRecording} | Playback: {isPlayback}");
        Debug.Log($"Recording Time: {currentRecordingTime:F2}s");
        Debug.Log($"Recorded Events: {recordedEvents.Count}");
        Debug.Log($"Tracked Objects: {sceneObjects.Count}");
        
        foreach (var kvp in sceneObjects)
        {
            var obj = kvp.Value;
            Debug.Log($"  - {obj.Name} ({obj.Prefab}): {obj.Behaviors.Count} behaviors, {obj.Attributes.Count} attributes");
        }
    }

    /// <summary>
    /// Print all recorded events with timestamps.
    /// </summary>
    [ContextMenu("Debug: Print Recorded Events")]
    public void DebugPrintRecordedEvents()
    {
        Debug.Log($"=== Recorded Events ({recordedEvents.Count}) ===");
        for (int i = 0; i < recordedEvents.Count; i++)
        {
            var evt = recordedEvents[i];
            Debug.Log($"{i + 1}. t={evt.timestamp:F2}s - {evt.eventDescription}");
        }
    }

    #endregion
}

/// <summary>
/// Represents a single recorded event in the scenario timeline.
/// Contains the LLM response (ScenarioPlan) and when it occurred.
/// </summary>
[Serializable]
public class ScenarioEvent
{
    [Tooltip("Time when this event occurred during recording")]
    public float timestamp;
    
    [Tooltip("The scenario plan from the LLM")]
    public ScenarioPlan plan;
    
    [Tooltip("Human-readable description of this event")]
    public string eventDescription;
}