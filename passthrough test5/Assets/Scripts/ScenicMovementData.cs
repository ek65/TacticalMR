using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenicMovementData : MonoBehaviour
{
    public Model model;
    public Vector3 position;
    
    public string actionFunc;
    public List<object> actionArgs;

    // Prepare the ScenicMovementData using the data received from scenic
    public ScenicMovementData (Vector3 position, string modelType)
    {
        this.position = position;
        this.model = new Model(modelType);
    }

    public ScenicMovementData(Vector3 position, string modelType, string actionFunc, List<object> actionArgs)
    {
        this.position = position;
        this.model = new Model(modelType);
        this.actionFunc = actionFunc;
        this.actionArgs = actionArgs;
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