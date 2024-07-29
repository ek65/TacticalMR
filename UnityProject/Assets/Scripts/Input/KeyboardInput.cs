using System.Collections;
using System.Collections.Generic;
using OpenAI.Samples.Chat;
using TMPro;
using UnityEngine;
using Whisper.Samples;

public class KeyboardInput : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 4f;
    public ExitScenario exitScenario;
    private Rigidbody rb;
    private ChatBehaviour chatBehaviour;
    private StreamingSampleMic streamingSampleMic;

    private TimelineManager timelineManager;
    private JSONToLLM jsonToLLM;
    public TextMeshProUGUI countdownText;
    private SynthConnect synthConnect;

    public Dictionary<int, object> annotation = new Dictionary<int, object>();
    private Dictionary<int, string> annotationDescriptions = new Dictionary<int, string>();
    private Dictionary<GameObject, int> objectToKey = new Dictionary<GameObject, int>();
    public Dictionary<int, float> annotationTimes = new Dictionary<int, float>();
    private int clickOrder = 0;
    private bool isAnnotationMode = false;
    private bool isReferenceMode = false;
    private bool isPositionMode = false;
    public string explanation;

    private GameObject firstObject = null;
    private GameObject secondObject = null;

    public Vector3 movement;

    private bool canClick = true; // Add a boolean to track if clicking is allowed

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        countdownText = GameObject.FindGameObjectWithTag("countdown").GetComponent<TextMeshProUGUI>();
        chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponent<ChatBehaviour>();
        streamingSampleMic = GameObject.FindGameObjectWithTag("stream").GetComponent<StreamingSampleMic>();
        synthConnect = GameObject.FindGameObjectWithTag("connect").GetComponent<SynthConnect>();
        Debug.Log("KeyboardInput script initialized");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && gameObject.CompareTag("keyboard"))
        {
            exitScenario.EndScenario();
        }
        if (Input.GetKeyDown(KeyCode.M) && gameObject.CompareTag("keyboard"))
        {
            jsonToLLM.WriteFile();
            StartCoroutine(JSONCoroutine());
        }

        if (Input.GetKeyDown(KeyCode.P) && gameObject.CompareTag("keyboard"))
        {
            if (timelineManager.Paused)
            {
                timelineManager.Unpause();
            }
            else
            {
                timelineManager.Pause();
                StartCoroutine(Countdown());
            }
        }

        if (Input.GetKeyDown(KeyCode.J) && gameObject.CompareTag("keyboard"))
        {
            isAnnotationMode = !isAnnotationMode;
            Debug.Log("Annotation mode: " + (isAnnotationMode ? "ON" : "OFF"));
        }

        if (Input.GetKeyDown(KeyCode.H) && gameObject.CompareTag("keyboard"))
        {
            isReferenceMode = !isReferenceMode;
            firstObject = null;
            secondObject = null;
            Debug.Log("Reference mode: " + (isReferenceMode ? "ON" : "OFF"));
        }

        if (Input.GetKeyDown(KeyCode.L) && gameObject.CompareTag("keyboard"))
        {
            isPositionMode = !isPositionMode;
            Debug.Log("Position mode: " + (isPositionMode ? "ON" : "OFF"));
        }

        if (Input.GetKeyDown(KeyCode.I) && gameObject.CompareTag("keyboard"))
        {
            streamingSampleMic.OnButtonPressed();
        }

        if (canClick)
        {
            if (isAnnotationMode && Input.GetMouseButtonDown(0))
            {
                StartCoroutine(HandleClickWithDelay(HandleAnnotationMode));
            }

            if (isReferenceMode && Input.GetMouseButtonDown(0))
            {
                StartCoroutine(HandleClickWithDelay(HandleReferenceMode));
            }

            if (isPositionMode && Input.GetMouseButtonDown(0))
            {
                StartCoroutine(HandleClickWithDelay(HandlePositionMode));
            }
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            foreach (var annotation in annotation)
            {
                if (annotation.Value is GameObject gameObject)
                {
                    Debug.Log($"Key: {annotation.Key}, Object: {gameObject.name}");
                }
                else if (annotation.Value is Vector3 vector)
                {
                    Debug.Log($"Key: {annotation.Key}, Reference Vector: {vector}");
                }
            }
        }
    }

    private IEnumerator HandleClickWithDelay(System.Action handleClickAction)
    {
        canClick = false;
        handleClickAction();
        yield return new WaitForSeconds(0.5f);
        canClick = true;
    }

    private void HandleAnnotationMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;

            if (objectToKey.TryGetValue(clickedObject, out int existingKey))
            {
                streamingSampleMic.InsertAnnotationKey(existingKey); // Use existing key
                annotationTimes.Add(existingKey, jsonToLLM.time);
                Debug.Log($"Referred {clickedObject.name} with existing key {existingKey}");
            }
            else
            {
                annotation.Add(clickOrder, clickedObject);
                annotationDescriptions.Add(clickOrder, GetDescriptionAnnotation(clickedObject));
                objectToKey[clickedObject] = clickOrder; // Map object to key
                streamingSampleMic.InsertAnnotationKey(clickOrder); // Insert annotation key into transcription
                annotationTimes.Add(clickOrder, jsonToLLM.time);
                Debug.Log($"Added {clickedObject.name} to annotations with key {clickOrder}");
                clickOrder++;
            }
        }
    }

    private void HandleReferenceMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;

            if (firstObject == null)
            {
                firstObject = clickedObject;
                Debug.Log($"First object selected: {firstObject.name}");
            }
            else if (secondObject == null)
            {
                secondObject = clickedObject;
                Debug.Log($"Second object selected: {secondObject.name}");

                Vector3 referenceVector = secondObject.transform.position - firstObject.transform.position;
                annotation.Add(clickOrder, referenceVector);
                annotationTimes.Add(clickOrder, jsonToLLM.time);
                annotationDescriptions.Add(clickOrder, GetDescriptionReference(firstObject, secondObject));
                streamingSampleMic.InsertAnnotationKey(clickOrder); // Insert reference key into transcription
                Debug.Log($"Added reference vector {referenceVector} to annotations with key {clickOrder}");
                clickOrder++;

                // Reset objects for next reference
                firstObject = null;
                secondObject = null;
            }
        }
    }

    private void HandlePositionMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 clickedPosition = hit.point;
            annotation.Add(clickOrder, clickedPosition);
            annotationDescriptions.Add(clickOrder, $"(Position at {clickedPosition})");
            annotationTimes.Add(clickOrder, jsonToLLM.time);
            streamingSampleMic.InsertAnnotationKey(clickOrder); // Insert position key into transcription
            Debug.Log($"Added position {clickedPosition} to annotations with key {clickOrder}");
            clickOrder++;
        }
    }

    private string GetDescriptionAnnotation(GameObject gameObject)
    {
        return $"(Coach is pointing at {gameObject.name})";
    }
    private string GetDescriptionReference(GameObject object1, GameObject object2)
    {
        return $"(Coach is referring from {object1.name} to {object2.name})";
    }
    private string GetDescriptionPosition(Vector3 vector)
    {
        return $"(Coach is set on the position vector, {vector})";
    }

    public void OnTranscriptionFinished(string finalTranscription)
    {
        // string processedTranscription = ReplaceAnnotationKeys(finalTranscription);
        Debug.Log("Processed Transcription: " + finalTranscription);
        explanation = finalTranscription;
    }

    private string ReplaceAnnotationKeys(string transcription)
    {
        foreach (var entry in annotationDescriptions)
        {
            string key = $"[{entry.Key}]";
            string description = entry.Value;
            transcription = transcription.Replace(key, description);
        }
        return transcription;
    }
    
    public List<Dictionary<string, object>> GetAnnotationsAsJson()
    {
        List<Dictionary<string, object>> annotationsList = new List<Dictionary<string, object>>();

        foreach (var entry in annotation)
        {
            int id = entry.Key;
            object value = entry.Value;

            if (value is GameObject gameObject)
            {
                annotationsList.Add(new Dictionary<string, object>
                {
                    { "id", id.ToString() },
                    { "type", "Reference" },
                    { "obj", gameObject.name }
                });
            }
            else if (value is Vector3 vector)
            {
                annotationsList.Add(new Dictionary<string, object>
                {
                    { "id", id.ToString() },
                    { "type", "Point" },
                    { "point", new { x = vector.x, y = vector.z } },
                });
            }
        }

        return annotationsList;
    }

    IEnumerator ToggleRecordingCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(1);
        chatBehaviour.ToggleRecording(); // Start recording
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }

    IEnumerator JSONCoroutine()
    {
        yield return new WaitForSeconds(2);
        synthConnect.SendScene();
        yield return new WaitForSeconds(1);
        synthConnect.SendSceneAndExplanation();
      
    }

    IEnumerator ConditionCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(2);
        chatBehaviour.SubmitCombinedInput("condition");
        yield return new WaitForSeconds(2);
        StartCoroutine(ActionCoroutine());
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }

    IEnumerator ActionCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(2);
        chatBehaviour.SubmitCombinedInput("action");
        yield return new WaitForSeconds(2);
        StartCoroutine(FileCoroutine());
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }

    IEnumerator FileCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(1);
        jsonToLLM.WriteFile();
    }

    private IEnumerator Countdown()
    {
        countdownText.gameObject.SetActive(true);
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1);
        }

        countdownText.text = "GO";
        yield return new WaitForSeconds(1);
        countdownText.gameObject.SetActive(false);
    }

    void FixedUpdate()
    {
        if (timelineManager.Paused)
        {
            return;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 forwardDirection = transform.forward;
        movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized *
                           moveSpeed;

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }
}
