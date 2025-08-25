using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class FSMVisualizer : MonoBehaviour
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
    private KeyboardInput keyboardInput;
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

    private readonly Dictionary<int, GameObject> stateNodes = new();
    private readonly Dictionary<int, float> stateHeights = new();   // pre-measured
    private readonly Dictionary<int, float> stateWidths  = new();   // pre-measured (all fixedNodeWidth but stored anyway)

    void Start()
    {
        TryRun();
        SetupAnnotateButton();
    }

    #region Bootstrap

    void TryRun()
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

        // if (orphans.Count == 0)
        // {
        //     // Fully cyclic (or every node has a parent). Connect virtual root to all real nodes.
        //     Log("No orphan states found. Graph seems cyclic. Connecting virtual root to ALL states.");
        //     foreach (var s in fsm.states)
        //     {
        //         if (s.id == 0) continue;
        //         fsm.transitions.Add(new Transition
        //         {
        //             id = -s.id, // mark as virtual
        //             from = 0,
        //             to = s.id,
        //             condition = "virtual",
        //             description = "Auto-connect (cyclic graph)"
        //         });
        //         Log($"  0 -> {s.id} ({s.name})");
        //     }
        // }
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

    void PreMeasureNodes(List<State> states)
    {
        Log("Pre-measuring node sizes");

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

    void CreateNodesBFS(List<State> states, List<Transition> transitions)
    {
        Log("Begin CreateNodesBFS()");

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
                node.name = $"State_{id}_{CleanStateName(s.name)}";
                RectTransform rect = node.GetComponent<RectTransform>();

                // TMP - Use cleaned name for display
                TMP_Text label = node.GetComponentInChildren<TMP_Text>();
                string displayName = CleanStateName(s.name);
                label.text = displayName;
                label.enableWordWrapping = true;
                label.enableAutoSizing = true;
                label.fontSizeMax = maxFontSize;
                label.fontSizeMin = minFontSize;
                label.alignment = TextAlignmentOptions.Center;
                label.overflowMode = TextOverflowModes.Overflow;
                label.ForceMeshUpdate();

                rect.sizeDelta = new Vector2(w, h);

                float yPos = - (yCursor - h * 0.5f);
                rect.anchoredPosition = new Vector2(layerWidthX, yPos);

                Log($"  Placed state {id} '{displayName}' (original: '{s.name}') at ({layerWidthX}, {yPos}) depth={d}");

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
    }

    #endregion

    #region Transitions

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

            GameObject lineGO = new GameObject($"UILine_{trans.id}", typeof(RectTransform), typeof(Image), typeof(Button));
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
        keyboardInput = FindObjectOfType<KeyboardInput>();
        if (keyboardInput == null)
        {
            Debug.LogError("[FSMVisualizer] KeyboardInput component not found!");
        }
        
        jsonToLLM = FindObjectOfType<JSONToLLM>();
    
        if (annotateButton != null)
        {
            annotateButton.onClick.AddListener(HandleAnnotateClick);
        }
    }
    
    void HandleAnnotateClick()
    {
        if (keyboardInput == null)
        {
            Log("KeyboardInput not found for annotation");
            return;
        }
    
        if (selectedStateId == -1 && selectedTransitionId == -1)
        {
            Log("No node or transition selected for annotation");
            return;
        }

        if (isStateSelected)
        {
            // Node annotation
            Dictionary<string, object> nodeAnnotation = new Dictionary<string, object>
            {
                { "type", "node annotation" },
                { "stateId", selectedStateId },
                { "description", descriptionText.text }
            };
        
            keyboardInput.annotation.Add(keyboardInput.clickOrder, nodeAnnotation);
            keyboardInput.annotationDescriptions.Add(keyboardInput.clickOrder, $"Node annotation: State {selectedStateId}");
            
            // Highlight the annotated node
            annotatedStates.Add(selectedStateId);
            HighlightAnnotatedNode(selectedStateId);
        
            Log($"Added node annotation for state {selectedStateId}, key {keyboardInput.clickOrder}");
        }
        else
        {
            // Edge annotation  
            Dictionary<string, object> edgeAnnotation = new Dictionary<string, object>
            {
                { "type", "edge annotation" },
                { "transitionId", selectedTransitionId },
                { "description", descriptionText.text }
            };
        
            keyboardInput.annotation.Add(keyboardInput.clickOrder, edgeAnnotation);
            keyboardInput.annotationDescriptions.Add(keyboardInput.clickOrder, $"Edge annotation: Transition {selectedTransitionId}");
            
            // Highlight the annotated transition
            annotatedTransitions.Add(selectedTransitionId);
            HighlightAnnotatedTransition(selectedTransitionId);
        
            Log($"Added edge annotation for transition {selectedTransitionId}, key {keyboardInput.clickOrder}");
        }
        
        keyboardInput.annotationTimes.Add(keyboardInput.clickOrder, Time.time - keyboardInput.segmentStartTime);
    
        keyboardInput.clickOrder++;
    }
    
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

    void HighlightAnnotatedTransition(int transitionId)
    {
        // Find transition line by name
        GameObject transitionLine = GameObject.Find($"UILine_{transitionId}");
        if (transitionLine != null)
        {
            Image lineImage = transitionLine.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = Color.yellow;
            }
        
            // Also highlight the arrowhead if it exists
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