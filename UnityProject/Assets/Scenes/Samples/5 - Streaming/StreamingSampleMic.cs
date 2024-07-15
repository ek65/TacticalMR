using System.Collections.Generic;
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

        [Header("UI")]
        public Button button;
        public Text buttonText;
        public Text text;
        public ScrollRect scroll;
        private WhisperStream _stream;
        private List<string> segments = new List<string>();
        private List<string> annotationKeys = new List<string>();
        private string currentSegment = "";
        private string finalTranscription = "";
        private bool segmentUpdated = false;
        private KeyboardInput keyboard;

        private async void Start()
        {
            keyboard = GameObject.FindGameObjectWithTag("player").GetComponent<KeyboardInput>();
            _stream = await whisper.CreateStream(microphoneRecord);
            _stream.OnResultUpdated += OnResult;
            _stream.OnSegmentUpdated += OnSegmentUpdated;
            _stream.OnSegmentFinished += OnSegmentFinished;
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
            // Update the current segment with real-time transcription results
            if (!segmentUpdated)
            {
                currentSegment = result;
                segmentUpdated = true;
            }
            else
            {
                currentSegment += result;
            }

            // Append annotation keys to the current segment
            foreach (var key in annotationKeys)
            {
                currentSegment += $" [{key}]";
            }

            text.text = currentSegment;
            UiUtils.ScrollDown(scroll);
        }

        private void OnSegmentUpdated(WhisperResult segment)
        {
            print($"Segment updated: {segment.Result}");
            currentSegment = segment.Result;

            // Append annotation keys to the current segment
            foreach (var key in annotationKeys)
            {
                currentSegment += $" [{key}]";
            }

            text.text = currentSegment;
            UiUtils.ScrollDown(scroll);
        }

        private void OnSegmentFinished(WhisperResult segment)
        {
            // Finalize the current segment
            if (segmentUpdated)
            {
                currentSegment = segment.Result;

                // Append annotation keys to the current segment
                foreach (var key in annotationKeys)
                {
                    currentSegment += $" [{key}]";
                }

                segments.Add(currentSegment);
                Debug.Log($"Segment finished: {currentSegment}");
                segmentUpdated = false;
                currentSegment = "";
                annotationKeys.Clear(); // Clear annotation keys for the next segment
            }
        }

        private void OnFinished(string finalResult)
        {
            // Compile only the finalized segments into one final transcription
            finalTranscription = string.Join(" ", segments);
            text.text = finalTranscription;
            Debug.Log("Final");
            Debug.Log(finalTranscription);
            keyboard.OnTranscriptionFinished(finalTranscription);
        }

        public void InsertAnnotationKey(int key)
        {
            if (!string.IsNullOrEmpty(currentSegment))
            {
                annotationKeys.Add(key.ToString()); // Add key to the annotation list
                currentSegment += $" [{key}]"; // Update the current segment with the new key
                text.text = currentSegment;
                Debug.Log($"Updated current segment with key: {currentSegment}");
                UiUtils.ScrollDown(scroll);
            }
        }
    }
}
