using System.Collections;
using System.Collections.Generic;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class OpenAIManager : MonoBehaviour
{
    ZMQOpenAIClient client;
    // Start is called before the first frame update
    void Start()
    {
        client = new ZMQOpenAIClient("just testing");
    }
    public void SubmitContext()
    {

    }
    public async void FormatInput(string speech, string sceneInfoJson)
    {
     string response =  await client.SendMessageAsync(speech + ". And below is scene info: " + sceneInfoJson);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


public class ZMQOpenAIClient
{
    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly string endpoint;

    public ZMQOpenAIClient(string apiKey)
    {
        this.apiKey = apiKey;
        this.endpoint = "https://api.openai.com/v1/completions";
        this.client = new HttpClient();
        InitializeClient();
    }

    private void InitializeClient()
    {

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> SendMessageAsync(string message, string engineId = "text-davinci-003", double temperature = 0.5, int maxTokens = 100)
    {
        try
        {
            var requestBody = new
            {
                model = engineId,
                prompt = message,
                temperature = temperature,
                max_tokens = maxTokens,
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();
            return result;
        }
        catch (HttpRequestException e)
        {
            throw new Exception("An error occurred while sending the request to OpenAI: " + e.Message, e);
        }
    }
}