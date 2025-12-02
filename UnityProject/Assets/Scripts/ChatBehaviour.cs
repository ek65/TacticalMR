// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Debug = UnityEngine.Debug;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace OpenAI.Samples.Chat
{
    [RequireComponent(typeof(StreamAudioSource))]
    public class ChatBehaviour : MonoBehaviour
    {
        public static ChatBehaviour Instance { get; private set; }

        [Header("Click capture (hint only)")]
        [SerializeField] private bool includeClickHints = true;
        private readonly List<Vector3> pendingClickPositions = new();
        [SerializeField] private bool includeSelectionHints = true;
        private readonly List<string> pendingSelectedObjects = new(); // store stable names/ids

        // Track recording start time for proper timestamp alignment
        private float recordingStartTime = 0f;

        // === Structured Output wiring ===
        [SerializeField] private bool useStructuredOutput = true;
        [SerializeField, Tooltip("Where to spawn prefabs & apply attributes/behaviors.")]
        private ScenarioPlanExecutor planExecutor;
        [SerializeField, Tooltip("Memory system that records all LLM responses with timestamps.")]
        private ScenarioMemory scenarioMemory;
        
        private TimelineManager timelineManager;
        private ProgramSynthesisManager programSynthesisManager;
        private JSONToLLM jsonToLLM;
        private bool isRecordingForProgramSynthesis = false;
        
        [SerializeField]
        private OpenAIConfiguration configuration;

        [SerializeField]
        private bool enableDebug;

        [SerializeField]
        private Button submitButton;

        [SerializeField]
        private Button recordButton;

        [SerializeField]
        private TMP_InputField inputField;

        [SerializeField]
        private RectTransform contentArea;

        [SerializeField]
        private ScrollRect scrollView;

        [SerializeField]
        private StreamAudioSource streamAudioSource;

        [SerializeField]
        private Voice voice;

        // Force STRICT JSON output for scenario planning.
        private string systemPrompt =
            "You are a scenario planner for a Unity simulation. " +
            "Output ONLY raw, minified JSON that matches this schema EXACTLY (no markdown, no commentary): " +
            "{ \"objects\": [ { " +
            "\"prefab\":\"string\", " +
            "\"name\":\"string(REQUIRED)\", " +
            "\"position\":[x,y,z], " +
            "\"rotation\":[x,y,z], " +
            "\"scale\":[x,y,z], " +
            "\"actions\":[{\"name\":\"string\",\"parameters\":{\"paramName\":\"value\"}}] } ] } " +
            "Rules: " +
            "- Always return an 'objects' array (can be empty). " +
            "- Every object MUST have a unique 'name' field. " +
            "- Never include explanations.";

        private OpenAIClient openAI;

        private readonly Conversation conversation = new();
        private readonly List<Tool> assistantTools = new();

#if !UNITY_2022_3_OR_NEWER
        private readonly CancellationTokenSource lifetimeCts = new();
        // ReSharper disable once InconsistentNaming
        private CancellationToken destroyCancellationToken => lifetimeCts.Token;
#endif

        private void OnValidate()
        {
            inputField.Validate();
            contentArea.Validate();
            submitButton.Validate();
            recordButton.Validate();

            if (streamAudioSource == null)
            {
                streamAudioSource = GetComponent<StreamAudioSource>();
            }
        }

        private void Awake()
        {
            Instance = this;
            
            OnValidate();
            openAI = new OpenAIClient(configuration)
            {
                EnableDebug = enableDebug
            };
            RecordingManager.EnableDebug = enableDebug;

            // Initialize TimelineManager reference
            timelineManager = GameObject.FindGameObjectWithTag("TimelineManager")?.GetComponent<TimelineManager>();
            if (timelineManager == null)
            {
                Debug.LogWarning("TimelineManager not found. Pause during recording will not work.");
            }

            // Initialize ProgramSynthesisManager reference
            programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager")?.GetComponent<ProgramSynthesisManager>();
            if (programSynthesisManager == null)
            {
                Debug.LogWarning("ProgramSynthesisManager not found. Program synthesis recording will not work.");
            }

            // Initialize JSONToLLM reference
            jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager")?.GetComponent<JSONToLLM>();
            if (jsonToLLM == null)
            {
                Debug.LogWarning("JSONToLLM not found. Token timing will not work.");
            }

            assistantTools.Add(Tool.GetOrCreateTool(openAI.ImagesEndPoint, nameof(ImagesEndpoint.GenerateImageAsync)));
            conversation.AppendMessage(new Message(Role.System, systemPrompt +
                                                                " Rules:\n- Always return an 'objects' array (can be empty).\n- Every object MUST have a unique 'name' field.\n- Never include explanations.\n\nSPAWNING NEW OBJECTS:\n- Include: 'prefab', 'name', 'position', 'rotation', 'scale'\n- Use clicked_positions for placement\n\nUPDATING EXISTING OBJECTS:\n- Include ONLY: 'name' and 'actions'\n- DO NOT include: 'prefab', 'position', 'rotation', 'scale'\n- If [selected_objects] contains the object name, it EXISTS - do not spawn it again\n\nAdditional rules: \n(1) If the user message includes [clicked_positions:...] and/or [selected_objects:...], treat them as HINTS to resolve deictic references like 'this one' or 'here'. \n(2) When assigning behaviors to already-spawned objects, identify them by a stable name from selected_objects when available.\");"
            ));
            // Constrain LLM to only use configured prefabs
            var validPrefabs = planExecutor.GetValidPrefabKeys();
            var prefabListJson = JsonConvert.SerializeObject(validPrefabs, Formatting.None);
            conversation.AppendMessage(new Message(
                Role.System,
                "AllowedPrefabs=" + prefabListJson +
                " Rules: You can ONLY spawn objects using these prefab keys. " +
                "Use these exact strings (case-insensitive) for the 'prefab' field. " +
                "Do not invent or use any prefab names not in this list."
            ));
            var actionCatalogJson = ActionCatalog.BuildJson();
            conversation.AppendMessage(new Message(
                Role.System,
                "AllowedActionCatalog=" + actionCatalogJson +
                " Rules: The only actions that players/robots can take are provided here, based on the entire list of action functions. Note that objects such as boxes CANNOT take actions, only players/robots can. " +
                "Use only 'name' values in this catalog for 'actions[].func'. " +
                "Map Vector3 as {\"x\":float,\"y\":float,\"z\":float}. " +
                "Do not invent functions not listed here."
            ));
            inputField.onSubmit.AddListener(SubmitChat);
            submitButton.onClick.AddListener(SubmitChat);
            recordButton.onClick.AddListener(ToggleRecording);
        }

#if !UNITY_2022_3_OR_NEWER
        private void OnDestroy()
        {
            lifetimeCts.Cancel();
        }
#endif

        private void SubmitChat(string _) => SubmitChat();

        private static bool isChatPending;

        private async void SubmitChat()
        {
            if (isChatPending || string.IsNullOrWhiteSpace(inputField.text)) { return; }
            isChatPending = true;

            inputField.ReleaseSelection();
            inputField.interactable = false;
            submitButton.interactable = false;
            conversation.AppendMessage(new Message(Role.User, inputField.text));
            var userMessageContent = AddNewTextMessageContent(Role.User);
            userMessageContent.text = $"User: {inputField.text}";
            inputField.text = string.Empty;
            var assistantMessageContent = AddNewTextMessageContent(Role.Assistant);
            assistantMessageContent.text = "Assistant: ";

            try
            {
                var messages = conversation.Messages;
                if (useStructuredOutput)
                {
                    // Ensure the very first message is the structured system prompt (prepend only once).
                    if (messages.Count == 0 || messages[0].Role != Role.System)
                    {
                        conversation.AppendMessage(new Message(Role.System, systemPrompt));
                    }
                }
                
                var request = new ChatRequest(messages, tools: assistantTools);
                var response = await openAI.ChatEndpoint.StreamCompletionAsync(request, resultHandler: deltaResponse =>
                {
                    if (deltaResponse?.FirstChoice?.Delta == null) { return; }
                    assistantMessageContent.text += deltaResponse.FirstChoice.Delta.ToString();
                    scrollView.verticalNormalizedPosition = 0f;
                }, cancellationToken: destroyCancellationToken);

                conversation.AppendMessage(response.FirstChoice.Message);

                if (response.FirstChoice.FinishReason == "tool_calls")
                {
                    response = await ProcessToolCallsAsync(response);
                    assistantMessageContent.text += response.ToString().Replace("![Image](output.jpg)", string.Empty);
                }

                Debug.Log("Final Assistant Response: " + response);

                // await GenerateSpeechAsync(response, destroyCancellationToken);
                
                // === Structured Output → Execute Plan ===
                if (useStructuredOutput && planExecutor != null)
                {
                    // Get the final assistant text as a string
                    var finalText = response.ToString();
                    finalText = finalText.Replace("![Image](output.jpg)", string.Empty);
                    finalText = StripCodeFences(finalText).Trim();

                    if (ScenarioPlanParser.TryParse(finalText, out var plan, out var parseError))
                    {
                        // Execute the plan
                        planExecutor.Apply(plan);
                        
                        // Record the plan in memory for replay
                        if (scenarioMemory != null)
                        {
                            scenarioMemory.RecordScenarioPlan(plan);
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"Structured JSON parse failed: {parseError}\nRaw: {finalText}");
                    }
                }
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
                        Debug.Log($"{response.FirstChoice.Message.Role}: {toolCall.Function.Name} | Finish Reason: {response.FirstChoice.FinishReason}");
                        Debug.Log($"{toolCall.Function.Arguments}");
                    }

                    toolCalls.Add(ProcessToolCall());

                    async Task ProcessToolCall()
                    {
                        await Awaiters.UnityMainThread;
                        string result;

                        try
                        {
                            var imageResults = await toolCall.InvokeFunctionAsync<IReadOnlyList<ImageResult>>(destroyCancellationToken).ConfigureAwait(true);

                            foreach (var imageResult in imageResults)
                            {
                                AddNewImageContent(imageResult);
                            }

                            result = JsonConvert.SerializeObject(new { result = imageResults });
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                            result = JsonConvert.SerializeObject(new { error = new Error(e) });
                        }

                        conversation.AppendMessage(new(toolCall, result));
                    }
                }


                await Task.WhenAll(toolCalls).ConfigureAwait(true);
                ChatResponse toolCallResponse;

                try
                {
                    var toolCallRequest = new ChatRequest(conversation.Messages, tools: assistantTools);
                    toolCallResponse = await openAI.ChatEndpoint.GetCompletionAsync(toolCallRequest, destroyCancellationToken);
                    conversation.AppendMessage(toolCallResponse.FirstChoice.Message);
                }
                catch (RestException restEx)
                {
                    Debug.LogError(restEx);

                    foreach (var toolCall in response.FirstChoice.Message.ToolCalls)
                    {
                        conversation.AppendMessage(new Message(toolCall, restEx.Response.Body));
                    }

                    var toolCallRequest = new ChatRequest(conversation.Messages, tools: assistantTools);
                    toolCallResponse = await openAI.ChatEndpoint.GetCompletionAsync(toolCallRequest, destroyCancellationToken);
                    conversation.AppendMessage(toolCallResponse.FirstChoice.Message);
                }

                if (toolCallResponse.FirstChoice.FinishReason == "tool_calls")
                {
                    return await ProcessToolCallsAsync(toolCallResponse);
                }

                return toolCallResponse;
            }
        }

        private async Task GenerateSpeechAsync(string text, CancellationToken cancellationToken)
        {
            text = text.Replace("![Image](output.jpg)", string.Empty);
            if (string.IsNullOrWhiteSpace(text)) { return; }
            var request = new SpeechRequest(input: text, model: OpenAI.Models.Model.TTS_1, voice: voice, responseFormat: SpeechResponseFormat.PCM);
            var stopwatch = Stopwatch.StartNew();
            var speechClip = await openAI.AudioEndpoint.GetSpeechAsync(request, partialClip =>
            {
                streamAudioSource.BufferCallback(partialClip.AudioSamples);
            }, cancellationToken);
            var playbackTime = speechClip.Length - (float)stopwatch.Elapsed.TotalSeconds + 0.1f;

            await Awaiters.DelayAsync(TimeSpan.FromSeconds(playbackTime), cancellationToken).ConfigureAwait(true);
            ((AudioSource)streamAudioSource).clip = speechClip.AudioClip;

            if (enableDebug)
            {
                Debug.Log(speechClip.CachePath);
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

        private void ToggleRecording()
        {
            RecordingManager.EnableDebug = enableDebug;

            var groundSelections = GameObject.FindGameObjectsWithTag("Ground");
        
            foreach (var gs in groundSelections)
            {
                gs.GetComponent<GroundSelection>().ClearGroundHighlights();
            }

            var groundHighlights = GameObject.FindGameObjectsWithTag("GroundHighlight");
        
            foreach (var gh in groundHighlights)
            {
                Destroy(gh);
            }


            if (RecordingManager.IsRecording)
            {
                RecordingManager.EndRecording();
                // Note: Timeline will be unpaused after transcription completes in ProcessRecording
                // DO NOT stop program synthesis here - it continues until spacebar
            }
            else
            {
                pendingClickPositions.Clear(); // <— start fresh for this utterance
                pendingSelectedObjects.Clear();
                
                // Pause the timeline during voice recording to prevent time from advancing
                if (timelineManager != null)
                {
                    timelineManager.Pause();
                    if (enableDebug)
                    {
                        Debug.Log("Timeline paused for voice recording");
                    }
                }
                
                // Start program synthesis recording ONLY on first mic press
                if (!isRecordingForProgramSynthesis && 
                    ScenarioTypeManager.Instance != null && 
                    ScenarioTypeManager.Instance.currentScenario == ScenarioTypeManager.ScenarioType.FactoryScenarioCreation)
                {
                    if (programSynthesisManager != null && !programSynthesisManager.segmentStarted)
                    {
                        programSynthesisManager.HandleSegment();
                        isRecordingForProgramSynthesis = true;
                        if (enableDebug)
                        {
                            Debug.Log("Started program synthesis recording (will continue until spacebar)");
                        }
                    }
                }
                
                // Capture recording start time for timestamp alignment
                if (ScenarioTypeManager.Instance != null && 
                    ScenarioTypeManager.Instance.currentScenario == ScenarioTypeManager.ScenarioType.FactoryScenarioCreation &&
                    jsonToLLM != null)
                {
                    recordingStartTime = jsonToLLM.time;
                    if (enableDebug)
                    {
                        Debug.Log($"Recording started at jsonToLLM.time = {recordingStartTime:F3}s");
                    }
                }
                
                inputField.interactable = false;
                // ReSharper disable once MethodSupportsCancellation
                RecordingManager.StartRecording<WavEncoder>(callback: ProcessRecording);
            }
        }

        private async void ProcessRecording(Tuple<string, AudioClip> recording)
        {
            var (path, clip) = recording;

            if (enableDebug)
            {
                Debug.Log(path);
            }

            try
            {
                recordButton.interactable = false;
                
                // Use verbose JSON response with word timestamps if in FactoryScenarioCreation mode
                string userInput;
                if (ScenarioTypeManager.Instance != null && 
                    ScenarioTypeManager.Instance.currentScenario == ScenarioTypeManager.ScenarioType.FactoryScenarioCreation &&
                    jsonToLLM != null)
                {
                    using var verboseRequest = new AudioTranscriptionRequest(clip, responseFormat: AudioResponseFormat.Verbose_Json, timestampGranularity: TimestampGranularity.Word, temperature: 0.1f, language: "en");
                    var response = await openAI.AudioEndpoint.CreateTranscriptionJsonAsync(verboseRequest, destroyCancellationToken);
                    
                    // Use recording start time as the base for word timestamps
                    // This ensures words align with annotations that were created during recording
                    float baseTime = recordingStartTime;
                    
                    if (enableDebug)
                    {
                        Debug.Log($"Using recordingStartTime = {baseTime:F3}s as base for token timestamps");
                    }
                    
                    // Populate token dictionary with word timestamps
                    if (response.Words != null && response.Words.Any())
                    {
                        foreach (var word in response.Words)
                        {
                            // Calculate timestamp: recording start time + word offset within audio
                            // Whisper provides timestamps relative to the audio clip start
                            float timestamp = baseTime + (float)word.Start;
                            
                            if (!jsonToLLM.tokenDictionary.ContainsKey(timestamp))
                            {
                                jsonToLLM.tokenDictionary[timestamp] = new List<object>();
                            }
                            jsonToLLM.tokenDictionary[timestamp].Add(word.Word);
                            
                            if (enableDebug)
                            {
                                Debug.Log($"[{timestamp:F3}] \"{word.Word}\" (word.Start={word.Start:F3}, baseTime={baseTime:F3})");
                            }
                        }
                    }
                    
                    userInput = response.Text;
                }
                else
                {
                    // Regular transcription without timestamps
                    var request = new AudioTranscriptionRequest(clip, temperature: 0.1f, language: "en");
                    userInput = await openAI.AudioEndpoint.CreateTranscriptionTextAsync(request, destroyCancellationToken);
                }
                
                if (enableDebug)
                {
                    Debug.Log(userInput);
                }
                
                // Build hint: [["x","y","z"], ...] (minified) — HINT ONLY
                string hintSuffix = BuildClickHintSuffix();

                inputField.text = string.IsNullOrWhiteSpace(hintSuffix) ? userInput : $"{userInput}\n{hintSuffix}";

                // inputField.text = userInput;
                SubmitChat();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                inputField.interactable = true;
                
                // Unpause timeline even on error
                if (timelineManager != null)
                {
                    timelineManager.Unpause();
                    if (enableDebug)
                    {
                        Debug.Log("Timeline unpaused (error recovery)");
                    }
                }
            }
            finally
            {
                recordButton.interactable = true;
                
                // Unpause the timeline after transcription and chat submission
                if (timelineManager != null)
                {
                    timelineManager.Unpause();
                    if (enableDebug)
                    {
                        Debug.Log("Timeline unpaused after voice recording");
                    }
                }
            }
        }
        
        private string BuildClickHintSuffix()
        {
            var chunks = new List<string>();

            if (includeClickHints && pendingClickPositions.Count > 0)
            {
                var arr = pendingClickPositions.Select(v => new[] { v.x, v.y, v.z }).ToArray();
                string posPayload = JsonConvert.SerializeObject(arr, Formatting.None);
                chunks.Add($"[clicked_positions:{posPayload}]");
            }

            if (includeSelectionHints && pendingSelectedObjects.Count > 0)
            {
                // keep it simple: a flat array of names
                var uniq = pendingSelectedObjects.Distinct().ToArray();
                string selPayload = JsonConvert.SerializeObject(uniq, Formatting.None);
                chunks.Add($"[selected_objects:{selPayload}]");
            }

            return chunks.Count == 0 ? null : string.Join("\n", chunks);
        }
        
        private static string StripCodeFences(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            s = s.Trim();
            if (s.StartsWith("```"))
            {
                s = System.Text.RegularExpressions.Regex.Replace(s, "^```(json)?\\s*", "");
                s = System.Text.RegularExpressions.Regex.Replace(s, "\\s*```$", "");
            }
            return s.Trim();
        }
        
        public void RegisterClick(Vector3 worldPosition)
        {
            pendingClickPositions.Add(worldPosition);
        }
        
        public void RegisterSelectedObject(GameObject go)
        {
            if (go == null) return;

            // Prefer a stable, human-readable identifier the LLM can also emit in JSON.
            // 1) If the object has PlayerInterface with a Networked name, use that
            var pi = go.GetComponent<PlayerInterface>();
            if (pi != null)
            {
                var nameNet = pi.ObjName.ToString();
                if (!string.IsNullOrWhiteSpace(nameNet))
                {
                    pendingSelectedObjects.Add(nameNet);
                    return;
                }
            }

            // 2) Else fall back to GameObject.name (works for ball/goal, etc.)
            pendingSelectedObjects.Add(go.name);
        }

        /// <summary>
        /// Reset program synthesis recording flag (called when spacebar stops recording)
        /// </summary>
        public void ResetProgramSynthesisRecording()
        {
            isRecordingForProgramSynthesis = false;
            if (enableDebug)
            {
                Debug.Log("Reset program synthesis recording flag");
            }
        }

    }
    
    static class ActionCatalog
    {
        // Types we expose cleanly to the LLM
        private static readonly HashSet<string> _disallowNames = new(StringComparer.OrdinalIgnoreCase)
        {
            // Unity lifecycle & internal helpers you don’t want the model to call
            "Start","Update","Awake","OnEnable","OnDisable","FixedUpdate","LateUpdate"
        };

        public static string BuildJson()
        {
            var t = typeof(ActionAPI);
            var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var actions = new JArray();

            foreach (var m in methods)
            {
                if (_disallowNames.Contains(m.Name)) continue;
                if (m.IsSpecialName) continue;          // property getters/setters
                if (m.Name.StartsWith("RPC_", StringComparison.Ordinal)) continue;

                // Optional: allow only void-returning methods
                if (m.ReturnType != typeof(void)) continue;

                var pList = new JArray();
                foreach (var p in m.GetParameters())
                {
                    pList.Add(new JObject {
                        ["name"] = p.Name,
                        ["type"] = PrettyType(p.ParameterType)
                    });
                }

                actions.Add(new JObject {
                    ["name"] = m.Name,
                    ["params"] = pList
                });
            }

            var root = new JObject { ["actions"] = actions };
            return root.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static string PrettyType(Type t)
        {
            if (t == typeof(UnityEngine.Vector3)) return "Vector3{x,y,z}";
            if (t == typeof(float)) return "float";
            if (t == typeof(int)) return "int";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "string";
            // Extend as needed (Vector2, Quaternion as Euler, etc.)
            return t.Name;
        }
    }
}