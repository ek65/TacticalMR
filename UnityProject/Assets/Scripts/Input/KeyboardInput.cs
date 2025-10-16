using UnityEngine;
using Vector3 = UnityEngine.Vector3;

// TODO: Switch to new input system to match the controller/VR input handling. Right now we are using the old input system for keyboard input.

/// <summary>
/// Handles keyboard input for player movement and game control actions.
/// Manages WASD movement for laptop mode and provides key bindings for core game functions.
/// Integrates with annotation and interaction systems to handle user input for demonstrations.
/// Serves as the primary input manager for desktop/laptop gameplay modes.
/// </summary>
public class KeyboardInput : MonoBehaviour
{
    [Header("Movement Configuration")]
    [SerializeField] 
    [Tooltip("Movement speed multiplier for WASD controls")]
    public float moveSpeed = 4f;
    
    [Header("Physics and Transform")]
    [Tooltip("Rigidbody component for physics-based movement")]
    public Rigidbody rb;
    
    [Tooltip("Current movement vector calculated from input")]
    public Vector3 movement;
    
    [Header("System References")]
    public TimelineManager timelineManager;
    private GameManager gameManager;
    private ProgramSynthesisManager programSynthesisManager;

    #region Initialization

    /// <summary>
    /// Initialize component references and system connections
    /// </summary>
    void Start()
    {
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();

        Debug.Log("KeyboardInput script initialized");
    }

    #endregion

    #region Input Processing

    /// <summary>
    /// Process keyboard input for game control actions
    /// Handles key bindings for restart, segment control, and pause functionality
    /// </summary>
    void Update()
    {
        // Only process input for keyboard-tagged objects
        if (!gameObject.CompareTag("keyboard")) return;

        // Press E to restart scenario
        if (Input.GetKeyDown(KeyCode.E))
        {
            programSynthesisManager.HandleRestart();
        }

        // Press B to toggle segment recording
        if (Input.GetKeyDown(KeyCode.B))
        {
            programSynthesisManager.HandleSegment();
        }

        // Press P to pause/unpause timeline
        if (Input.GetKeyDown(KeyCode.P))
        {
            programSynthesisManager.HandlePause();
        }
    }

    #endregion

    #region Interaction Handlers

    /// <summary>
    /// Handle position-based interaction clicks
    /// Triggers position annotation mode with proper click delay management
    /// </summary>
    public void HandlePositionClick()
    {
        StartCoroutine(programSynthesisManager.HandleClickWithDelay(programSynthesisManager.HandlePositionMode));
    }

    /// <summary>
    /// Handle object annotation interaction clicks
    /// Triggers annotation mode with proper click delay management
    /// </summary>
    public void HandleAnnotationClick()
    {
        StartCoroutine(programSynthesisManager.HandleClickWithDelay(programSynthesisManager.HandleAnnotationMode));
    }

    #endregion

    #region Movement Processing

    /// <summary>
    /// Process WASD movement input and apply physics-based locomotion
    /// Handles movement only in laptop mode and respects timeline pause state
    /// Uses transform-relative movement for intuitive directional control
    /// </summary>
    void FixedUpdate()
    {
        // Initialize rigidbody reference if needed
        if (rb == null)
        {
            GameObject coachObject = GameObject.FindGameObjectWithTag("human");
            if (coachObject == null) return;
            
            rb = coachObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log("Found human rigidbody");
            }
        }
        
        // Validate rigidbody and timeline state
        if (rb == null) return;
        if (timelineManager.Paused) return;

        // Process input only in laptop mode
        float horizontalInput = 0f;
        float verticalInput = 0f;
        
        if (gameManager.laptopMode)
        {
            horizontalInput = Input.GetAxis("Horizontal"); // A/D or Arrow Keys
            verticalInput = Input.GetAxis("Vertical");     // W/S or Arrow Keys
        }
        
        // Calculate movement relative to current transform orientation
        Vector3 forwardDirection = transform.forward;
        movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized * moveSpeed;
        
        // Apply movement using physics
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    #endregion
}