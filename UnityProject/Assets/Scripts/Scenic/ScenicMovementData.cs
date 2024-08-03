using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenicMovementData : MonoBehaviour
{
    public Model model;
    public Vector3 position;

    public string actionFunc;
    public List<object> actionArgs;
    public string behavior;

    public bool stopButton;

    public bool pause;
    // Prepare the ScenicMovementData using the data received from scenic
    public ScenicMovementData(Vector3 position, string modelType, string behavior, bool pause)
    {
        this.position = position;
        this.model = new Model(modelType);
        this.behavior = behavior;

        this.pause = pause;
    }


    public ScenicMovementData(Vector3 position, string modelType, string behavior, string actionFunc, List<object> actionArgs, bool pause)
    {
        this.position = position;
        this.model = new Model(modelType);
        this.behavior = behavior;
        this.actionFunc = actionFunc;
        this.actionArgs = actionArgs;

        this.pause = pause;
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