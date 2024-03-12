using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;

public class AIInterface : MonoBehaviour
{
    private int localTick;  // NOTE: This is not the true tick and is what we will use to internally record a timestep.

    public ActionAPI actionAPI;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void ApplyMovement(ScenicMovementData data)
    {
        localTick += 1;
        // Debug.Log(GetComponent<Rigidbody>().velocity);
        if (localTick < 4)
        {
            return;
        }

        
        if (data.actionFunc != null)
        {
            Type type = actionAPI.GetType();
            MethodInfo method = type.GetMethod(data.actionFunc);
            
            Debug.LogError("im in here");

            method.Invoke(actionAPI, data.actionArgs.ToArray());
        }
        else //idle
        {
            Debug.LogError("im in here2");
            actionAPI.stopMovement = true;
        }
    }
}
