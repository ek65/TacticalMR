// Licensed under the MIT License. See LICENSE in the project root for license information.

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
        private AudioSource audioSource;

        [SerializeField]
        [TextArea(3, 10)]
        private string systemPrompt = "I am a soccer coach giving explanations of the strategy I am using against my opponents. Based on the given soccer strategy explanation provided by the coach who is on the field and the provided positioning data of the field, output a JSON object with three fields: 'motion', 'rationale', and 'condition'. The 'motion' field should describe the action to be taken, 'rationale' explains why this action is necessary within the strategy, and 'condition' specifies the circumstances under which the action is to be initiated.";

        private OpenAIClient openAI;

        private readonly Conversation conversation = new();

        // private readonly List<Tool> assistantTools = new();
        
        // Daniel added variables
        public string[] sentences;
        public AudioClip[] clips;
        public int sentenceIndex;
        public string responseText;
        public string userInput;
        public string jsonText; // json text to be appended to userInput
        public string combinedInput; // userinput + jsonText to send to LLM

        private void OnValidate()
        {
            inputField.Validate();
            contentArea.Validate();
            submitButton.Validate();
            recordButton.Validate();
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
            recordButton.onClick.AddListener(ToggleRecording);
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
            if (isChatPending || string.IsNullOrWhiteSpace(inputField.text)) { return; }
            isChatPending = true;

            // inputField.ReleaseSelection();
            inputField.interactable = false;
            submitButton.interactable = false;
            conversation.AppendMessage(new Message(Role.User, inputField.text));
            // var userMessageContent = AddNewTextMessageContent(Role.User);
            // userMessageContent.text = $"User: {inputField.text}";
            inputField.text = string.Empty;
            var assistantMessageContent = AddNewTextMessageContent(Role.Assistant);
            assistantMessageContent.text = "Expert: ";

            try
            {
                var request = new ChatRequest(conversation.Messages);
                var response = await openAI.ChatEndpoint.StreamCompletionAsync(request, resultHandler: deltaResponse =>
                {
                    if (deltaResponse?.FirstChoice?.Delta == null) { return; }
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
                        Debug.Log($"{response.FirstChoice.Message.Role}: {toolCall.Function.Name} | Finish Reason: {response.FirstChoice.FinishReason}");
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
        
        public async void SubmitChatNoSpeech()
        {
            if (isChatPending || string.IsNullOrWhiteSpace(inputField.text)) { return; }
            isChatPending = true;

            // inputField.ReleaseSelection();
            inputField.interactable = false;
            submitButton.interactable = false;
            conversation.AppendMessage(new Message(Role.User, inputField.text));
            // var userMessageContent = AddNewTextMessageContent(Role.User);
            // userMessageContent.text = $"User: {inputField.text}";
            inputField.text = string.Empty;
            var assistantMessageContent = AddNewTextMessageContent(Role.Assistant);
            assistantMessageContent.text = "Expert: ";

            try
            {
                var request = new ChatRequest(conversation.Messages);
                var response = await openAI.ChatEndpoint.StreamCompletionAsync(request, resultHandler: deltaResponse =>
                {
                    if (deltaResponse?.FirstChoice?.Delta == null) { return; }
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

                        try
                        {
                            var imageResults = await toolCall.InvokeFunctionAsync<IReadOnlyList<ImageResult>>().ConfigureAwait(true);

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

        public async void GenerateSpeech(string text)
        {
            text = text.Replace("![Image](output.jpg)", string.Empty);
            if (string.IsNullOrWhiteSpace(text)) { return; }
            
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
                
                // audioSource.PlayOneShot(clip);
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
            
            
            // text = text.Replace("![Image](output.jpg)", string.Empty);
            // if (string.IsNullOrWhiteSpace(text)) { return; }
            // var request = new SpeechRequest(text, Model.TTS_1);
            // var (clipPath, clip) = await openAI.AudioEndpoint.CreateSpeechAsync(request, destroyCancellationToken);
            // audioSource.PlayOneShot(clip);
            //
            // if (enableDebug)
            // {
            //     Debug.Log(clipPath);
            // }
        }
        
        IEnumerator PlayAudioSequentially()
        {
            yield return null;

            //1.Loop through each AudioClip
            for (int i = 0; i < clips.Length; i++)
            {
                sentenceIndex = i;
                //2.Assign current AudioClip to audiosource
                audioSource.clip = clips[i];

                //3.Play Audio
                audioSource.Play();
                // Debug.LogError(sentenceIndex);

                //4.Wait for it to finish playing
                while (audioSource.isPlaying)
                {
                    yield return null;
                }

                //5. Go back to #2 and play the next audio in the clips array
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

            if (RecordingManager.IsRecording)
            {
                RecordingManager.EndRecording();
            }
            else
            {
                inputField.interactable = false;
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
                var request = new AudioTranscriptionRequest(clip, temperature: 0.1f, language: "en");
                userInput = await openAI.AudioEndpoint.CreateTranscriptionAsync(request, destroyCancellationToken);

                if (enableDebug)
                {
                    Debug.Log(userInput);
                }
                
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                inputField.interactable = true;
            }
            finally
            {
                recordButton.interactable = true;
            }
        }

        public void SubmitCombinedInput()
        {
            // combinedInput = userInput + " " + jsonText;
            inputField.text = userInput;
            SubmitChatNoSpeech();
        }
    }
}
