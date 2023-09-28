using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallInteraction : MonoBehaviour
{
    public bool StickToPlayer = false;
    public Transform transformPlayer;
    public Transform PlayerBallPosition;
    
    public bool InRangeofPlayer = false;

    float Rotationspeed;
    Vector3 previousLocation;


    // Update is called once per frame
    void Update()
    {
        if (InRangeofPlayer)
        {
            float distanceToPlayer = Vector3.Distance(transformPlayer.position, transform.position);
            if(distanceToPlayer < 0.5)
            {
                StickToPlayer = true;
            }
        }
        else
        {
            StickToPlayer = false;
        }

        if(StickToPlayer)
        {
            Vector2 currentLocation = new(transform.position.x, transform.position.z);
            // Rotationspeed = Vector2.Distance(currentLocation, previousLocation) / Time.deltaTime;
            transform.position = PlayerBallPosition.position;
            this.gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            // transform.Rotate(new Vector3(transformPlayer.right.x, 0, transformPlayer.right.z), Rotationspeed, Space.World);
            // previousLocation = currentLocation;

            transformPlayer.parent.GetComponent<PlayerInterface>().ballPossession = true;
        }
        else
        {
            transformPlayer.parent.GetComponent<PlayerInterface>().ballPossession = false;
        }
    }
}
