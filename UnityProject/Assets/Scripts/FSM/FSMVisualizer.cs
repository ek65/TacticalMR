using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

/// <summary>
/// Visualizes finite state machines (FSM) as interactive UI diagrams with nodes and transitions.
/// Loads FSM data from JSON resources and creates clickable node/edge representations.
/// Supports annotation of states and transitions for demonstration recording.
/// Automatically handles complex layouts using breadth-first search and virtual root injection.
/// </summary>
public class FSMVisualizer : MonoBehaviour
{
    #region Data Structures

    /// <summary>
    /// Represents a single state in the finite state machine
    /// </summary>
    [System.Serializable]
    public class State
    {
        public int id;
        public string name;
        public string description;
    }

    /// <summary>
    /// Represents a transition between two states in the finite state machine
    /// </summary>
    [System.Serializable]
    public class Transition
    {
        public int id;
        public int from;
        public int to;
        public string condition;
        public string description;
    }

    /// <summary>
    /// Root container for FSM data loaded from JSON
    /// </summary>
    [System.Serializable]
    public class FSMData
    {
        public List<State> states = new();
        public List<Transition> transitions = new();
    }

    #endregion

    #region Configuration

    [Header("UI References")]
    [Tooltip("Container for all FSM node elements")]
    public RectTransform nodesContainer;
    
    [Tooltip("Prefab for creating state node buttons")]
    public GameObject nodeButtonPrefab;
    
    [Tooltip("Text component for displaying state/transition descriptions")]
    public TMP_Text descriptionText;
    
    [Tooltip("Path to FSM JSON file in Resources folder (without .json extension)")]
    public string fsmDataPath;

    [Header("Layout Settings")]
    [Tooltip("Fixed width for all state nodes")]
    public float fixedNodeWidth = 250f;
    
    [Tooltip("Vertical padding within nodes")]
    public float paddingY = 20f;
    
    [Tooltip("Maximum height allowed for nodes")]
    public float maxHeight = 200f;
    
    [Tooltip("Horizontal spacing between columns")]
    public float spacingX = 60f;
    
    [Tooltip("Vertical spacing between nodes")]
    public float spacingY = 60f;
    
    [Tooltip("Maximum font size for node text")]
    public int maxFontSize = 36;
    
    [Tooltip("Minimum font size for node text")]
    public int minFontSize = 14;
    
    [Tooltip("Starting X position for layout")]
    public float startX = 100f;

    [Header("Visual Styling")]
    [Tooltip("Color for transition lines")]
    public Color transitionColor = Color.red;
    
    [Tooltip("Thickness of transition lines")]
    public float transitionThickness = 6f;
    
    [Tooltip("Size of arrow heads on transitions")]
    public Vector2 arrowHeadSize = new Vector2(28, 28);
    
    [Tooltip("Sprite for arrow heads")]
    public Sprite arrowSprite;
    
    [Header("Annotation System")]
    [Tooltip("Button for creating annotations")]
    public Button annotateButton;
    
    private AnnotationManager annotationManager;
    private JSONToLLM jsonToLLM;
    
    [Header("Selection Tracking")]
    private int selectedStateId = -1;
    private int selectedTransitionId = -1;
    private bool isStateSelected = false;
    
    [Header("Annotation Highlighting")]
    private HashSet<int> annotatedStates = new HashSet<int>();
    private HashSet<int> annotatedTransitions = new HashSet<int>();
    
    [Header("Debug")]
    [Tooltip("Enable verbose logging for debugging")]
    public bool logVerbose = true;

    #endregion

    #region Runtime Data

    private readonly Dictionary<int, GameObject> stateNodes = new();
    private readonly Dictionary<int, float> stateHeights = new();
    private readonly Dictionary<int, float> stateWidths  = new();

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize FSM visualization and annotation system
    /// </summary>
    void Start()
    {
        TryRun();
        SetupAnnotateButton();
    }

    /// <summary>
    /// Main initialization method with comprehensive error handling
    /// Loads JSON data, processes FSM structure, and creates visual representation
    /// </summary>
    void TryRun()
    {
        try
        {
            // Validate required components
            if (nodesContainer == null)
            {
                Debug.LogError("[FSMVisualizer] nodesContainer is not assigned.");
                return;
            }

            if (nodeButtonPrefab == null)
            {
                Debug.LogError("[FSMVisualizer] nodeButtonPrefab is not assigned.");
                return;
            }

            // Load and parse JSON data
            string json = LoadJson();
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("[FSMVisualizer] FSM JSON is empty or not found.");
                return;
            }

            FSMData fsm = null;
            try
            {
                fsm = JsonConvert.DeserializeObject<FSMData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("[FSMVisualizer] Failed to deserialize FSM JSON: " + ex.Message);
                return;
            }

            if (fsm == null)
            {
                Debug.LogError("[FSMVisualizer] Deserialized FSM is null.");
                return;
            }

            Log($"FSM loaded: {fsm.states.Count} states, {fsm.transitions.Count} transitions");

            // Process FSM structure and create visualization
            InjectVirtualRoot(fsm);
            PreMeasureNodes(fsm.states);
            CreateNodesBFS(fsm.states, fsm.transitions);
            CreateTransitions(fsm.transitions);
        }
        catch (Exception ex)
        {
            Debug.LogError("[FSMVisualizer] Unhandled exception: " + ex);
        }
    }

    /// <summary>
    /// Load FSM JSON data from Resources folder
    /// </summary>
    /// <returns>JSON string or null if file not found</returns>
    string LoadJson()
    {
        Log($"Loading FSM JSON from Resources/{fsmDataPath}.json");
        TextAsset jsonFile = Resources.Load<TextAsset>(fsmDataPath);
        if (jsonFile == null)
        {
            Debug.LogError($"[FSMVisualizer] FSM JSON file not found at Resources/{fsmDataPath}.json");
            return null;
        }

        Log("FSM JSON loaded successfully.");
        return jsonFile.text;
    }

    #endregion

    #region Text Processing

    /// <summary>
    /// Clean state names by removing lambda expressions and empty parentheses
    /// Removes content within parentheses only if they contain lambda (λ) or are empty
    /// Examples: 
    /// - "MoveTo(λ_target0(), True)" becomes "MoveTo" (contains lambda)
    /// - "Pass(teammate)" stays "Pass(teammate)" (no lambda)
    /// - "StopAndReceiveBall()" becomes "StopAndReceiveBall" (empty parentheses)
    /// </summary>
    /// <param name="name">Original state name</param>
    /// <returns>Cleaned state name for display</returns>
    string CleanStateName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        int openParen = name.IndexOf('(');
        int closeParen = name.LastIndexOf(')');
        
        if (openParen >= 0 && closeParen > openParen)
        {
            string parenthesesContent = name.Substring(openParen + 1, closeParen - openParen - 1);
            
            // Only hide parentheses content if it contains lambda (λ or unicode lambda)
            if (parenthesesContent.Contains("\u03bb") || parenthesesContent.Contains("λ"))
            {
                return name.Substring(0, openParen).Trim();
            }
            
            // If parentheses are empty, remove them
            if (string.IsNullOrWhiteSpace(parenthesesContent))
            {
                return name.Substring(0, openParen).Trim();
            }
        }
        
        // Return original name if no parentheses or no lambda found
        return name;
    }

    #endregion

    #region Graph Structure Processing

    /// <summary>
    /// Inject virtual root node to ensure graph connectivity
    /// Creates a root node (ID = 0) that connects to all orphaned states
    /// Essential for proper breadth-first traversal and layout
    /// </summary>
    /// <param name="fsm">FSM data to modify</param>
    void InjectVirtualRoot(FSMData fsm)
    {
        Log("Injecting virtual root node (ID = 0)");

        // Ensure virtual root exists
        bool rootExists = fsm.states.Exists(s => s.id == 0);
        if (!rootExists)
        {
            fsm.states.Insert(0, new State
            {
                id = 0,
                name = "Start",
                description = "Virtual root node"
            });
        }

        // Find states that have no parent (orphaned states)
        HashSet<int> hasParent = new();
        foreach (var t in fsm.transitions)
            hasParent.Add(t.to);

        // Collect orphaned states (excluding virtual root)
        List<State> orphans = new();
        foreach (var s in fsm.states)
            if (s.id != 0 && !hasParent.Contains(s.id))
                orphans.Add(s);

        // Connect virtual root to orphaned states
        if (orphans.Count != 0)
        {
            foreach (var s in orphans)
            {
                fsm.transitions.Add(new Transition
                {
                    id = -s.id, // negative ID marks as virtual
                    from = 0,
                    to = s.id,
                    condition = "virtual",
                    description = "Auto-connect (no parent)"
                });
                Log($"  0 -> {s.id} ({s.name})");
            }
        }
    }

    #endregion

    #region Node Measurement

    /// <summary>
    /// Pre-measure all nodes to determine optimal sizes for layout
    /// Creates temporary instances to measure text rendering dimensions
    /// </summary>
    /// <param name="states">List of states to measure</param>
    void PreMeasureNodes(List<State> states)
    {
        Log("Pre-measuring node sizes");

        // Create hidden measuring instance
        GameObject measurer = Instantiate(nodeButtonPrefab);
        measurer.hideFlags = HideFlags.HideAndDontSave;
        measurer.SetActive(false);

        TMP_Text label = measurer.GetComponentInChildren<TMP_Text>();
        if (label == null)
        {
            Debug.LogError("[FSMVisualizer] nodeButtonPrefab must have a TMP_Text child!");
            DestroyImmediate(measurer);
            return;
        }

        // Measure each state
        foreach (var s in states)
        {
            string displayName = CleanStateName(s.name);
            
            // Configure text component for measurement
            label.text = displayName;
            label.enableWordWrapping = true;
            label.enableAutoSizing = true;
            label.fontSizeMax = maxFontSize;
            label.fontSizeMin = minFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.overflowMode = TextOverflowModes.Overflow;
            label.ForceMeshUpdate();

            // Calculate dimensions
            float width = fixedNodeWidth;
            Vector2 preferred = label.GetPreferredValues(displayName, width, Mathf.Infinity);
            float height = Mathf.Min(preferred.y + paddingY, maxHeight);

            // Store measurements
            stateWidths[s.id] = width;
            stateHeights[s.id] = height;

            Log($"Measured state {s.id} '{displayName}' (original: '{s.name}') -> (w: {width}, h: {height})");
        }

        DestroyImmediate(measurer);
    }

    #endregion

    #region Layout Generation

    /// <summary>
    /// Create node layout using breadth-first search for hierarchical positioning
    /// Handles cycle-safe traversal and groups nodes by depth layers
    /// </summary>
    /// <param name="states">List of states to layout</param>
    /// <param name="transitions">List of transitions for adjacency</param>
    void CreateNodesBFS(List<State> states, List<Transition> transitions)
    {
        Log("Begin CreateNodesBFS()");

        // Build state lookup and adjacency lists
        Dictionary<int, State> stateLookup = new();
        foreach (var s in states)
            stateLookup[s.id] = s;

        Dictionary<int, List<int>> adj = new();
        foreach (var t in transitions)
        {
            if (!adj.ContainsKey(t.from))
                adj[t.from] = new List<int>();
            if (!adj[t.from].Contains(t.to))
                adj[t.from].Add(t.to);
        }

        // Perform BFS from virtual root to assign depths
        Dictionary<int, int> depth = new();
        Queue<int> q = new Queue<int>();

        if (!stateLookup.ContainsKey(0))
        {
            Debug.LogError("[FSMVisualizer] Virtual root (0) missing even after injection!");
            return;
        }

        depth[0] = 0;
        q.Enqueue(0);

        // BFS traversal
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            if (!adj.TryGetValue(u, out var children)) continue;

            foreach (int v in children)
            {
                if (!depth.ContainsKey(v))
                {
                    depth[v] = depth[u] + 1;
                    q.Enqueue(v);
                }
            }
        }

        Log("Node Depth Assignments:");
        foreach (var kvp in depth)
        {
            Log($"  State {kvp.Key} → Depth {kvp.Value}");
        }

        // Handle unreachable states (shouldn't happen after injection)
        foreach (var s in states)
        {
            if (!depth.ContainsKey(s.id))
            {
                Log($"State {s.id} ('{s.name}') was unreachable from root, force depth=1");
                depth[s.id] = 1;
            }
        }

        // Group states by depth and create layout
        SortedDictionary<int, List<int>> layers = new();
        foreach (var kvp in depth)
        {
            if (!layers.ContainsKey(kvp.Value))
                layers[kvp.Value] = new List<int>();
            layers[kvp.Value].Add(kvp.Key);
        }

        // Sort layers for reproducible layout
        foreach (var layer in layers.Values)
            layer.Sort();

        // Position nodes layer by layer
        foreach (var kvp in layers)
        {
            int d = kvp.Key;
            List<int> ids = kvp.Value;

            float layerWidthX = startX + (d - 3.5f) * spacingX;

            // Calculate vertical centering
            float totalHeight = 0f;
            for (int i = 0; i < ids.Count; i++)
            {
                int id = ids[i];
                float h = stateHeights.ContainsKey(id) ? stateHeights[id] : maxHeight;
                totalHeight += h;
                if (i < ids.Count - 1) totalHeight += spacingY;
            }

            float startYForLayer = totalHeight * 0.5f;
            float yCursor = startYForLayer;

            Log($"Placing layer {d} with {ids.Count} nodes. totalHeight={totalHeight}, startY={startYForLayer}");

            // Place each node in the layer
            foreach (int id in ids)
            {
                CreateNodeUI(id, stateLookup[id], layerWidthX, yCursor, d);
                yCursor -= (stateHeights[id] + spacingY);
            }
        }
    }

    /// <summary>
    /// Create individual node UI element with proper positioning and interaction
    /// </summary>
    /// <param name="id">State ID</param>
    /// <param name="state">State data</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y cursor position</param>
    /// <param name="depth">Depth in hierarchy</param>
    void CreateNodeUI(int id, State state, float x, float y, int depth)
    {
        float w = stateWidths[id];
        float h = stateHeights[id];
        string displayName = CleanStateName(state.name);

        // Create node GameObject
        GameObject node = Instantiate(nodeButtonPrefab, nodesContainer);
        node.name = $"State_{id}_{displayName}";
        RectTransform rect = node.GetComponent<RectTransform>();

        // Configure text display
        TMP_Text label = node.GetComponentInChildren<TMP_Text>();
        label.text = displayName;
        label.enableWordWrapping = true;
        label.enableAutoSizing = true;
        label.fontSizeMax = maxFontSize;
        label.fontSizeMin = minFontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Overflow;
        label.ForceMeshUpdate();

        // Position and size node
        rect.sizeDelta = new Vector2(w, h);
        float yPos = -(y - h * 0.5f);
        rect.anchoredPosition = new Vector2(x, yPos);

        // Configure click interaction
        int capturedId = id;
        string capturedDesc = state.description;
        var btn = node.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                selectedStateId = capturedId;
                selectedTransitionId = -1;
                isStateSelected = true;

                if (descriptionText != null)
                    descriptionText.text = $"[State {capturedId}] {displayName}\n{capturedDesc}";
                Log($"State {capturedId} clicked: {capturedDesc}");
            });
        }

        stateNodes[id] = node;
        Log($"  Placed state {id} '{displayName}' at ({x}, {yPos}) depth={depth}");
    }

    #endregion

    #region Transition Visualization

    /// <summary>
    /// Create visual representations of all transitions with lines and arrows
    /// Calculates proper edge attachment points and arrow positioning
    /// </summary>
    /// <param name="transitions">List of transitions to visualize</param>
    void CreateTransitions(List<Transition> transitions)
    {
        Log("Begin CreateTransitions()");

        foreach (var trans in transitions)
        {
            if (!stateNodes.ContainsKey(trans.from) || !stateNodes.ContainsKey(trans.to))
            {
                LogWarning($"Skipping transition {trans.id}: missing node(s) {trans.from}->{trans.to}");
                continue;
            }

            CreateTransitionUI(trans);
        }
    }

    /// <summary>
    /// Create UI elements for a single transition (line + arrow)
    /// </summary>
    /// <param name="trans">Transition data</param>
    void CreateTransitionUI(Transition trans)
    {
        RectTransform fromRect = stateNodes[trans.from].GetComponent<RectTransform>();
        RectTransform toRect = stateNodes[trans.to].GetComponent<RectTransform>();

        // Calculate connection points
        Vector2 from = fromRect.anchoredPosition;
        Vector2 to = toRect.anchoredPosition;
        Vector2 direction = (to - from).normalized;

        // Calculate edge offsets to attach to node boundaries
        Vector2 fromSize = fromRect.rect.size;
        Vector2 toSize = toRect.rect.size;

        float fromPadding = GetNodeEdgeOffset(direction, fromSize);
        float toPadding = GetNodeEdgeOffset(-direction, toSize);

        Vector2 start = from + direction * fromPadding;
        Vector2 end = to - direction * toPadding;

        // Create transition line
        CreateTransitionLine(trans, start, end, direction);
        
        // Create arrow head
        CreateArrowHead(trans, end, direction);

        Log($"Created transition {trans.id}: {trans.from} -> {trans.to}");
    }

    /// <summary>
    /// Create the line portion of a transition
    /// </summary>
    /// <param name="trans">Transition data</param>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="direction">Direction vector</param>
    void CreateTransitionLine(Transition trans, Vector2 start, Vector2 end, Vector2 direction)
    {
        Vector2 mid = (start + end) / 2f;
        float length = Vector2.Distance(start, end);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject lineGO = new GameObject($"UILine_{trans.id}", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = lineGO.GetComponent<RectTransform>();
        rect.SetParent(nodesContainer, false);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = mid;
        rect.sizeDelta = new Vector2(length, transitionThickness);
        rect.rotation = Quaternion.Euler(0, 0, angle);

        Image img = lineGO.GetComponent<Image>();
        img.color = transitionColor;

        // Configure click interaction
        Button btn = lineGO.GetComponent<Button>();
        string desc = $"[Transition {trans.id}] {trans.description}";
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                selectedStateId = -1;
                selectedTransitionId = trans.id;
                isStateSelected = false;

                if (descriptionText != null)
                    descriptionText.text = desc;
                Log($"Transition {trans.id} clicked: {desc}");
            });
        }
    }

    /// <summary>
    /// Create arrow head for transition
    /// </summary>
    /// <param name="trans">Transition data</param>
    /// <param name="end">End position</param>
    /// <param name="direction">Direction vector</param>
    void CreateArrowHead(Transition trans, Vector2 end, Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject arrow = new GameObject("ArrowHead", typeof(RectTransform), typeof(Image));
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.SetParent(nodesContainer, false);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.sizeDelta = arrowHeadSize;
        arrowRect.anchoredPosition = end - direction * 5f; // small inward nudge
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);

        Image arrowImg = arrow.GetComponent<Image>();
        arrowImg.sprite = arrowSprite;
        arrowImg.color = transitionColor;
        arrowImg.type = Image.Type.Simple;
        arrowImg.preserveAspect = true;
    }

    /// <summary>
    /// Calculate edge offset for proper line attachment to node boundaries
    /// Determines how far from node center to place connection point
    /// </summary>
    /// <param name="direction">Direction vector from node</param>
    /// <param name="nodeSize">Size of the node</param>
    /// <returns>Distance from center to edge</returns>
    float GetNodeEdgeOffset(Vector2 direction, Vector2 nodeSize)
    {
        direction.Normalize();
        float dx = Mathf.Abs(direction.x);
        float dy = Mathf.Abs(direction.y);

        if (dx == 0 && dy == 0)
            return 0;

        // Find minimum scale to fit direction within node bounds
        float scaleX = (nodeSize.x / 2f) / dx;
        float scaleY = (nodeSize.y / 2f) / dy;
        float scale = Mathf.Min(scaleX, scaleY);

        return scale + 6f; // +6 for visual buffer
    }

    #endregion

    #region Annotation System

    /// <summary>
    /// Initialize annotation system integration
    /// </summary>
    void SetupAnnotateButton()
    {
        annotationManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<AnnotationManager>();
        jsonToLLM = FindObjectOfType<JSONToLLM>();

        if (annotateButton != null)
        {
            annotateButton.onClick.AddListener(HandleAnnotateClick);
        }
    }

    /// <summary>
    /// Handle annotation button clicks for selected states or transitions
    /// </summary>
    void HandleAnnotateClick()
    {
        if (annotationManager == null)
        {
            Log("annotationManager not found for annotation");
            return;
        }

        if (selectedStateId == -1 && selectedTransitionId == -1)
        {
            Log("No node or transition selected for annotation");
            return;
        }

        if (isStateSelected)
        {
            // Annotate selected state
            annotationManager.CreateFsmNodeAnnotation(selectedStateId, descriptionText);
            annotatedStates.Add(selectedStateId);
            HighlightAnnotatedNode(selectedStateId);
        }
        else
        {
            // Annotate selected transition
            annotationManager.CreateFsmEdgeAnnotation(selectedTransitionId, descriptionText);
            annotatedTransitions.Add(selectedTransitionId);
            HighlightAnnotatedTransition(selectedTransitionId);
        }
    }

    /// <summary>
    /// Highlight an annotated state node with color change
    /// </summary>
    /// <param name="stateId">ID of state to highlight</param>
    void HighlightAnnotatedNode(int stateId)
    {
        if (stateNodes.ContainsKey(stateId))
        {
            GameObject node = stateNodes[stateId];
            Image nodeImage = node.GetComponent<Image>();
            if (nodeImage != null)
            {
                nodeImage.color = Color.yellow;
            }
        }
    }

    /// <summary>
    /// Highlight an annotated transition with color change
    /// </summary>
    /// <param name="transitionId">ID of transition to highlight</param>
    void HighlightAnnotatedTransition(int transitionId)
    {
        // Highlight transition line
        GameObject transitionLine = GameObject.Find($"UILine_{transitionId}");
        if (transitionLine != null)
        {
            Image lineImage = transitionLine.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = Color.yellow;
            }

            // Also highlight the arrow head
            Transform arrowHead = transitionLine.transform.parent.Find("ArrowHead");
            if (arrowHead != null)
            {
                Image arrowImage = arrowHead.GetComponent<Image>();
                if (arrowImage != null)
                {
                    arrowImage.color = Color.yellow;
                }
            }
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Log message if verbose logging is enabled
    /// </summary>
    /// <param name="msg">Message to log</param>
    void Log(string msg)
    {
        if (logVerbose)
            Debug.Log("[FSMVisualizer] " + msg);
    }

    /// <summary>
    /// Log warning message if verbose logging is enabled
    /// </summary>
    /// <param name="msg">Warning message to log</param>
    void LogWarning(string msg)
    {
        if (logVerbose)
            Debug.LogWarning("[FSMVisualizer] " + msg);
    }

    #endregion
}