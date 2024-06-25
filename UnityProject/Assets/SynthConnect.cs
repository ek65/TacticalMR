using System;
using System.Collections;
using System.Collections.Generic;
using OpenAI.Samples.Chat;
using SynthNetworkKit;
using UnityEngine;

public class SynthConnect : MonoBehaviour
{
    private ChatBehaviour chatBehaviour;
    private SynthNetwork network;
    
    // Start is called before the first frame update
    void Start()
    {
        chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponent<ChatBehaviour>();
        network = GameObject.FindGameObjectWithTag("synth").GetComponent<SynthNetwork>();
    }

    public void SendExplanation(string explanation)
    {
        explanation = chatBehaviour.userInput;
        // network.UploadTask("language",explanation);
        Debug.Log($"{chatBehaviour.userInput} sent to firebase!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}