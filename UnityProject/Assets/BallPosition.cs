using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class BallPosition : MonoBehaviour
{
    public Transform vrCamera; 
    public float forwardDistance = 0.34f;
    public float fixedY = 0.18f;
    
    private GameManager gm;

    private void Start()
    {
        vrCamera = GameObject.Find("CenterEyeAnchor").transform;
        gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    void Update()
    {
        if (vrCamera == null || !gm.isHost)
        {
            return;
        }

        // Calculate target position
        Vector3 forwardPosition = vrCamera.position + vrCamera.forward * forwardDistance;
        
        // Set y to fixed value
        forwardPosition.y = fixedY;

        // Apply to this GameObject
        transform.position = forwardPosition;
    }
}
