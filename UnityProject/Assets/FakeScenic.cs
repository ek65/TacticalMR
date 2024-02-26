using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class FakeScenic : MonoBehaviour
{
    public ActionAPI playerActionAPI;
    public Vector3 dest;
    // Start is called before the first frame update
    void Start()
    {

    }
    

    // Update is called once per frame
    void Update()
    {
        playerActionAPI.MoveToPosMM(dest, false);
    }
}
