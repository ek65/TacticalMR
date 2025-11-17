using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class FSMVisualizer1 : MonoBehaviour
{
    [System.Serializable]
    public class State
    {
        public int id;
        public string name;
        public string description;
    }

    [System.Serializable]
    public class Transition
    {
        public int id;
        public int from;
        public int to;
        public string condition;
        public string description;
    }

    [System.Serializable]
    public class FSMData
    {
        public List<State> states = new();
        public List<Transition> transitions = new();
    }

    [Header("UI References")]
    public RectTransform nodesContainer;
    public GameObject nodeButtonPrefab;
    public TMP_Text descriptionText;
    [Tooltip("Resources/<fsmDataPath>.json")]
    public string fsmDataPath;

    // NEW: optional second FSM
    [Header("Second FSM (optional)")]
    [Tooltip("Resources/<fsmDataPathSecond>.json (optional second FSM)")]
    public string fsmDataPathSecond;
    [Tooltip("Vertical distance between the first and second FSM (in UI units)")]
    public float secondFSMVerticalOffset = 600f;

    [Header("Layout Settings")]
    public float fixedNodeWidth = 250f;
    public float paddingY = 20f;
    public float maxHeight = 200f;
    public float spacingX = 60f;
    public float spacingY = 60f;
    public int maxFontSize = 36;
    public int minFontSize = 14;
    public float startX = 100f;

    [Header("Visuals")]
    public Color transitionColor = Color.red;
    public float transitionThickness = 6f;
    public Vector2 arrowHeadSize = new Vector2(28, 28);
    public Sprite arrowSprite;
    
    [Header("Annotation")]
    public Button annotateButton;
    private AnnotationManager annotationManager;
    private JSONToLLM jsonToLLM;
    // Track selected item for annotation
    private int selectedStateId = -1;
    private int selectedTransitionId = -1;
    private bool isStateSelected = false;
    // Track annotated items for highlighting
    private HashSet<int> annotatedStates = new HashSet<int>();
    private HashSet<int> annotatedTransitions = new HashSet<int>();
    
    [Header("Debug")]
    public bool logVerbose = true;

    // CHANGED: Use FSM-scoped dictionaries to prevent conflicts between multiple FSMs
    private readonly Dictionary<string, Dictionary<int, GameObject>> stateNodesByFSM = new();
    private readonly Dictionary<string, Dictionary<int, float>> stateHeightsByFSM = new();
    private readonly Dictionary<string, Dictionary<int, float>> stateWidthsByFSM = new();
    
    // Track the bounding box of each FSM to calculate proper spacing
    private readonly Dictionary<string, float> fsmMinY = new();
    private readonly Dictionary<string, float> fsmMaxY = new();

    void Start()
    {
        // CHANGED: run primary + optional second FSM
        TryRun();
        SetupAnnotateButton();
    }

    #region Bootstrap

    // CHANGED: now orchestrates one or two FSMs with proper vertical spacing
    void TryRun()
    {
        // Main FSM (exact same behavior as before - centers around Y=0)
        TryRunInternal(fsmDataPath, "fsm1", 0f);

        // Optional second FSM, rendered below the first one
        if (!string.IsNullOrWhiteSpace(fsmDataPathSecond))
        {
            // Calculate vertical offset based on first FSM's actual bounds
            float verticalOffset = CalculateVerticalOffsetForSecondFSM();
            TryRunInternal(fsmDataPathSecond, "fsm2", verticalOffset);
        }
    }
    
    // NEW: Calculate vertical offset to place second FSM below first FSM
    float CalculateVerticalOffsetForSecondFSM()
    {
        if (!fsmMinY.ContainsKey("fsm1") || !fsmMaxY.ContainsKey("fsm1"))
        {
            // Fallback to manual offset if bounds aren't calculated yet
            Log("First FSM bounds not yet calculated, using manual offset");
            return secondFSMVerticalOffset;
        }
        
        float firstFSMMinY = fsmMinY["fsm1"];  // Bottom of first FSM (negative)
        float firstFSMMaxY = fsmMaxY["fsm1"];  // Top of first FSM (positive)
        
        // The first FSM's bottom is at minY (negative value).
        // We want to shift the second FSM down by: abs(minY) + spacing
        // The offset will shift all nodes of second FSM down (subtract from Y)
        // Add extra spacing to push the second FSM lower
        float offset = Mathf.Abs(firstFSMMinY) + spacingY * 4f;
        
        Log($"Calculated vertical offset for second FSM: {offset} (first FSM spans from {firstFSMMinY} to {firstFSMMaxY})");
        
        return offset;
    }

    // NEW: internal worker that takes a path + FSM identifier + vertical offset
    void TryRunInternal(string path, string fsmId, float verticalOffset)
    {
        try
        {
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

            string json = LoadJson(path);
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

            Log($"FSM loaded from '{path}': {fsm.states.Count} states, {fsm.transitions.Count} transitions");

            // CHANGED: Initialize FSM-specific dictionaries
            if (!stateNodesByFSM.ContainsKey(fsmId))
            {
                stateNodesByFSM[fsmId] = new Dictionary<int, GameObject>();
                stateHeightsByFSM[fsmId] = new Dictionary<int, float>();
                stateWidthsByFSM[fsmId] = new Dictionary<int, float>();
            }

            InjectVirtualRoot(fsm);
            PreMeasureNodes(fsm.states, fsmId);

            // CHANGED: pass the FSM ID and vertical offset
            CreateNodesBFS(fsm.states, fsm.transitions, fsmId, verticalOffset);
            CreateTransitions(fsm.transitions, fsmId);
        }
        catch (Exception ex)
        {
            Debug.LogError("[FSMVisualizer] Unhandled exception: " + ex);
        }
    }

    // CHANGED: takes a path instead of using the single field
    string LoadJson(string path)
    {
        Log($"Loading FSM JSON from Resources/{path}.json");
        TextAsset jsonFile = Resources.Load<TextAsset>(path);
        if (jsonFile == null)
        {
            Debug.LogError($"[FSMVisualizer] FSM JSON file not found at Resources/{path}.json");
            return null;
        }

        Log("FSM JSON loaded successfully.");
        return jsonFile.text;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Removes content within parentheses from state names only if the parentheses contain a lambda (λ).
    /// Examples: 
    /// - "MoveTo(λ_target0(), True)" becomes "MoveTo" (contains lambda)
    /// - "Pass(teammate)" stays "Pass(teammate)" (no lambda)
    /// - "StopAndReceiveBall()" becomes "StopAndReceiveBall" (empty parentheses)
    /// </summary>
    string CleanStateName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        int openParen = name.IndexOf('(');
        int closeParen = name.LastIndexOf(')');
        
        if (openParen >= 0 && closeParen > openParen)
        {
            string parenthesesContent = name.Substring(openParen + 1, closeParen - openParen - 1);
            
            // Only hide parentheses content if it contains lambda (λ or \u03bb)
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

    #region Virtual Root Injection

    /// <summary>
    /// Always ensure there is a virtual root (ID = 0). It connects to all states that have no parent.
    /// If there are no such states (fully cyclic graph), it connects to ALL real states to guarantee reachability.
    /// </summary>
    void InjectVirtualRoot(FSMData fsm)
    {
        Log("Injecting virtual root node (ID = 0)");

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

        // Build set of states that have parents
        HashSet<int> hasParent = new();
        foreach (var t in fsm.transitions)
            hasParent.Add(t.to);

        // Collect real roots (no parent)
        List<State> orphans = new();
        foreach (var s in fsm.states)
            if (s.id != 0 && !hasParent.Contains(s.id))
                orphans.Add(s);

        if (orphans.Count != 0)
        {
            foreach (var s in orphans)
            {
                fsm.transitions.Add(new Transition
                {
                    id = -s.id, // virtual
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

    #region Pre-measure

    // CHANGED: Added fsmId parameter to scope measurements per FSM
    void PreMeasureNodes(List<State> states, string fsmId)
    {
        Log($"Pre-measuring node sizes for FSM '{fsmId}'");

        // Create a hidden measuring instance of the prefab
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

        Dictionary<int, float> stateWidths = stateWidthsByFSM[fsmId];
        Dictionary<int, float> stateHeights = stateHeightsByFSM[fsmId];

        foreach (var s in states)
        {
            // Use cleaned name for measurement
            string displayName = CleanStateName(s.name);
            label.text = displayName;
            label.enableWordWrapping = true;
            label.enableAutoSizing = true;
            label.fontSizeMax = maxFontSize;
            label.fontSizeMin = minFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.overflowMode = TextOverflowModes.Overflow;
            label.ForceMeshUpdate();

            float width = fixedNodeWidth;
            Vector2 preferred = label.GetPreferredValues(displayName, width, Mathf.Infinity);
            float height = Mathf.Min(preferred.y + paddingY, maxHeight);

            stateWidths[s.id] = width;
            stateHeights[s.id] = height;

            Log($"Measured state {s.id} '{displayName}' (original: '{s.name}') -> (w: {width}, h: {height})");
        }

        DestroyImmediate(measurer);
    }

    #endregion

    #region Layout (BFS, cycle-safe)

    // CHANGED: Added fsmId parameter and verticalOffset to scope nodes per FSM
    // This is EXACTLY the same as the original CreateNodesBFS, just with FSM-scoped dictionaries and a vertical offset
    void CreateNodesBFS(List<State> states, List<Transition> transitions, string fsmId, float verticalOffset)
    {
        Log($"Begin CreateNodesBFS() for FSM '{fsmId}'");

        // Get FSM-specific dictionaries (references to the dictionaries in stateNodesByFSM)
        Dictionary<int, GameObject> stateNodes = stateNodesByFSM[fsmId];
        Dictionary<int, float> stateHeights = stateHeightsByFSM[fsmId];
        Dictionary<int, float> stateWidths = stateWidthsByFSM[fsmId];

        // Build lookups
        Dictionary<int, State> stateLookup = new();
        foreach (var s in states)
            stateLookup[s.id] = s;

        // Adjacency (children)
        Dictionary<int, List<int>> adj = new();
        foreach (var t in transitions)
        {
            if (!adj.ContainsKey(t.from))
                adj[t.from] = new List<int>();
            if (!adj[t.from].Contains(t.to))
                adj[t.from].Add(t.to);
        }

        // BFS from 0 (virtual root)
        Dictionary<int, int> depth = new();
        Queue<int> q = new Queue<int>();

        if (!stateLookup.ContainsKey(0))
        {
            Debug.LogError("[FSMVisualizer] Virtual root (0) missing even after injection!");
            return;
        }

        depth[0] = 0;
        q.Enqueue(0);

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

        // Any state not reached by BFS (shouldn't happen after injection, but just in case),
        // attach it to depth 1.
        foreach (var s in states)
        {
            if (!depth.ContainsKey(s.id))
            {
                Log($"State {s.id} ('{s.name}') was unreachable from root, force depth=1");
                depth[s.id] = 1;
            }
        }

        // Group by depth
        SortedDictionary<int, List<int>> layers = new();
        foreach (var kvp in depth)
        {
            if (!layers.ContainsKey(kvp.Value))
                layers[kvp.Value] = new List<int>();
            layers[kvp.Value].Add(kvp.Key);
        }

        // For reproducibility order layers by id (except root first)
        foreach (var layer in layers.Values)
            layer.Sort();

        // Track bounds for this FSM
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        // Place nodes per layer
        foreach (var kvp in layers)
        {
            int d = kvp.Key;
            List<int> ids = kvp.Value;

            float layerWidthX = startX + (d-3.5f) * spacingX;

            // Compute total height for this layer to center vertically
            float totalHeight = 0f;
            for (int i = 0; i < ids.Count; i++)
            {
                int id = ids[i];
                float h = stateHeights.ContainsKey(id) ? stateHeights[id] : maxHeight;
                totalHeight += h;
                if (i < ids.Count - 1) totalHeight += spacingY;
            }

            // center around 0
            float startYForLayer = totalHeight * 0.5f;
            float yCursor = startYForLayer;

            Log($"Placing layer {d} with {ids.Count} nodes. totalHeight={totalHeight}, startY={startYForLayer}");

            foreach (int id in ids)
            {
                State s = stateLookup[id];
                float w = stateWidths[id];
                float h = stateHeights[id];

                // Create node GO
                GameObject node = Instantiate(nodeButtonPrefab, nodesContainer);
                node.name = $"{fsmId}_State_{id}_{CleanStateName(s.name)}";
                RectTransform rect = node.GetComponent<RectTransform>();

                // TMP - Use cleaned name for display
                TMP_Text label = node.GetComponentInChildren<TMP_Text>();
                string displayName = CleanStateName(s.name);
                label.text = displayName;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.enableAutoSizing = true;
                label.fontSizeMax = maxFontSize;
                label.fontSizeMin = minFontSize;
                label.alignment = TextAlignmentOptions.Center;
                label.overflowMode = TextOverflowModes.Overflow;
                label.ForceMeshUpdate();

                rect.sizeDelta = new Vector2(w, h);

                float yPos = - (yCursor - h * 0.5f);
                
                // CHANGED: Subtract vertical offset (0 for first FSM, calculated offset for second FSM)
                float finalY = yPos - verticalOffset;
                rect.anchoredPosition = new Vector2(layerWidthX, finalY);

                // Track bounds (NEW: for calculating second FSM position)
                float nodeTop = finalY + h * 0.5f;
                float nodeBottom = finalY - h * 0.5f;
                if (nodeTop > maxY) maxY = nodeTop;
                if (nodeBottom < minY) minY = nodeBottom;

                Log($"  Placed state {id} '{displayName}' (original: '{s.name}') at ({layerWidthX}, {finalY}) depth={d}");

                int capturedId = id;
                string capturedDesc = s.description;
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
                yCursor -= (h + spacingY);
            }
        }

        // Store bounds for this FSM (NEW: for calculating second FSM position)
        fsmMinY[fsmId] = minY;
        fsmMaxY[fsmId] = maxY;
        Log($"FSM '{fsmId}' bounds: Y from {minY} to {maxY}");
    }

    #endregion

    #region Transitions

    // CHANGED: Added fsmId parameter to scope transitions per FSM
    void CreateTransitions(List<Transition> transitions, string fsmId)
    {
        Log($"Begin CreateTransitions() for FSM '{fsmId}'");

        Dictionary<int, GameObject> stateNodes = stateNodesByFSM[fsmId];

        foreach (var trans in transitions)
        {
            if (!stateNodes.ContainsKey(trans.from) || !stateNodes.ContainsKey(trans.to))
            {
                LogWarning($"Skipping transition {trans.id}: missing node(s) {trans.from}->{trans.to}");
                continue;
            }

            RectTransform fromRect = stateNodes[trans.from].GetComponent<RectTransform>();
            RectTransform toRect = stateNodes[trans.to].GetComponent<RectTransform>();

            Vector2 from = fromRect.anchoredPosition;
            Vector2 to = toRect.anchoredPosition;
            Vector2 direction = (to - from).normalized;

            // Get true half extents in the direction of the transition
            Vector2 fromSize = fromRect.rect.size;
            Vector2 toSize = toRect.rect.size;

            float fromPadding = GetNodeEdgeOffset(direction, fromSize);
            float toPadding = GetNodeEdgeOffset(-direction, toSize); // opposite dir

            Vector2 start = from + direction * fromPadding;
            Vector2 end = to - direction * toPadding;

            Vector2 mid = (start + end) / 2f;
            float length = Vector2.Distance(start, end);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject lineGO = new GameObject($"{fsmId}_UILine_{trans.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = lineGO.GetComponent<RectTransform>();
            rect.SetParent(nodesContainer, false);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = mid;
            rect.sizeDelta = new Vector2(length, transitionThickness);
            rect.rotation = Quaternion.Euler(0, 0, angle);

            Image img = lineGO.GetComponent<Image>();
            img.color = transitionColor;

            Button btn = lineGO.GetComponent<Button>();
            // Note: We only show the description, not the condition, for cleaner visualization
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

            // Arrowhead
            GameObject arrow = new GameObject($"{fsmId}_ArrowHead_{trans.id}", typeof(RectTransform), typeof(Image));
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

            Log($"Created transition {trans.id}: {trans.from} -> {trans.to} (condition hidden from visualization)");
        }
    }

    /// <summary>
    /// Calculates how far from a node's center to offset the transition line
    /// so it visually attaches to the edge of the node.
    /// </summary>
    float GetNodeEdgeOffset(Vector2 direction, Vector2 nodeSize)
    {
        direction.Normalize();
        float dx = Mathf.Abs(direction.x);
        float dy = Mathf.Abs(direction.y);

        if (dx == 0 && dy == 0)
            return 0;

        // Find the minimum scale factor so the direction vector fits within the box
        float scaleX = (nodeSize.x / 2f) / dx;
        float scaleY = (nodeSize.y / 2f) / dy;
        float scale = Mathf.Min(scaleX, scaleY);

        return scale + 6f; // +6 for buffer
    }

    #endregion
    
    #region Annotations
    
    void SetupAnnotateButton()
    {
        annotationManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<AnnotationManager>();
        jsonToLLM = FindObjectOfType<JSONToLLM>();

        if (annotateButton != null)
        {
            annotateButton.onClick.AddListener(HandleAnnotateClick);
        }
    }
    
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
    
    void HighlightAnnotatedNode(int stateId)
    {
        // Check both FSMs for the state
        foreach (var kvp in stateNodesByFSM)
        {
            if (kvp.Value.ContainsKey(stateId))
            {
                GameObject node = kvp.Value[stateId];
                Image nodeImage = node.GetComponent<Image>();
                if (nodeImage != null)
                {
                    nodeImage.color = Color.yellow;
                }
                return;
            }
        }
    }

    void HighlightAnnotatedTransition(int transitionId)
    {
        // Find transition line by name (check both FSMs)
        GameObject transitionLine = GameObject.Find($"fsm1_UILine_{transitionId}");
        if (transitionLine == null)
        {
            transitionLine = GameObject.Find($"fsm2_UILine_{transitionId}");
        }
        
        if (transitionLine != null)
        {
            Image lineImage = transitionLine.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = Color.yellow;
            }
        
            // Also highlight the arrowhead if it exists (check both FSMs)
            string fsmId = transitionLine.name.StartsWith("fsm1_") ? "fsm1" : "fsm2";
            GameObject arrowHead = GameObject.Find($"{fsmId}_ArrowHead_{transitionId}");
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

    #region Utils

    void Log(string msg)
    {
        if (logVerbose)
            Debug.Log("[FSMVisualizer] " + msg);
    }

    void LogWarning(string msg)
    {
        if (logVerbose)
            Debug.LogWarning("[FSMVisualizer] " + msg);
    }

    #endregion
}