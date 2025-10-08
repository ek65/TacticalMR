using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework.Constraints;
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
        [Header("UI")] public Button button;
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
        private ProgramSynthesisManager programSynthesisManager;
        public bool transDone = false;
        public int tokenOrder = 0;
        public bool isSpeechDetected = false;
        


        // Initialize the necessary components, set up event listeners, and start the transcription stream
        private async void Start()
        {
            programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();
            jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
            _stream = await whisper.CreateStream(microphoneRecord);
            _stream.OnResultUpdated += OnResult;
            _stream.OnSegmentUpdated += OnPhraseUpdated;
            _stream.OnSegmentFinished += OnPhraseFinished;
            _stream.OnStreamFinished += OnFinished;
            microphoneRecord.OnRecordStop += OnRecordStop;
            microphoneRecord.OnChunkReady += OnChunkReady;
            button.onClick.AddListener(OnButtonPressed);
        }
        
        private void OnChunkReady(AudioChunk chunk)
        {
            isSpeechDetected = CheckForAudioInput(chunk);
        }

        // Method to check for audio input based on chunk amplitude
        private bool CheckForAudioInput(AudioChunk chunk)
        {
            float sum = 0;
            foreach (var sample in chunk.Data)
            {
                sum += sample * sample;
            }
            float rmsValue = Mathf.Sqrt(sum / chunk.Data.Length);

            float rmsThreshold = 0.01f; 
            return rmsValue > rmsThreshold;
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
            programSynthesisManager.explanation = ""; // Clear the explanation in KeyboardInput
            jsonToLLM.ResetSegmentData(); // Clear tokens in JSONToLLM
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

            // Remove links from the phrase before updating the current phrase
            currentPhrase = RemoveLinks(phrase.Result);

            foreach (var seg in phrase.Segments)
            {
                text.text = currentPhrase;
                UiUtils.ScrollDown(scroll);
            }
        }

        private string RemoveLinks(string text)
        {
            // Regular expression to identify URLs, emails, unwanted patterns, and specific unwanted words/phrases
            string unwantedPattern = @"(http[s]?://|ftp://|www\.|\.html|\b\S+\.com\b|\b\S+\.html\b|\b\S+\.org\b|\burn\b|\b\S*/sport\S*|\b\S*@\S+\b|urn@\S+)";

            // Remove any unwanted patterns found in the text
            return Regex.Replace(text, unwantedPattern, string.Empty, RegexOptions.IgnoreCase).Trim();
        }


        // Finalize the current phrase when transcription is complete and clear annotation keys
        private void OnPhraseFinished(WhisperResult phrase)
        {
           
                // Remove links from the phrase before adding it
                currentPhrase = RemoveLinks(phrase.Result);

                phrases.Add(phrase);
                phraseStrings.Add(currentPhrase);
                Debug.Log($"phrase finished: {currentPhrase}");
                phraseUpdated = false;
                currentPhrase = "";
                annotationKeys.Clear(); // Clear annotation keys for the next phrase
            
            foreach (var phr in phrase.Segments)
            {
                foreach (var token in phr.Tokens)
                {
                    if (token.Timestamp != null)
                    {
                        float tokenTime = jsonToLLM.time + (float)((token.Timestamp.Start.TotalSeconds));
                        string cleanTokenText = RemoveLinks(token.Text);
                        if (!string.IsNullOrEmpty(cleanTokenText) && !token.IsSpecial)
                        {
                            jsonToLLM.tokenDictionary[tokenOrder] = new List<object> { cleanTokenText, tokenTime };
                            tokenOrder++; 
                            Debug.Log($"Token saved at order: {tokenOrder}, Time: {tokenTime:F2} seconds, Text: {cleanTokenText}");
                        }
                    }
                }
            }
            
        }


        // Compile the finalized transcription phrases into one string and process the tokens
        private void OnFinished(string finalResult)
        {
            finalTranscriptionString = string.Join(" ", phraseStrings);
            text.text = finalTranscriptionString;
            Debug.Log($"Final transcription: {finalTranscriptionString}");

            // Pass the cleaned transcription to the keyboard
            programSynthesisManager.OnTranscriptionFinished(finalTranscriptionString);
            transDone = true;

            Debug.Log("NEW TRANSCRIPTION");

            // Initialize the key index for ordered storage
            // int tokenOrder = 0;
            //
            // foreach (var phrase in phrases)
            // {
            //     foreach (var phr in phrase.Segments)
            //     {
            //         foreach (var token in phr.Tokens)
            //         {
            //             if (token.Timestamp != null)
            //             {
            //                 
            //                 // Adjust the time as necessary
            //                 float tokenTime = jsonToLLM.time + (float)((token.Timestamp.Start.TotalSeconds));
            //
            //                 // Remove unwanted text patterns and very short tokens
            //                 string cleanTokenText = RemoveLinks(token.Text);
            //
            //                 // Only include tokens that are not empty after cleaning, have meaningful length, and aren't special tokens
            //                 if (!string.IsNullOrEmpty(cleanTokenText) && !token.IsSpecial)
            //                 {
            //                     jsonToLLM.tokenDictionary[tokenOrder] = new List<object> { cleanTokenText, tokenTime };
            //                     tokenOrder++; // Increment the order index
            //                     Debug.Log($"Token saved at order: {tokenOrder}, Time: {tokenTime:F2} seconds, Text: {cleanTokenText}");
            //                 }
            //                 
            //                 jsonToLLM.ProcessTokens();
            //             }
            //         }
            //     }
            // }
            //
            // transDone = false;
        }
    }
}

