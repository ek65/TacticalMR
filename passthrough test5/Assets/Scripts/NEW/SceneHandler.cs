using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
DestinationZones { empty, left-top, left-middle, left-bottom, center-top, center-middle, center-bottom, right-top, right-middle, right-bottom };
BallProjectileHeights {low, medium, high}
 */

public class SceneHandler : MonoBehaviour
{
    public GameObject apiManager;
    public GameObject goalPost;
    public GameObject playerOne;
    public GameObject goalKeeper;
    public GameObject soccerBall;

    ActionAPI actionAPIs;

    void Start()
    {
        actionAPIs = apiManager.GetComponent<ActionAPI>();

        // example for calling the APIs
        actionAPIs.Shoot(playerOne, goalPost.transform.position, "left-bottom");
    }

    
}
