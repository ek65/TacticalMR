using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    private bool isHost;

    public GameObject ZMQManagerObject;

    void Start()
    {
        // print the current runtime type
        Debug.Log("Runtime type: " + Application.platform);
        
        // Detect if we're the host
        isHost = false;
#if UNITY_EDITOR
        isHost = true;
#endif
        Debug.Log("We are the " + (isHost ? "host" : "client"));

        if (isHost)
        {
            // enable `ZMQManager` to listen to Scenic
            ZMQManagerObject.SetActive(true);
        }
        else
        {
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}