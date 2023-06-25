using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallInteraction : MonoBehaviour
{
    public bool StickToPlayer = false;
    [SerializeField] internal Transform transformPlayer;
    public Transform PlayerBallPosition;
    
    internal bool InRangeofPlayer = false;

    float Rotationspeed;
    Vector3 previousLocation;
    
    public static BallInteraction instance;

    private void Awake()
    {
        instance = this;
    }

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

        if(StickToPlayer)
        {
            Vector2 currentLocation = new Vector2(transform.position.x, transform.position.z);
            Rotationspeed = Vector2.Distance(currentLocation, previousLocation) / Time.deltaTime;
            transform.position = PlayerBallPosition.position;
            transform.Rotate(new Vector3(transformPlayer.right.x, 0, transformPlayer.right.z), Rotationspeed, Space.World);
            previousLocation = currentLocation;
        }
    }
}
