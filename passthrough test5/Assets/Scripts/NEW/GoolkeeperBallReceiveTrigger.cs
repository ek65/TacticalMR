using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoolkeeperBallReceiveTrigger : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SoccerBall"))
        {
            other.GetComponent<Rigidbody>().isKinematic = true;
            other.transform.position = transform.position;
            other.transform.parent = transform; 
            Debug.Log("Detect");
        }
    }
}
