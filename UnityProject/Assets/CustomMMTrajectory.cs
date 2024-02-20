using System.Collections;
using System.Collections.Generic;
using MxM;
using UnityEngine;

public class CustomMMTrajectory : MxMTrajectoryGeneratorBase
{
    public override bool HasMovementInput()
    {
        return true;
    }

    protected override void Setup(float[] a_predictionTimes)
    {
            
    }

    protected override void UpdatePrediction(float a_deltaTime)
    {
        for (int i = 0; i < 20; ++i)
        {
            p_trajPositions[i] = Vector3.one*5*i;
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
