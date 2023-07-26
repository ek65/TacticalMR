using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBallInteraction : MonoBehaviour
{
    [SerializeField] Transform playerBallPos;
    bool ReceivedBall = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ball"))
        {
            Debug.Log("Detected");
            other.gameObject.GetComponent<BallInteraction>().transformPlayer = gameObject.transform;
            other.gameObject.GetComponent<BallInteraction>().PlayerBallPosition = playerBallPos;
            other.gameObject.GetComponent<BallInteraction>().InRangeofPlayer = true;

            gameObject.GetComponentInParent<ActionAPI>().ReceiveBall(other.transform.position);

        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("ball"))
        {
            other.gameObject.GetComponent<BallInteraction>().transformPlayer = gameObject.transform;
            other.gameObject.GetComponent<BallInteraction>().PlayerBallPosition = playerBallPos;
            other.gameObject.GetComponent<BallInteraction>().InRangeofPlayer = true;

/*
            if (other.gameObject.GetComponent<BallInteraction>().StickToPlayer && !ReceivedBall)
            {
                gameObject.GetComponent<ActionAPI>().ReceiveBall(gameObject, other.transform.position);
                ReceivedBall = true;
            }
*/
        }
    }

}
