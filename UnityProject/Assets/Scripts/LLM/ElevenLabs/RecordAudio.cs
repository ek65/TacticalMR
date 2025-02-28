using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Records audio from the microphone, saves as WAV, and invokes Scribe after 1 second.
/// </summary>
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
        // Using the first device in the Microphone.devices array
        // You can customize which device you pick
        string device = Microphone.devices[0];
        int sampleRate = 44100;
        int lengthSec = 3599; // ~1 hour max

        // Start the mic
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

        // Stop the mic
        Microphone.End(null);

        // Calculate how long we actually recorded
        recordingLength = Time.realtimeSinceStartup - startTime;
        recordedClip = TrimClip(recordedClip, recordingLength);

        // Save the file
        SaveRecording();

        // Invoke sending to Scribe **1 second** later
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

        // We'll create "Recordings/recording.wav"
        string fullPath = Path.Combine(directoryPath, fileName);
        WavUtility.Save(fullPath, recordedClip);

        Debug.Log("Recording saved as: " + fullPath);
    }

    /// <summary>
    /// Invoke this after a short delay to send the WAV file to ElevenLabs.
    /// </summary>
    private void SendRecordingToScribe()
    {
        if (scribe == null)
        {
            Debug.LogWarning("No Scribe reference set—cannot transcribe.");
            return;
        }

        // The final path to "Recordings/recording.wav"
        string fullPath = Path.Combine(directoryPath, fileName);

        Debug.Log("Sending WAV to Scribe: " + fullPath);
        scribe.TranscribeAudio(fullPath);
    }

    /// <summary>
    /// Trims the AudioClip so it only includes the actual recorded portion.
    /// </summary>
    /// <param name="clip">The full AudioClip from Microphone.</param>
    /// <param name="length">Number of seconds actually recorded.</param>
    private AudioClip TrimClip(AudioClip clip, float length)
    {
        int samples = (int)(clip.frequency * length);
        float[] data = new float[samples];
        clip.GetData(data, 0);

        // Create a new clip with the exact length
        AudioClip trimmedClip = AudioClip.Create(clip.name, samples,
            clip.channels, clip.frequency, false);
        trimmedClip.SetData(data, 0);

        return trimmedClip;
    }
}
