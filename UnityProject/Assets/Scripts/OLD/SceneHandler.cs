using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
DestinationZones { empty, left-top, left-middle, left-bottom, center-top, center-middle, center-bottom, right-top, right-middle, right-bottom };
BallProjectileHeights {grounded, low, medium, high}
 */

public class SceneHandler : MonoBehaviour
{
    public GameObject apiManager;
    public GameObject goalPost;
    public GameObject playerOne;
    // public GameObject playerTwo;
    // public GameObject playerThree;
    // public GameObject playerFour;
    public GameObject goalKeeper;
    public GameObject soccerBall;
    public GameObject finalPositionPlaceholder;


    // For Testing 
    public List<Transform> Destinations;

    // AI
    NavMeshAgent agent;

    void Start()
    {
        
        // Remove this line (this line is sample for showing the working of movmement api)
        //StartCoroutine (DestinationCalling(actionAPIs));
        // playerOne.GetComponent<ActionAPI>().MoveToPos(Destinations[0].position);
        playerOne.GetComponent<ActionAPI>().DribbleFromOnePositionToAnother(goalPost.transform.position);
        // playerOne.GetComponent<ActionAPI>().BallHeaderShoot(goalPost.transform.position, "center-middle");
        
        // This line is responsible for calling Movement
        //actionAPIs.MoveFromOnePositionToAnother (playerOne, Destinations[i].position);
        
        // example for calling the APIs
        //actionAPIs. Shoot(playerOne, goalPost.transform.position, "center-middle"); // actionAPIs. AirPass(playerOne, finalPositionPlaceholder.transform.position, "low");

    }

    private void Update()
    {
        //playerOne.GetComponent<ActionAPI>().MoveToPos(Destinations[0].position);
        
        // playerTwo.GetComponent<ActionAPI>().MoveToPos(Destinations[1].position);
        // playerThree.GetComponent<ActionAPI>().MoveToPos(Destinations[1].position);
        // playerFour.GetComponent<ActionAPI>().MoveToPos(Destinations[1].position);
    }

    // For Testing New Movement Feature 
    IEnumerator DestinationCalling(ActionAPI actionAPIs)
    {
        for(int i = 0; i < Destinations.Capacity; i++)
        {
            actionAPIs.MoveToPosMM(Destinations[i].position);
            // here we wait for 3 sec before update destination for self Player
            yield return new WaitForSeconds(3f);
            Debug.Log(i + " : ) ");
        }
    }

}
