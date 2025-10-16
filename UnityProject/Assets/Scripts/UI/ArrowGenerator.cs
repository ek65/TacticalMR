using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Procedurally generates arrow mesh geometry for 3D visualization and direction indication.
/// Creates arrows with customizable dimensions (stem and tip) pointing from origin to target positions.
/// Supports networked visibility control and automatic rotation to face target objects.
/// Designed for annotation and guidance systems requiring directional visual indicators.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ArrowGenerator : NetworkBehaviour
{
    [Header("Arrow Geometry Configuration")]
    [Tooltip("Length of the arrow stem (calculated automatically based on distance)")]
    public float stemLength;
    
    [Tooltip("Width of the arrow stem")]
    public float stemWidth;
    
    [Tooltip("Length of the arrow tip/head")]
    public float tipLength;
    
    [Tooltip("Width of the arrow tip/head")]
    public float tipWidth;

    [Header("Positioning")]
    [Tooltip("Starting position of the arrow")]
    public Vector3 origin;
    
    [Tooltip("Target position the arrow points toward")]
    public Vector3 target;

    [Header("Mesh Data")]
    [System.NonSerialized]
    public List<Vector3> verticesList;
    
    [System.NonSerialized]
    public List<int> trianglesList;

    [Header("Components")]
    private Mesh mesh;

    #region Network Control

    /// <summary>
    /// Network RPC to control arrow visibility across all clients
    /// Enables synchronized show/hide functionality for multiplayer scenarios
    /// </summary>
    /// <param name="active">Whether the arrow should be visible</param>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetActiveState(bool active) 
    {
        this.transform.parent.gameObject.SetActive(active);
    }

    #endregion

    #region Position Configuration

    /// <summary>
    /// Set the arrow's starting position
    /// </summary>
    /// <param name="origin">The origin point for the arrow</param>
    public void SetOrigin(Vector3 origin)
    {
        this.origin = origin;
    }

    /// <summary>
    /// Set the arrow's target position
    /// </summary>
    /// <param name="target">The target point the arrow should point toward</param>
    public void SetTarget(Vector3 target)
    {
        this.target = target;
    }

    /// <summary>
    /// Calculate appropriate stem length based on distance between origin and destination
    /// Automatically adjusts length to prevent tip clipping and maintain proper spacing
    /// </summary>
    /// <param name="origin">Starting position</param>
    /// <param name="destination">Target position</param>
    private void CalculateStemLength(Vector3 origin, Vector3 destination)
    {
        stemLength = Vector3.Distance(origin, destination);
        // Subtract 2 meters: 1 to prevent tip clipping + 1 for proper spacing
        stemLength = stemLength - 2;
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initialize mesh components and validate material assignment
    /// </summary>
    void Start()
    {
        // Ensure MeshRenderer has a material assigned
        mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = mesh;
    }

    /// <summary>
    /// Update arrow geometry and orientation based on target position
    /// Only executes on server to prevent duplicate mesh generation
    /// </summary>
    void Update()
    {
        // Only generate mesh on server to avoid duplicate work
        if (Runner.IsClient)
        {
            return;
        }

        // Generate the arrow mesh
        GenerateArrow();
        
        // Skip positioning if no target is set
        if (target == Vector3.zero)
        {
            return;
        }
        
        // Position arrow at origin
        transform.parent.position = origin;
        
        // Rotate parent to face target (arrow faces right by default)
        transform.parent.right = target - transform.parent.position;
        
        // Calculate appropriate stem length for current distance
        CalculateStemLength(origin, target);
    }

    #endregion

    #region Mesh Generation

    /// <summary>
    /// Generate arrow mesh geometry procedurally
    /// Creates arrow starting at Vector3.zero facing right (positive X direction)
    /// Constructs separate stem and tip sections with proper triangulation
    /// </summary>
    void GenerateArrow()
    {
        // Initialize mesh data collections
        verticesList = new List<Vector3>();
        trianglesList = new List<int>();

        // Generate stem geometry
        GenerateStem();
        
        // Generate tip geometry
        GenerateTip();

        // Apply mesh data to the MeshFilter component
        mesh.vertices = verticesList.ToArray();
        mesh.triangles = trianglesList.ToArray();
    }

    /// <summary>
    /// Generate the rectangular stem portion of the arrow
    /// Creates a simple quad extending rightward from the origin
    /// </summary>
    private void GenerateStem()
    {
        Vector3 stemOrigin = Vector3.zero;
        float stemHalfWidth = stemWidth / 2f;

        // Create stem vertices (rectangular cross-section)
        verticesList.Add(stemOrigin + (stemHalfWidth * Vector3.down));  // Bottom-left
        verticesList.Add(stemOrigin + (stemHalfWidth * Vector3.up));    // Top-left
        verticesList.Add(verticesList[0] + (stemLength * Vector3.right)); // Bottom-right
        verticesList.Add(verticesList[1] + (stemLength * Vector3.right)); // Top-right

        // Create stem triangles (two triangles forming a quad)
        // First triangle: bottom-left, top-left, top-right
        trianglesList.Add(0);
        trianglesList.Add(1);
        trianglesList.Add(3);

        // Second triangle: bottom-left, top-right, bottom-right
        trianglesList.Add(0);
        trianglesList.Add(3);
        trianglesList.Add(2);
    }

    /// <summary>
    /// Generate the triangular tip portion of the arrow
    /// Creates an arrowhead at the end of the stem pointing rightward
    /// </summary>
    private void GenerateTip()
    {
        Vector3 tipOrigin = stemLength * Vector3.right;
        float tipHalfWidth = tipWidth / 2;

        // Create tip vertices (triangular arrowhead)
        verticesList.Add(tipOrigin + (tipHalfWidth * Vector3.up));    // Top corner
        verticesList.Add(tipOrigin + (tipHalfWidth * Vector3.down));  // Bottom corner
        verticesList.Add(tipOrigin + (tipLength * Vector3.right));    // Point

        // Create tip triangle
        trianglesList.Add(4); // Top corner
        trianglesList.Add(6); // Point
        trianglesList.Add(5); // Bottom corner
    }

    #endregion
}