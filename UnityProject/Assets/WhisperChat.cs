using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Whisper;
using Whisper.Utils;
using UnityEngine;
using UnityEngine.UI;


public class WhisperChat : MonoBehaviour
{
    // Start is called before the first frame update
    public WhisperManager whisper;
    public MicrophoneRecord microphoneRecord;
    
    [Header("UI")] 
    public Button button;
    public Text buttonText;
    public Text text;
    public ScrollRect scroll;
    private WhisperStream _stream;

    private async void Start()
    {
        _stream = await whisper.CreateStream(microphoneRecord);
        _stream.OnResultUpdated += OnResult;
        _stream.OnSegmentUpdated += OnSegmentUpdated;
        _stream.OnSegmentFinished += OnSegmentFinished;
        _stream.OnStreamFinished += OnFinished;

        microphoneRecord.OnRecordStop += OnRecordStop;
        button.onClick.AddListener(OnButtonPressed);
    }

    private void OnButtonPressed()
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
        text.text = result;
        UiUtils.ScrollDown(scroll);
    }
        
    private void OnSegmentUpdated(WhisperResult segment)
    {
        print($"Segment updated: {segment.Result}");
    }
        
    private void OnSegmentFinished(WhisperResult segment)
    {
        print($"Segment finished: {segment.Result}");
    }
        
    private void OnFinished(string finalResult)
    {
        print("Stream finished!");
    }
}
