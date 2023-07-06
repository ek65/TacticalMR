using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenicMovementData : MonoBehaviour
{
    public Model model;
    public Vector3 position;
    
    public bool doMove;
    public Vector3 moveToPosition;

    // public Dictionary<string, List<IActionDictType>> actionDict;
    public string action;
    public List<IActionDictType> parameters;

    // public bool doKick;
    // public Vector3 kickPosition;

    // Prepare the ScenicMovementData using the data received from scenic
    public ScenicMovementData (Dictionary<string, bool> boolVals, Dictionary<string, Vector3> vectorVals, Dictionary<string, string> stringVals, Dictionary<string, List<IActionDictType>> actionDict)
    {
        this.position = vectorVals["Position"];
        this.doMove = boolVals["DoMove"];
        this.moveToPosition = vectorVals["MoveToPosition"];
        this.model = new Model(stringVals["ModelType"]);
        Debug.Log("here");
        Debug.Log(actionDict.Keys);
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