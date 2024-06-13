using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
using OpenAI.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utilities.Async;
using Utilities.Audio;
using Utilities.Encoding.Wav;
using Utilities.Extensions;
using Utilities.WebRequestRest;

namespace OpenAI.Samples.Chat
{
    public class ChatBehaviour : MonoBehaviour
{
    public CancellationToken destroyCancellationToken;
    [SerializeField] private OpenAIConfiguration configuration;

    [SerializeField] private bool enableDebug;

    [SerializeField] private Button submitButton;

    [SerializeField] private TMP_InputField inputField;

    [SerializeField] private RectTransform contentArea;

    [SerializeField] private ScrollRect scrollView;

    [SerializeField] private AudioSource audioSource;

    [SerializeField] [TextArea(3, 10)] public string systemPrompt =
        "I am a soccer coach giving explanations of the strategy I am using against my opponents. Based on the given soccer strategy explanation provided by the coach who is on the field and the provided positioning data of the field, output a JSON object with three fields: 'motion', 'rationale', and 'condition'. The 'motion' field should describe the action to be taken, 'rationale' explains why this action is necessary within the strategy, and 'condition' specifies the circumstances under which the action is to be initiated."; 
    private List<string> transcriptions = new List<string>();
    private OpenAIClient openAI;
    public string conditionOutput;
    public string actionOutput;
    public string jsonResponseText; 

    private readonly Conversation conversation = new();

    public string[] sentences;
    public AudioClip[] clips;
    public int sentenceIndex;
    public string responseText;
    public string userInput;
    public string jsonText; // json text to be appended to userInput
    public string combinedInput; // userinput + jsonText to send to LLM
    public bool isRecording = false;

    public string synthesizeInput =
        "\"You're a helpful coding assistant.\\n" +
        "Your task is to write a python function that return a boolean true if the condition to perform the motion is satisfied, and false otherwise. \\n" +
        "The function should take no arguments.\\nDo so by using the language description of the condition and rationale that you just generated based on my explanation. Make sure to use primarily the information from the \\\"condition\\\" field\\nNote that the objects in the scene are already defined and their properties are readily accessible (e.g. opponent.position.x). \\nWrite the expressions(s) within the function including properties of objects relevant to the action (e.g player.position.x).\\n\\nThe objects are defined as follows:\\n\\nclass Object:\\n\\tdef __init__(self, position):\\n\\t\\tself.name: string\\n\\t\\tself.position: Vector # Vector describing position of object (x, y, z)\\nself.rotation: int # Direction at which the player is facing (roll, pitch, yaw)\\n\\nclass Player(Object):\\n \\tdef __init__(self, name, position, rotation, behavior=None):\\nsuper.__init__(name, position)\\nself.behavior: Behavior # Behavior that is assigned to the player (e.g. Intercept Ball, Shoot Ball, etc.)\\n\\ndef hasBehavior(self, behavior): \\n\\\"\\\"Checks if a player's behavior is the action we want\\\"\\n\\ndef distanceBetween(self, target): \\n\\\"\\\"Return the distance between current player with another object\\\"\\\"\\n\\ndef hasBallPosession(self):\\n“”Returns whether or not the current player instance has possession of the ball””\\n\\ndef movingTowards(self, target):\\n“”Returns whether or not the current instance of the player is heading towards the target object.””\\n\\ndef lookingAt(self, target):\\n“”Returns whether or not the current instance of the player is facing towards the target object.””\\n\\nThe scene contains the initialized classes [goal, football, defendant, attacker]\\n\\nYou may use the following already implemented class methods:\\n\\ndef closest(objectType: ObjectType, referenceObj: UnityObject) -> UnityObject\\u2028\\t“””\\n\\tGets the closest object of specified type to a reference object\\n\\t\\n\\tArgs:\\n\\t\\tobjectType (ObjectType): the type of the target objects who’s distance to the reference object will be compared to\\n\\t\\treferenceObj (UnityObject): the reference object to which the closest object of the specified type will be returned\\n\\n\\tReturns:\\n\\t\\tUnityObject: the object of specified type that is closest to the specified reference object\\n\\t“””\\n\\ndef likelyTo(object: Player, futureAction: str) -> Probability:\\n\\t“””\\n\\tReturns the probability of a specified object to take a future specified action \\n\\t\\n\\tArgs:\\n\\t\\tobject (Player): the player performing an action and who’s future action probability is being examined\\n\\t\\tfutureAction (str): the future action to be checked to get a probability of the specified object taking it\\n\\n\\tReturns:\\n\\t\\tProbability: a probability represented as an enum with possible cases [.low, .mid, .high]\\n\\t“””\\n\\ndef lookingAt(object: UnityObject) -> bool\\u2028\\t“””\\u2028\\tDetermines if the player is looking at the specified object.\\n\\n\\tArgs:\\n   \\tobject (UnityObject): The object that is being checked if it is being looked at.\\n\\n\\tReturns:\\n   \\t bool: True if the player is looking at the specified object, False otherwise.\\n\\t“””\\n\\ndef movingTowards(object: UnityObject) -> bool\\u2028\\t“””\\u2028\\tChecks if the player is moving towards the specified object.\\n\\n\\tArgs:\\n    \\t\\tobject (UnityObject): The object that is being checked if the player is moving towards it.\\n\\n\\tReturns:\\n    \\t\\tbool: True if the player is moving towards the specified object, False otherwise.\\u2028\\t“””\\n\\ndef hasBallPossession(object: Player) -> bool\\u2028\\t“””\\u2028\\tDetermines if the specified player has possession of the ball.\\n\\n\\tArgs:\\n    \\t\\tobject (Player): The player whose ball possession status is being checked.\\n\\n\\tReturns:\\n   \\t\\tbool: True if the player has possession of the ball, False otherwise.\\n\\t“””\\n\\ndef isPerformingAction(action: str) -> bool\\n\\t“””\\n\\tChecks if the player is currently performing the specified action.\\n\\n\\tArgs:\\n    \\t\\taction (str): The action that is being checked if it is currently being performed by the player.\\n\\n\\tReturns:\\n    \\t\\tbool: True if the player is performing the specified action, False otherwise.\\n\\t“””\\n\\nReturn ONLY the condition function.\\n\\nAssume the following behaviors for the Player class have been correctly implemented.\\n\\ndef Idle(self):\\n\\t“””\\n\\tplayer will stand still\\n\\t“””\\n\\ndef ShootBall(point: Vector):\\n\\t“””\\n\\tplayer will shoot the ball to the point defined as a vector\\n\\t“””\\n\\ndef InterceptBall(ball):\\n\\t“””\\n\\tplayer will approach the ball and attempt to intercept it\\n\\t“””\\n\\ndef GroundPassFast(point: Vector):\\n\\t“””\\n\\tplayer will pass the ball to the point defined as a vector\\n\\t“””\\n\\ndef MoveTo(point: Vector):\\n\\t“””\\n\\tplayer will move to the point defined as a vector\\n\\t“””\\n\\ndef MoveTowards(object: UnityObject)\\n\\t“””\\n\\tplayer will move towards the object defined as another UnityObject, like the goal, the ball or other players\\n\\t“””\\n\\nYou are allowed to check whether a player is performing a specific behavior by using the hasBehavior() helper function. For instance, to check if the attacker is approaching the goal for example, one should check if attacker.hasBehavior(MoveTo) returns true.\\n\\nIf you feel like there would be a helpful API function that is missing from the list given to you, feel free to come up with your own. If you do so, write a list of your assumed functions and variables with a description of the arguments and return value.\\n\\nIf some of the inputs to a function are not well defined, make sure to mark them with \\\"!\\\" in code.\\n";

    private void OnValidate()
    {
        inputField.Validate();
        contentArea.Validate();
        submitButton.Validate();
        audioSource.Validate();
    }

    void Update()
    {

    }

    private void Awake()
    {
        OnValidate();
        openAI = new OpenAIClient(configuration)
        {
            EnableDebug = enableDebug
        };
        conversation.AppendMessage(new Message(Role.System, systemPrompt));
        inputField.onSubmit.AddListener(SubmitChat);
        submitButton.onClick.AddListener(SubmitChat);

        // Start continuous recording when the script initializes
        
    }

    private void SubmitChat(string _) => SubmitChat();

    private static bool isChatPending;

    public void SetInputTextAndSubmit(string text)
    {
        userInput = text;
        inputField.text = text;
        SubmitChat();
    }

    public async void SubmitChat()
    {
        if (isChatPending || string.IsNullOrWhiteSpace(inputField.text))
        {
            return;
        }

        isChatPending = true;

        inputField.interactable = false;
        submitButton.interactable = false;
        conversation.AppendMessage(new Message(Role.User, inputField.text));
        inputField.text = string.Empty;
        var assistantMessageContent = AddNewTextMessageContent(Role.Assistant);
        assistantMessageContent.text = "Expert: ";

        try
        {
            var request = new ChatRequest(conversation.Messages);
            var response = await openAI.ChatEndpoint.StreamCompletionAsync(request, resultHandler: deltaResponse =>
            {
                if (deltaResponse?.FirstChoice?.Delta == null)
                {
                    return;
                }

                assistantMessageContent.text += deltaResponse.FirstChoice.Delta.ToString();
                scrollView.verticalNormalizedPosition = 0f;
            }, destroyCancellationToken);

            conversation.AppendMessage(response.FirstChoice.Message);

            if (response.FirstChoice.FinishReason == "tool_calls")
            {
                response = await ProcessToolCallsAsync(response);
                assistantMessageContent.text += response.ToString().Replace("![Image](output.jpg)", string.Empty);
            }

            responseText = response;
            GenerateSpeech(response);
        }
        catch (Exception e)
        {
            switch (e)
            {
                case TaskCanceledException:
                case OperationCanceledException:
                    break;
                default:
                    Debug.LogError(e);
                    break;
            }
        }
        finally
        {
            if (destroyCancellationToken is { IsCancellationRequested: false })
            {
                inputField.interactable = true;
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                submitButton.interactable = true;
            }

            isChatPending = false;
        }

        async Task<ChatResponse> ProcessToolCallsAsync(ChatResponse response)
        {
            var toolCalls = new List<Task>();

            foreach (var toolCall in response.FirstChoice.Message.ToolCalls)
            {
                if (enableDebug)
                {
                    Debug.Log(
                        $"{response.FirstChoice.Message.Role}: {toolCall.Function.Name} | Finish Reason: {response.FirstChoice.FinishReason}");
                    Debug.Log($"{toolCall.Function.Arguments}");
                }

                toolCalls.Add(ProcessToolCall());

                async Task ProcessToolCall()
                {
                    await Awaiters.UnityMainThread;

                    try
                    {
                        // var imageResults = await toolCall.InvokeFunctionAsync<IReadOnlyList<ImageResult>>().ConfigureAwait(true);
                        //
                        // foreach (var imageResult in imageResults)
                        // {
                        //     AddNewImageContent(imageResult);
                        // }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        conversation.AppendMessage(new(toolCall, $"{{\"result\":\"{e.Message}\"}}"));
                        return;
                    }

                    conversation.AppendMessage(new(toolCall, "{\"result\":\"completed\"}"));
                }
            }

            await Task.WhenAll(toolCalls).ConfigureAwait(true);
            ChatResponse toolCallResponse;

            try
            {
                var toolCallRequest = new ChatRequest(conversation.Messages);
                toolCallResponse = await openAI.ChatEndpoint.GetCompletionAsync(toolCallRequest);
                conversation.AppendMessage(toolCallResponse.FirstChoice.Message);
            }
            catch (RestException restEx)
            {
                Debug.LogError(restEx);

                foreach (var toolCall in response.FirstChoice.Message.ToolCalls)
                {
                    conversation.AppendMessage(new Message(toolCall, restEx.Response.Body));
                }

                var toolCallRequest = new ChatRequest(conversation.Messages);
                toolCallResponse = await openAI.ChatEndpoint.GetCompletionAsync(toolCallRequest);
                conversation.AppendMessage(toolCallResponse.FirstChoice.Message);
            }

            if (toolCallResponse.FirstChoice.FinishReason == "tool_calls")
            {
                return await ProcessToolCallsAsync(toolCallResponse);
            }

            return toolCallResponse;
        }
    }

public async void SubmitChatNoSpeech(string field)
    {
        if (isChatPending || string.IsNullOrWhiteSpace(inputField.text))
        {
            return;
        }

        isChatPending = true;

        inputField.interactable = false;
        submitButton.interactable = false;

        // Determine the system prompt based on the field
        if (field == "explain")
        {
            systemPrompt = "I am a soccer coach giving explanations of the strategy I am using against my opponents. Based on the given soccer strategy explanation provided by the coach who is on the field and the provided positioning data of the field, output a JSON object with three fields: 'motion', 'rationale', and 'condition'. The 'motion' field should describe the action to be taken, 'rationale' explains why this action is necessary within the strategy, and 'condition' specifies the circumstances under which the action is to be initiated."; 

        }
        if (field == "condition")
        {
            systemPrompt = "You're a helpful coding assistant.\nYour task is to write a python function that return a boolean true if the condition to perform the motion is satisfied, and false otherwise. \nThe function should take no arguments.\nDo so by using the language description of the condition and rationale for the action given. Make sure to use primarily the information from the \"condition\" field\nNote that the objects in the scene are already defined and their properties are readily accessible (e.g. opponent.position.x). \nWrite the expressions(s) within the function including properties of objects relevant to the action (e.g player.position.x).\n\nThe objects are defined as follows:\n\nclass Object:\n\tdef __init__(self, position):\n\t\tself.name: string\n\t\tself.position: Vector # Vector describing position of object (x, y, z)\nself.rotation: int # Direction at which the player is facing (roll, pitch, yaw)\n\nclass Player(Object):\n \tdef __init__(self, name, position, rotation, behavior=None):\nsuper.__init__(name, position)\nself.behavior: Behavior # Behavior that is assigned to the player (e.g. Intercept Ball, Shoot Ball, etc.)\n\ndef hasBehavior(self, behavior): \n\"\"Checks if a player's behavior is the action we want\"\n\ndef distanceBetween(self, target): \n\"\"Return the distance between current player with another object\"\"\n\ndef hasBallPosession(self):\n“”Returns whether or not the current player instance has possession of the ball””\n\ndef movingTowards(self, target):\n“”Returns whether or not the current instance of the player is heading towards the target object.””\n\ndef lookingAt(self, target):\n“”Returns whether or not the current instance of the player is facing towards the target object.””\n\nThe scene contains the initialized classes [goal, football, defendant, attacker]\n\nYou may use the following already implemented class methods:\n\ndef closest(objectType: ObjectType, referenceObj: UnityObject) -> UnityObject\u2028\t“””\n\tGets the closest object of specified type to a reference object\n\t\n\tArgs:\n\t\tobjectType (ObjectType): the type of the target objects who’s distance to the reference object will be compared to\n\t\treferenceObj (UnityObject): the reference object to which the closest object of the specified type will be returned\n\n\tReturns:\n\t\tUnityObject: the object of specified type that is closest to the specified reference object\n\t“””\n\ndef likelyTo(object: Player, futureAction: str) -> Probability:\n\t“””\n\tReturns the probability of a specified object to take a future specified action \n\t\n\tArgs:\n\t\tobject (Player): the player performing an action and who’s future action probability is being examined\n\t\tfutureAction (str): the future action to be checked to get a probability of the specified object taking it\n\n\tReturns:\n\t\tProbability: a probability represented as an enum with possible cases [.low, .mid, .high]\n\t“””\n\ndef lookingAt(object: UnityObject) -> bool\u2028\t“””\u2028\tDetermines if the player is looking at the specified object.\n\n\tArgs:\n   \tobject (UnityObject): The object that is being checked if it is being looked at.\n\n\tReturns:\n   \t bool: True if the player is looking at the specified object, False otherwise.\n\t“””\n\ndef movingTowards(object: UnityObject) -> bool\u2028\t“””\u2028\tChecks if the player is moving towards the specified object.\n\n\tArgs:\n    \t\tobject (UnityObject): The object that is being checked if the player is moving towards it.\n\n\tReturns:\n    \t\tbool: True if the player is moving towards the specified object, False otherwise.\u2028\t“””\n\ndef hasBallPossession(object: Player) -> bool\u2028\t“””\u2028\tDetermines if the specified player has possession of the ball.\n\n\tArgs:\n    \t\tobject (Player): The player whose ball possession status is being checked.\n\n\tReturns:\n   \t\tbool: True if the player has possession of the ball, False otherwise.\n\t“””\n\ndef isPerformingAction(action: str) -> bool\n\t“””\n\tChecks if the player is currently performing the specified action.\n\n\tArgs:\n    \t\taction (str): The action that is being checked if it is currently being performed by the player.\n\n\tReturns:\n    \t\tbool: True if the player is performing the specified action, False otherwise.\n\t“””\n\nReturn ONLY the condition function.\n\nAssume the following behaviors for the Player class have been correctly implemented.\n\ndef Idle(self):\n\t“””\n\tplayer will stand still\n\t“””\n\ndef ShootBall(point: Vector):\n\t“””\n\tplayer will shoot the ball to the point defined as a vector\n\t“””\n\ndef InterceptBall(ball):\n\t“””\n\tplayer will approach the ball and attempt to intercept it\n\t“””\n\ndef GroundPassFast(point: Vector):\n\t“””\n\tplayer will pass the ball to the point defined as a vector\n\t“””\n\ndef MoveTo(point: Vector):\n\t“””\n\tplayer will move to the point defined as a vector\n\t“””\n\ndef MoveTowards(object: UnityObject)\n\t“””\n\tplayer will move towards the object defined as another UnityObject, like the goal, the ball or other players\n\t“””\n\nYou are allowed to check whether a player is performing a specific behavior by using the hasBehavior() helper function. For instance, to check if the attacker is approaching the goal for example, one should check if attacker.hasBehavior(MoveTo) returns true.\n\nIf you feel like there would be a helpful API function that is missing from the list given to you, feel free to come up with your own. If you do so, write a list of your assumed functions and variables with a description of the arguments and return value.\n\nIf some of the inputs to a function are not well defined, make sure to mark them with \"!\" in code.\n";
        }
        else if (field == "action")
        {
            systemPrompt = "You're a helpful coding assistant.\nYour task is to write a python function that call a number of different API functions in a specific order and configuration to meet the description of the action.\nThe function should take no arguments. Note that the objects in the scene are already initialized and their properties are readily accessible, for instance, you may reference the position of a player object named opponent by writing \"opponent.position.x\".\n\nWrite the expressions(s) within the function you write including properties of objects relevant to the action like position, team or possession.\n\nThe objects are defined as follows:\n\nclass Object:\n\tname: string\n\tposition: Vector\n\trotation: Degrees # 0 points north\n\nclass Player(Object):\n \tteam: string\u2028\tbehavior: string\u2028\tpossession: bool\n\u2028You may use the following already implemented API functions:\u2028\nclosest(objType: ObjectType, refObj: Object) -> Object\n\t# Returns the closest object of specified type objType to a reference object refObj\n\ndef likelyTo(object: Player, futureAction: string) -> Probability:\n\tReturns the probability of a specified object to take a future specified action. The probability is represented by an enum with possible cases [.low, .mid, .high]. \n\ndef lookingAt(player: Player, targetObj: Object) -> bool\u2028\tDetermines if the player is looking at the specified object targetObj, returns true if so and false otherwise.\n\ndef movingTowards(refObj: Object, targetObj: Object) -> bool\u2028\tChecks if the reference object is moving towards the specified object targetObj.\n\ndef hasBallPossession(object: Player) -> bool\u2028\tDetermines if the specified player has possession of the ball.\n\ndef isPerformingAction(action: str) -> bool\n\tChecks if the player is currently performing the specified action.\n\ndef positionInLine(start: Object, end: Object, distance: Double) -> Vector\n\treturn the position as a vector in a line starting at the position of an object start and finishing at the position of an object end at a specified distance\n\nReturn ONLY the action function.\n\nYou should include in your function the necessary number of the following smaller action functions to compose the larger action that meets the specified description.\n\ndef Idle(self):\n\t“””\n\tplayer will stand still\n\t“””\n\ndef ShootBall(point: Vector):\n\t“””\n\tplayer will shoot the ball to the point defined as a vector\n\t“””\n\ndef InterceptBall(ball):\n\t“””\n\tplayer will approach the ball and attempt to intercept it\n\t“””\n\ndef GroundPassFast(point: Vector):\n\t“””\n\tplayer will pass the ball to the point defined as a vector\n\t“””\n\ndef MoveTo(point: Vector):\n\t“””\n\tplayer will move to the point defined as a vector\n\t“””\n\ndef MoveTowards(object: UnityObject)\n\t“””\n\tplayer will move towards the object defined as another UnityObject, like the goal, the ball or other players\n\t“””\n\nYou are allowed to check whether a player is performing a specific behavior by using the hasBehavior() helper function. For instance, to check if the attacker is approaching the goal for example, one should check if attacker.hasBehavior(MoveTo) returns true.\n\nIf you feel like there would be a helpful API function that is missing from the list given to you, feel free to come up with your own. If you do so, write a list of your assumed functions and variables with a description of the arguments and return value.\n\nIf some of the inputs to a function are not well defined, make sure to mark them with \"!\" in code.\n";
        }

        // Create a new conversation with the updated system prompt
        var newConversation = new Conversation();
        newConversation.AppendMessage(new Message(Role.System, systemPrompt));
        newConversation.AppendMessage(new Message(Role.User, inputField.text));

        inputField.text = string.Empty;
        var assistantMessageContent = AddNewTextMessageContent(Role.Assistant);
        assistantMessageContent.text = " ";

        try
        {
            var request = new ChatRequest(newConversation.Messages);
            var response = await openAI.ChatEndpoint.StreamCompletionAsync(request, resultHandler: deltaResponse =>
            {
                if (deltaResponse?.FirstChoice?.Delta == null)
                {
                    return;
                }

                assistantMessageContent.text += deltaResponse.FirstChoice.Delta.ToString();
                scrollView.verticalNormalizedPosition = 0f;
            }, destroyCancellationToken);

            newConversation.AppendMessage(response.FirstChoice.Message);

            responseText = response;

            // Store the output based on the field
            if (field == "explain")
            {
                jsonResponseText = responseText;
            }
            if (field == "condition")
            {
                conditionOutput = responseText;
            }
            else if (field == "action")
            {
                actionOutput = responseText;
            }

            Debug.Log($"Generated Python program: {responseText}");
        }
        catch (Exception e)
        {
            switch (e)
            {
                case TaskCanceledException:
                case OperationCanceledException:
                    break;
                default:
                    Debug.LogError(e);
                    break;
            }
        }
        finally
        {
            if (destroyCancellationToken is { IsCancellationRequested: false })
            {
                inputField.interactable = true;
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                submitButton.interactable = true;
            }

            isChatPending = false;
        }
    }



    public async void GenerateSpeech(string text)
    {
        text = text.Replace("![Image](output.jpg)", string.Empty);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        sentences = Regex.Split(text, @"(?<=[\.!\?])\s+");
        clips = new AudioClip[sentences.Length];
        sentenceIndex = 0;
        int i = 0;
        bool initAudio = true;
        foreach (string sentence in sentences)
        {
            var request = new SpeechRequest(sentence, Model.TTS_1);
            var (clipPath, clip) = await openAI.AudioEndpoint.CreateSpeechAsync(request, destroyCancellationToken);

            clips[i] = clip;
            i++;

            if (initAudio)
            {
                StartCoroutine(PlayAudioSequentially());
                initAudio = false;
            }

            if (enableDebug)
            {
                Debug.Log(clipPath);
            }
        }
    }

    IEnumerator PlayAudioSequentially()
    {
        yield return null;

        for (int i = 0; i < clips.Length; i++)
        {
            sentenceIndex = i;
            audioSource.clip = clips[i];
            audioSource.Play();

            while (audioSource.isPlaying)
            {
                yield return null;
            }
        }
    }

    private TextMeshProUGUI AddNewTextMessageContent(Role role)
    {
        var textObject = new GameObject($"{contentArea.childCount + 1}_{role}");
        textObject.transform.SetParent(contentArea, false);
        var textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.fontSize = 24;
#if UNITY_2023_1_OR_NEWER
        textMesh.textWrappingMode = TextWrappingModes.Normal;
#else
        textMesh.enableWordWrapping = true;
#endif
        return textMesh;
    }

    private void AddNewImageContent(Texture2D texture)
    {
        var imageObject = new GameObject($"{contentArea.childCount + 1}_Image");
        imageObject.transform.SetParent(contentArea, false);
        var rawImage = imageObject.AddComponent<RawImage>();
        rawImage.texture = texture;
        var layoutElement = imageObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = texture.height / 4f;
        layoutElement.preferredWidth = texture.width / 4f;
        var aspectRatioFitter = imageObject.AddComponent<AspectRatioFitter>();
        aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        aspectRatioFitter.aspectRatio = texture.width / (float)texture.height;
    }

   public void ToggleRecording()
{
    RecordingManager.EnableDebug = enableDebug;

    if (isRecording)
    {
        RecordingManager.EndRecording();
        isRecording = false;
    }
    else
    {
        RecordingManager.StartRecording<WavEncoder>(callback: ProcessRecording);
        isRecording = true;
    }
}

// public void StartContinuousRecording()
// {
//     RecordingManager.EnableDebug = enableDebug;
//     RecordingManager.StartRecording<WavEncoder>(callback: ProcessRecording);
//     isRecording = true;
// }

private async void ProcessRecording(Tuple<string, AudioClip> recording)
{
    var (path, clip) = recording;

    if (enableDebug)
    {
        Debug.Log($"Recording saved at: {path}");
    }

    try
    {
        var request = new AudioTranscriptionRequest(clip, temperature: 0.1f, language: "en");
        string transcriptionResult = await openAI.AudioEndpoint.CreateTranscriptionAsync(request, destroyCancellationToken);
        Debug.Log($"Transcription result: {transcriptionResult}");
        userInput = transcriptionResult;

        if (!string.IsNullOrWhiteSpace(transcriptionResult))
        {
            transcriptions.Add(transcriptionResult);
            userInput = transcriptionResult; // Set the userInput to the latest transcription
            combinedInput = transcriptionResult;
            
            // Parse the transcription into JSON format
            jsonText = await ParseTranscriptionToJSON(transcriptionResult);
            Debug.Log($"Parsed JSON: {jsonText}");
        }
        else
        {
            Debug.LogWarning("Transcription is empty or failed.");
        }

        if (enableDebug)
        {
            Debug.Log($"Submitting {userInput} to LLM");
            Debug.Log($"Combined input: {combinedInput}");
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"Error in transcription: {e.Message}");
        inputField.interactable = true;
    }
}

private async Task<string> ParseTranscriptionToJSON(string transcription)
{
    var systemPromptJSON = "Parse this transcription into JSON format with fields: motion, rationale, and condition.";
    var jsonConversation = new Conversation();
    jsonConversation.AppendMessage(new Message(Role.System, systemPromptJSON));
    jsonConversation.AppendMessage(new Message(Role.User, transcription));

    var request = new ChatRequest(jsonConversation.Messages);
    var response = await openAI.ChatEndpoint.GetCompletionAsync(request);
    var jsonResponse = response.FirstChoice.Message.Content.ToString();

    return jsonResponse;
}



public void SubmitCombinedInput(string field)
{
    if (enableDebug)
    {
        Debug.Log($"input is {combinedInput}");
    }

    inputField.text = userInput;
    SubmitChatNoSpeech(field);
    
}

    public List<string> GetTranscriptions()
    {
        return transcriptions;
    }
}

    
}