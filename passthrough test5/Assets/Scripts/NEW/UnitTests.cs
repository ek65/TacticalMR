using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitTests : MonoBehaviour
{
    public GameObject apiManager;
    ActionAPI scriptReference;
    public Transform goalPost;
    // Start is called before the first frame update
    void Start()
    {
        ActionAPI scriptReference = apiManager.GetComponent<ActionAPI>();

        scriptReference.ShootBallOnly(goalPost.position, "center-middle", "low");
    }

    
}
