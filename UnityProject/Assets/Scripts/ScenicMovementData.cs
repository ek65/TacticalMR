using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenicMovementData : MonoBehaviour
{
    public Model model;
    public Vector3 position;
    
    public string actionFunc;
    public List<object> actionArgs;
    
    public bool stopButton;

    // Prepare the ScenicMovementData using the data received from scenic
    public ScenicMovementData (Vector3 position, string modelType, bool stopButton)
    {
        this.position = position;
        this.model = new Model(modelType);
        
        this.stopButton = stopButton;
    }

    public ScenicMovementData(Vector3 position, string modelType, string actionFunc, List<object> actionArgs, bool stopButton)
    {
        this.position = position;
        this.model = new Model(modelType);
        this.actionFunc = actionFunc;
        this.actionArgs = actionArgs;
        
        this.stopButton = stopButton;
    }
}



public class Model
{
    // public float red;
    // public float green;
    // public float blue;
    // public float opacity;
    public string modelType;
    public Model(string modelType)
    {
        // this.red = red;
        // this.green = green;
        // this.blue = blue;
        // this.opacity = opacity;
        this.modelType = modelType;
    }
}