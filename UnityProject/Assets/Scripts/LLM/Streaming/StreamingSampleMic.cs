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
        

        // Initialize the necessary components, set up event listeners, and start the transcription stream
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
        

        // Start or stop recording based on the current state of the microphone
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
        
        // Reset the transcription data and clear all relevant lists and variables
        public void ResetTranscriptionData()
        {
            phrases.Clear();
            phraseStrings.Clear();
            currentPhrase = "";
            finalTranscriptionString = "";
            annotationKeys.Clear();
            keyboard.explanation = "";  // Clear the explanation in KeyboardInput
            jsonToLLM.ResetSegmentData();  // Clear tokens in JSONToLLM
            Debug.Log("Transcription data has been reset.");
        }

        // Handle the event when recording stops and update the button text
        private void OnRecordStop(AudioChunk recordedAudio)
        {
            buttonText.text = "Record";
        }

        // Update the current phrase with real-time transcription results and display it
        private void OnResult(string result)
        {
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

        // Handle updates to the phrase during transcription and display the updated text
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

        // Finalize the current phrase when transcription is complete and clear annotation keys
        private void OnPhraseFinished(WhisperResult phrase)
        {
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

        // Compile the finalized transcription phrases into one string and process the tokens
        private void OnFinished(string finalResult)
        {
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
                            // Adjust the time as necessary
                            float tokenTime = jsonToLLM.time + (float)token.Timestamp.Start.TotalSeconds - 18f;
                            
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
    }
}

