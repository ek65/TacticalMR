using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenicMovementData : MonoBehaviour
{
    public Model model;
    public Vector3 position;
    
    public bool doMove;
    public Vector3 moveToPosition;

    // public bool doKick;
    // public Vector3 kickPosition;

    // Prepare the ScenicMovementData using the data received from scenic
    public ScenicMovementData (Dictionary<string, int> intVals, Dictionary<string, bool> boolVals, Dictionary<string, float> floatVals, Dictionary<string, Vector3> vectorVals, Dictionary<string, Quaternion> quaternionVals, Dictionary<string, string> stringVals, Dictionary<string, List<string>> listVals, Dictionary<string, List<Vector3>> listVectorVals)
    {
        this.position = vectorVals["Position"];
        this.doMove = boolVals["DoMove"];
        this.moveToPosition = vectorVals["MoveToPosition"];
        this.model = new Model(stringVals["ModelType"]);

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