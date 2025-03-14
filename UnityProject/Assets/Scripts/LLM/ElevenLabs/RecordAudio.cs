using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RecordAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private string directoryPath;
    private string fileName = "recording.wav";

    private AudioClip recordedClip;
    private float startTime;
    private float recordingLength;

    [Header("ElevenLabs Integration")]
    public Scribe scribe;

    private void Awake()
    {
        directoryPath = Path.Combine(Application.persistentDataPath, "Recordings");
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Start recording audio from the default microphone.
    /// </summary>
    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone device found!");
            return;
        }

        string device = Microphone.devices[0];
        int sampleRate = 44100;
        int lengthSec = 3599; // ~1 hour max

        recordedClip = Microphone.Start(device, false, lengthSec, sampleRate);
        startTime = Time.realtimeSinceStartup;

        Debug.Log("Recording started...");
    }

    /// <summary>
    /// Stop recording, save the file, and send to Scribe.
    /// </summary>
    public void StopRecording()
    {
        if (!Microphone.IsRecording(null))
        {
            Debug.LogWarning("StopRecording was called, but Microphone is not recording.");
            return;
        }

        Microphone.End(null);

        recordingLength = Time.realtimeSinceStartup - startTime;
        recordedClip = TrimClip(recordedClip, recordingLength);

        SaveRecording();

        // Send to Scribe after 1 second
        Invoke(nameof(SendRecordingToScribe), 1f);

        Debug.Log("Recording stopped.");
    }

    /// <summary>
    /// Save the audio recording as a WAV file.
    /// </summary>
    private void SaveRecording()
    {
        if (recordedClip == null)
        {
            Debug.LogError("No recording found to save.");
            return;
        }

        string fullPath = Path.Combine(directoryPath, fileName);
        WavUtility.Save(fullPath, recordedClip);
        Debug.Log("Recording saved at: " + fullPath);
    }

    private void SendRecordingToScribe()
    {
        if (scribe == null)
        {
            Debug.LogWarning("No Scribe reference set—cannot transcribe.");
            return;
        }

        string fullPath = Path.Combine(directoryPath, fileName);
        Debug.Log("Sending WAV to Scribe: " + fullPath);
        scribe.TranscribeAudio(fullPath);
    }

    private AudioClip TrimClip(AudioClip clip, float length)
    {
        int samples = (int)(clip.frequency * length);
        float[] data = new float[samples];
        clip.GetData(data, 0);

        AudioClip trimmedClip = AudioClip.Create(clip.name, samples, clip.channels, clip.frequency, false);
        trimmedClip.SetData(data, 0);

        return trimmedClip;
    }
}