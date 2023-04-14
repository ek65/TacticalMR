using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoccerBall : MonoBehaviour
{
    public float Force;
        void OnMouseDown(){
            GetComponent<Rigidbody>().AddForce(Vector3.forward * Force, ForceMode.Impulse);
        }
}
