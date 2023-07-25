using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoolkeeperBallReceiveTrigger : MonoBehaviour
{
    [SerializeField] GameObject LeftHand;
    [SerializeField] GameObject RightHand;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ball"))
        {
            other.GetComponent<Rigidbody>().isKinematic = true;

            Vector3 Left = LeftHand.transform.position;
            Vector3 Right = RightHand.transform.position;

            other.transform.position = new Vector3((Left.x + Right.x) / 2f, (Left.y + Right.y) / 2f - 0.05f, (Left.z + Right.z) / 2f);
            other.transform.parent = transform; 
            Debug.Log("Detect");

            var Keeper = gameObject.GetComponentInParent<ActionAPI>().gameObject;

            FindAnyObjectByType<ActionAPI>().IdleWithBallInHand();
        }
    }

}
