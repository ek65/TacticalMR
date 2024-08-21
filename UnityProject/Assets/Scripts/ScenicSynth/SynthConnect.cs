using System;
using SynthNetworkKit;
using UnityEngine;

public class SynthConnect : MonoBehaviour
{
    private SynthNetwork network;
    private JSONToLLM jsonToLLM;
    public string id; // name of the JSON file
    public int segmentNum = 1;
    
    // Initialize the necessary components and references at the start of the scene
    void Start()
    {
        network = GameObject.FindGameObjectWithTag("synth").GetComponent<SynthNetwork>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
    }
    
    // Sends the scene data and its explanation to Firebase Firestore, telling Firestore where to look for the scene in storage
    public void SendSceneAndExplanation()
    {
        Debug.Log($"{id} sent to firebase!");
        network.UploadTask("scene", id);
    }
    
    // Stores the scene data in Firebase Storage with a unique ID
    public void SendScene()
    {
        // id = Guid.NewGuid().ToString(); // Generate a new unique ID for the scene
        id = $"transcript4-segment{segmentNum}";  // (Commented out: Use a static ID if needed)
        segmentNum += 1;
        network.StoreScene(jsonToLLM.jsonString, id);
    }
    
    void Update()
    {
        
    }
}
