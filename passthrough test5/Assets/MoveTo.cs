using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTo : MonoBehaviour
{
    public Transform[] targets;
    public float time = 1;
    public Vector3 velocity = Vector3.zero;

    private int nextPosIndex;

    private Transform nextPos;
    // Start is called before the first frame update
    void Start()
    {
        nextPos = targets[0];
    }

    // Update is called once per frame
    
    void Update()
    {
        Move();

    }

    void Move()
    {
        if (transform.position == nextPos.position)
        {
            nextPosIndex++;
            if (nextPosIndex >= targets.Length)
            {
                nextPosIndex--;
            }

            nextPos = targets[nextPosIndex];
        }
        else
        {
            transform.LookAt(nextPos);
            transform.position =
                Vector3.MoveTowards(transform.position, nextPos.position, time * Time.deltaTime);
        }
      
    }
}
