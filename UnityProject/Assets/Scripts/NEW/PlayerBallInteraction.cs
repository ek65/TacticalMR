using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBallInteraction : MonoBehaviour
{
    bool ReceivedBall = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SoccerBall"))
        {
            Debug.Log("Detected");
            other.gameObject.GetComponent<BallInteraction>().transformPlayer = gameObject.transform;
            other.gameObject.GetComponent<BallInteraction>().PlayerBallPosition = transform.Find("Ball Position").transform;
            other.gameObject.GetComponent<BallInteraction>().InRangeofPlayer = true;

            gameObject.GetComponent<ActionAPI>().ReceiveBall(gameObject, other.transform.position);

        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("SoccerBall"))
        {
            other.gameObject.GetComponent<BallInteraction>().transformPlayer = gameObject.transform;
            other.gameObject.GetComponent<BallInteraction>().PlayerBallPosition = transform.Find("Ball Position").transform;
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
