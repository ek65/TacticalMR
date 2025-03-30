using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class BallOwnership : NetworkBehaviour
{
    public NetworkBool heldByScenic = false;
    public NetworkBool heldByHuman = false;
    public GameObject ballOwner = null;
    
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
    
    public void SetBallOwner(GameObject owner)
    {
        ballOwner = owner;
    }
    
}
