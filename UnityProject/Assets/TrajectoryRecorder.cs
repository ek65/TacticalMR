using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryRecorder : MonoBehaviour
{
    [SerializeField] List<GameObject> ToRecord;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach(GameObject obj in ToRecord)
        {
         
        }
    }
    private void FixedUpdate()
    {
        foreach(GameObject obj in ToRecord)
        {

        }
    }
}

public class TimeSeries
{

}