using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallOwnership : MonoBehaviour
{
    private ObjectsList objectList;
    public bool heldByScenic = false;
    public bool heldByHuman = false;
    
    // Start is called before the first frame update
    void Start()
    {
        GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
    }

    public void SetHumanOwnership(bool ownership)
    {
        heldByHuman = ownership;
    }
    
    public void SetScenicOwnership(bool ownership)
    {
        heldByScenic = ownership;
    }
    
}
