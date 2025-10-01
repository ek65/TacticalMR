using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RecordAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    // Where we'll save the final file: "Recordings/recording.wav"
    private string directoryPath = "Recordings";
    private string fileName = "recording.wav";

    private AudioClip recordedClip;
    private float startTime;
    private float recordingLength;

    // Reference to the Scribe script
    [Header("ElevenLabs Integration")]
    public Scribe scribe;

    private void Awake()
    {
        // Create the Recordings folder if needed
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Begin recording from the default mic device.
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
    /// End the recording, trim to the actual length, save the file, and schedule the upload.
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
    /// Converts the AudioClip to a WAV file and saves it to disk.
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
        Debug.Log("Recording saved as: " + fullPath);
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

        AudioClip trimmedClip = AudioClip.Create(clip.name, samples,
            clip.channels, clip.frequency, false);
        trimmedClip.SetData(data, 0);

        return trimmedClip;
    }
}
