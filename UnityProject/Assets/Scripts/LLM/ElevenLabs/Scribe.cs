using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using Newtonsoft.Json;

public class Scribe : MonoBehaviour
{
    [Header("ElevenLabs Settings")]
    [Tooltip("Enter your ElevenLabs API Key here.")]
    public string elevenLabsApiKey = "YOUR_ELEVENLABS_API_KEY";

    [Header("Debug / Output")]
    [Tooltip("Will store the last transcribed text.")]
    public string transcriptionText;

    /// <summary>
    /// Call this to upload a WAV file to ElevenLabs and get the transcription.
    /// </summary>
    /// <param name="filePath">Full path to the WAV file.</param>
    public void TranscribeAudio(string filePath)
    {
        StartCoroutine(TranscribeAudioCoroutine(filePath));
    }

    private IEnumerator TranscribeAudioCoroutine(string filePath)
    {
        // 1. Check API key
        if (string.IsNullOrEmpty(elevenLabsApiKey))
        {
            Debug.LogError("Scribe: ElevenLabs API key is not set!");
            yield break;
        }

        // 2. Check that file exists
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Scribe: Audio file not found at path: {filePath}");
            yield break;
        }

        // 3. Read file as bytes
        byte[] fileData = File.ReadAllBytes(filePath);

        // 4. Prepare the POST form (Unity will set the correct "Content-Type" header)
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "audio/wav");
        form.AddField("model_id", "scribe_v1");
        form.AddField("diarize", "true");
        form.AddField("tag_audio_events", "true");
        form.AddField("language_code", "eng");

        // 5. Create the request
        using (UnityWebRequest request = UnityWebRequest.Post("https://api.elevenlabs.io/v1/speech-to-text", form))
        {
            // ✅ Only set the API key! DO NOT manually set Content-Type!
            request.SetRequestHeader("xi-api-key", elevenLabsApiKey);

            // 6. Send the request
            yield return request.SendWebRequest();

            // 7. Check for errors
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Scribe: Error - {request.error}\nResponse: {request.downloadHandler.text}");
            }
            else
            {
                // 8. On success, parse JSON
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Scribe: Transcription Response:\n" + jsonResponse);

                ElevenLabsResponse response = JsonConvert.DeserializeObject<ElevenLabsResponse>(jsonResponse);
                if (response == null)
                {
                    Debug.LogError("Scribe: Failed to deserialize response.");
                }
                else
                {
                    // 9. Store the transcribed text
                    transcriptionText = response.text;
                    Debug.Log("Scribe: Transcribed Text: " + transcriptionText);

                    // Optionally, log word details
                    if (response.words != null)
                    {
                        foreach (var word in response.words)
                        {
                            Debug.Log($"Scribe: Word '{word.text}' " +
                                      $"({word.start:F2}s to {word.end:F2}s, Speaker: {word.speaker_id})");
                        }
                    }
                }
            }
        }
    }

    [System.Serializable]
    private class ElevenLabsResponse
    {
        public string text;
        public Word[] words;  // FIXED: Changed from tokens to words
    }

    [System.Serializable]
    private class Word
    {
        public string text;
        public string type;
        public float start;
        public float end;
        public string speaker_id;
    }
}
