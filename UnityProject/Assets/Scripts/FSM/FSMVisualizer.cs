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
        public List<State> states;
        public List<Transition> transitions;
    }

    [Header("UI References")]
    public RectTransform nodesContainer;
    public GameObject nodeButtonPrefab;
    public LineRenderer linePrefab;
    public TMP_Text descriptionText;
    public string fsmDataPath;

    [Header("Layout Settings")]
    public float radius = 300f;

    private Dictionary<int, GameObject> stateNodes = new();

    void Start()
    {
        Debug.Log("FSMVisualizer started.");
        string json = LoadJson();
        FSMData fsm = JsonConvert.DeserializeObject<FSMData>(json);

        Debug.Log($"FSM loaded: {fsm.states.Count} states, {fsm.transitions.Count} transitions");

        CreateNodes(fsm.states);
        CreateTransitions(fsm.transitions);
    }

    string LoadJson()
    {
        Debug.Log($"Loading FSM JSON from Resources/{fsmDataPath}.json");
        TextAsset jsonFile = Resources.Load<TextAsset>(fsmDataPath);
        if (jsonFile == null)
        {
            Debug.LogError($"FSM JSON file not found at Resources/{fsmDataPath}.json");
            return "{}";
        }

        Debug.Log("FSM JSON loaded successfully.");
        return jsonFile.text;
    }

    [SerializeField] private float fixedNodeWidth = 250f;
[SerializeField] private float paddingY = 20f;
[SerializeField] private float maxHeight = 200f;
[SerializeField] private float spacingX = 300f;
[SerializeField] private int maxFontSize = 36;
[SerializeField] private int minFontSize = 14;

void CreateNodes(List<State> states)
{
    int count = states.Count;
    float angleStep = 360f / count;

    for (int i = 0; i < count; i++)
    {
        var state = states[i];
        float angle = i * angleStep * Mathf.Deg2Rad;
        Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        GameObject node = Instantiate(nodeButtonPrefab, nodesContainer);
        RectTransform rect = node.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;

        TMP_Text label = node.GetComponentInChildren<TMP_Text>();
        label.text = state.name;

        // 💡 Setup text for wrapping + auto-sizing
        label.enableWordWrapping = true;
        label.enableAutoSizing = true;
        label.fontSizeMax = maxFontSize;
        label.fontSizeMin = minFontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Overflow;

        // 🔧 Force TMP to calculate text layout properly
        label.ForceMeshUpdate();

        // 📏 Set fixed width, dynamic height based on wrapped text
        float width = fixedNodeWidth;
        Vector2 preferred = label.GetPreferredValues(state.name, width, Mathf.Infinity);
        float height = Mathf.Min(preferred.y + paddingY, maxHeight);

        rect.sizeDelta = new Vector2(width, height);

        // 📌 Button click action
        int capturedId = state.id;
        string capturedDesc = state.description;

        node.GetComponent<Button>().onClick.AddListener(() =>
        {
            descriptionText.text = $"[State {capturedId}] {state.name}\n{capturedDesc}";
            Debug.Log($"State {capturedId} clicked: {capturedDesc}");
        });

        stateNodes[state.id] = node;
    }
}

[SerializeField] private float nodeOffset = 50f; // Distance from node center

[SerializeField] private Sprite arrowSprite; // Assign a triangle sprite in the Inspector

    void CreateTransitions(List<Transition> transitions)
    {
        foreach (var trans in transitions)
        {
            if (!stateNodes.ContainsKey(trans.from) || !stateNodes.ContainsKey(trans.to))
            {
                Debug.LogWarning($"Skipping transition {trans.id}: missing node(s) {trans.from}->{trans.to}");
                continue;
            }

            // Get node positions and rect sizes
            RectTransform fromRect = stateNodes[trans.from].GetComponent<RectTransform>();
            RectTransform toRect = stateNodes[trans.to].GetComponent<RectTransform>();

            Vector2 from = fromRect.anchoredPosition;
            Vector2 to = toRect.anchoredPosition;
            Vector2 direction = (to - from).normalized;

            // Dynamically calculate offset from node size
            float fromOffset = Mathf.Min(fromRect.rect.width, fromRect.rect.height) / 2 + 50f;
            float toOffset = Mathf.Min(toRect.rect.width, toRect.rect.height) / 2 + 50f;

            Vector2 start = from + direction * fromOffset;
            Vector2 end = to - direction * toOffset;

            Vector2 mid = (start + end) / 2f;
            float length = Vector2.Distance(start, end);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // --- LINE ---
            GameObject lineGO = new GameObject($"UILine_{trans.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = lineGO.GetComponent<RectTransform>();
            rect.SetParent(nodesContainer, false);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = mid;
            rect.sizeDelta = new Vector2(length, 10f); // Thickness
            rect.rotation = Quaternion.Euler(0, 0, angle);

            Image img = lineGO.GetComponent<Image>();
            img.color = Color.red;

            Button btn = lineGO.GetComponent<Button>();
            string desc = $"[Transition {trans.id}] {trans.description}\nCondition: {trans.condition}";
            btn.onClick.AddListener(() =>
            {
                descriptionText.text = desc;
                Debug.Log($"Transition {trans.id} clicked: {desc}");
            });

            // --- ARROWHEAD ---
            GameObject arrow = new GameObject("ArrowHead", typeof(RectTransform), typeof(Image));
            RectTransform arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.SetParent(nodesContainer, false);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(40, 40); // Customize for style
            arrowRect.anchoredPosition = end - direction * 5f; // Slight back to not go past node
            arrowRect.rotation = Quaternion.Euler(0, 0, angle);

            Image arrowImg = arrow.GetComponent<Image>();
            arrowImg.sprite = arrowSprite;
            arrowImg.color = Color.red;
            arrowImg.type = Image.Type.Simple;
            arrowImg.preserveAspect = true;
        }
    }
}