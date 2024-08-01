using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;

namespace Whisper.Samples
{
    /// <summary>
    /// Stream transcription from microphone input.
    /// </summary>
    public class StreamingSampleMic : MonoBehaviour
    {
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        private JSONToLLM jsonToLLM;
        [Header("UI")]
        public Button button;
        public Text buttonText;
        public Text text;
        public ScrollRect scroll;
        private WhisperStream _stream;
        private List<WhisperResult> phrases = new List<WhisperResult>();
        private List<string> phraseStrings = new List<string>();
        private List<string> annotationKeys = new List<string>();
        private string currentPhrase = "";
        private string finalTranscriptionString = "";
        private bool phraseUpdated = false;
        private KeyboardInput keyboard;
        

        private async void Start()
        {
            keyboard = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
            jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
            _stream = await whisper.CreateStream(microphoneRecord);
            _stream.OnResultUpdated += OnResult;
            _stream.OnSegmentUpdated += OnPhraseUpdated;
            _stream.OnSegmentFinished += OnPhraseFinished;
            _stream.OnStreamFinished += OnFinished;
            microphoneRecord.OnRecordStop += OnRecordStop;
            button.onClick.AddListener(OnButtonPressed);
        }

        public void OnButtonPressed()
        {
            if (!microphoneRecord.IsRecording)
            {
                _stream.StartStream();
                microphoneRecord.StartRecord();
            }
            else
                microphoneRecord.StopRecord();

            buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
        }

        private void OnRecordStop(AudioChunk recordedAudio)
        {
            buttonText.text = "Record";
        }

        private void OnResult(string result)
        {
            // Update the current phrase with real-time transcription results
            if (!phraseUpdated)
            {
                currentPhrase = result;
                phraseUpdated = true;
            }
            else
            {
                currentPhrase += result;
            }

            text.text = currentPhrase;
            UiUtils.ScrollDown(scroll);
        }

        private void OnPhraseUpdated(WhisperResult phrase)
        {
            Debug.Log($"phrase updated: {phrase.Result}");
            currentPhrase = phrase.Result;
            foreach (var seg in phrase.Segments)
            {
                text.text = currentPhrase;
                UiUtils.ScrollDown(scroll);
            }
        }


        private void OnPhraseFinished(WhisperResult phrase)
        {
            // Finalize the current phrase
            if (phraseUpdated)
            {
                phrases.Add(phrase);
                currentPhrase = phrase.Result;
                phraseStrings.Add(currentPhrase);
                Debug.Log($"phrase finished: {currentPhrase}");
                phraseUpdated = false;
                currentPhrase = "";
                annotationKeys.Clear(); // Clear annotation keys for the next phrase
            }
        }

        private void OnFinished(string finalResult)
        {
            // Compile only the finalized phrases into one final transcription
            finalTranscriptionString = string.Join(" ", phraseStrings);
            text.text = finalTranscriptionString;
            Debug.Log("Final transcription:");
            Debug.Log(finalTranscriptionString);

            keyboard.OnTranscriptionFinished(finalTranscriptionString);

            Debug.Log("NEW TRANSCRIPTION");

            // Initialize the key index for ordered storage
            int tokenOrder = 0;

            foreach (var phrase in phrases)
            {
                foreach (var phr in phrase.Segments)
                {
                    foreach (var token in phr.Tokens)
                    {
                        if (token.Timestamp != null)
                        {
                            // adjust the time as necessary
                            float tokenTime = jsonToLLM.time + (float)token.Timestamp.Start.TotalSeconds - 13.5f;
                            
                            if (!token.IsSpecial && !token.Text.Contains("[") && !token.Text.Contains("<") && !token.Text.Contains("]"))
                            {
                                jsonToLLM.tokenDictionary[tokenOrder] = new List<object> { token.Text, tokenTime };
                                tokenOrder++; // Increment the order index
                            }
                            Debug.Log($"Token saved at order: {tokenOrder}, Time: {tokenTime:F2} seconds, Text: {token.Text}");
                            jsonToLLM.ProcessTokens();
                        }
                    }
                }
            }
        }

        public void InsertAnnotationKey(int key)
        {
            if (!string.IsNullOrEmpty(currentPhrase))
            {
                annotationKeys.Add(key.ToString()); // Add key to the annotation list
                // currentphrase += $" [{key}]"; // Update the current phrase with the new key
                text.text = currentPhrase;
                UiUtils.ScrollDown(scroll);
            }
        }
    }
}
