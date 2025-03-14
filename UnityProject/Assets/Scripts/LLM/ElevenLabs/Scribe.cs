using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using Newtonsoft.Json;

// Delegate + event for "transcription complete"
public delegate void TranscriptionCompleteHandler(Scribe.ElevenLabsResponse response);

public class Scribe : MonoBehaviour
{
    [Header("ElevenLabs Settings")]
    [Tooltip("Enter your ElevenLabs API Key here.")]
    public string elevenLabsApiKey = "YOUR_ELEVENLABS_API_KEY";

    [Header("Debug / Output")]
    [Tooltip("Will store the last transcribed text.")]
    public string transcriptionText;
    public event TranscriptionCompleteHandler OnTranscriptionComplete;

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
        if (string.IsNullOrEmpty(elevenLabsApiKey))
        {
            Debug.LogError("Scribe: ElevenLabs API key is not set!");
            yield break;
        }
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Scribe: Audio file not found at path: {filePath}");
            yield break;
        }
        byte[] fileData = File.ReadAllBytes(filePath);
        
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "audio/wav");
        form.AddField("model_id", "scribe_v1");
        form.AddField("diarize", "true");
        form.AddField("tag_audio_events", "true");
        form.AddField("language_code", "eng");
        
        using (UnityWebRequest request = UnityWebRequest.Post("https://api.elevenlabs.io/v1/speech-to-text", form))
        {
            request.SetRequestHeader("xi-api-key", elevenLabsApiKey);
            
            yield return request.SendWebRequest();

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

                ElevenLabsResponse response =
                    JsonConvert.DeserializeObject<ElevenLabsResponse>(jsonResponse);
                if (response == null)
                {
                    Debug.LogError("Scribe: Failed to deserialize response.");
                }
                else
                {

                    transcriptionText = response.text;
                    Debug.Log("Scribe: Transcribed Text: " + transcriptionText);
                    OnTranscriptionComplete?.Invoke(response);
                }
            }
        }
    }
    
    [System.Serializable]
    public class ElevenLabsResponse
    {
        public string text;
        public Word[] words;
    }

    [System.Serializable]
    public class Word
    {
        public string text;
        public float start;
        public float end;
        public string type;
        public string speaker_id;
    }
}
