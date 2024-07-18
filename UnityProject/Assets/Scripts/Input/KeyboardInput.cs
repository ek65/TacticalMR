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

    private Dictionary<int, object> annotations = new Dictionary<int, object>();
    private Dictionary<int, string> annotationDescriptions = new Dictionary<int, string>();
    private Dictionary<GameObject, int> objectToKey = new Dictionary<GameObject, int>();
    private int clickOrder = 0;
    private bool isAnnotationMode = false;
    private bool isReferenceMode = false;
    private bool isPositionMode = false;

    private GameObject firstObject = null;
    private GameObject secondObject = null;

    public Vector3 movement;

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
        if (Input.GetKeyDown(KeyCode.E))
        {
            exitScenario.EndScenario();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (timelineManager.Paused)
            {
                timelineManager.Unpause();
            }
            else
            {
                timelineManager.Pause();
                StartCoroutine(Countdown());
                chatBehaviour.ToggleRecording(); // Stop recording
                Debug.Log("Chat behaviour is:" + chatBehaviour.isRecording);
                if (!chatBehaviour.isRecording)
                {
                    StartCoroutine(ToggleRecordingCoroutine());
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            isAnnotationMode = !isAnnotationMode;
            Debug.Log("Annotation mode: " + (isAnnotationMode ? "ON" : "OFF"));
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            isReferenceMode = !isReferenceMode;
            firstObject = null;
            secondObject = null;
            Debug.Log("Reference mode: " + (isReferenceMode ? "ON" : "OFF"));
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            isPositionMode = !isPositionMode;
            Debug.Log("Position mode: " + (isPositionMode ? "ON" : "OFF"));
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            streamingSampleMic.OnButtonPressed();
        }

        if (isAnnotationMode && Input.GetMouseButtonDown(0))
        {
            HandleAnnotationMode();
        }

        if (isReferenceMode && Input.GetMouseButtonDown(0))
        {
            HandleReferenceMode();
        }

        if (isPositionMode && Input.GetMouseButtonDown(0))
        {
            HandlePositionMode();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            foreach (var annotation in annotations)
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

    private void HandleAnnotationMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;

            if (objectToKey.TryGetValue(clickedObject, out int existingKey))
            {
                streamingSampleMic.InsertAnnotationKey(existingKey); // Use existing key
                Debug.Log($"Referred {clickedObject.name} with existing key {existingKey}");
            }
            else
            {
                annotations.Add(clickOrder, clickedObject);
                annotationDescriptions.Add(clickOrder, GetDescriptionAnnotation(clickedObject));
                objectToKey[clickedObject] = clickOrder; // Map object to key
                streamingSampleMic.InsertAnnotationKey(clickOrder); // Insert annotation key into transcription
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
                annotations.Add(clickOrder, referenceVector);
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
            annotations.Add(clickOrder, clickedPosition);
            annotationDescriptions.Add(clickOrder, $"(Position at {clickedPosition})");
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
        string processedTranscription = ReplaceAnnotationKeys(finalTranscription);
        Debug.Log("Processed Transcription: " + processedTranscription);
        synthConnect.SendExplanation(processedTranscription);
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

    IEnumerator ToggleRecordingCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(1);
        chatBehaviour.ToggleRecording(); // Start recording
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }

    IEnumerator JSONCoroutine()
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        yield return new WaitForSeconds(1);
        chatBehaviour.SubmitCombinedInput("explain");
        yield return new WaitForSeconds(2);
        StartCoroutine(ConditionCoroutine());
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
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
        for (int i = 5; i > 0; i--)
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

        // THIS IS WHERE HUMAN MOVEMENT HAPPENS WITH KEYBOARD
        // TODO: MAKE THIS BETTER WHERE WE CAN MOVE ROTATION AROUND WITH MOUSE OR ARROW KEYS AND HAVE ACCEL/DECCEL 
        // CURRENTLY ONLY HAS SET SPEED DEFINED IN moveSpeed AND NO ACCELERATION
        
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 forwardDirection = transform.forward;
        movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized *
                           moveSpeed;

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }
}
