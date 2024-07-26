using System;
using System.Collections;
using System.Collections.Generic;
using OpenAI.Samples.Chat;
using SynthNetworkKit;
using UnityEngine;
// 
public class SynthConnect : MonoBehaviour
{
    private ChatBehaviour chatBehaviour;
    private SynthNetwork network;
    private JSONToLLM jsonToLLM;
    public string id;
    
    // Start is called before the first frame update
    void Start()
    {
        chatBehaviour = GameObject.FindGameObjectWithTag("Character").GetComponent<ChatBehaviour>();
        network = GameObject.FindGameObjectWithTag("synth").GetComponent<SynthNetwork>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
    }

    // public void SendExplanation(string explanation)
    // {
    //     Debug.Log($"{explanation} sent to firebase!");
    //     network.UploadTask("language",explanation);
    // }
    
    public void SendSceneAndExplanation()
    {
        Debug.Log($"{id} sent to firebase!");
        network.UploadTask("scene",id);
    }
    
    public void SendScene()
    {
        id = Guid.NewGuid().ToString();
        network.StoreScene(jsonToLLM.jsonString, id);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}