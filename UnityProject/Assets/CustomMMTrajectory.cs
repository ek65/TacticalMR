using System.Collections;
using System.Collections.Generic;
using MxM;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class CustomMMTrajectory : MxMTrajectoryGeneratorBase
{
    private NativeArray<float3> m_newTrajPositions;
    public Vector3 m_velocity;

    public override bool HasMovementInput()
    {
        return true;
    }

    protected override void Setup(float[] a_predictionTimes)
    {
            
    }
    protected override void InitializeNativeData()
    {
        base.InitializeNativeData();

        m_newTrajPositions = new NativeArray<float3>(p_trajectoryIterations,
            Allocator.Persistent, NativeArrayOptions.ClearMemory);
    }


    protected override void UpdatePrediction(float a_deltaTime)
    {
        if (p_trajPositions.Length == 0 || p_trajFacingAngles.Length == 0)
            return;

        Vector3 desiredLinearVelocity = m_velocity;

        desiredLinearVelocity = desiredLinearVelocity.normalized;

        //Calculate the desired linear displacement over a single iteration
        Vector3 desiredLinearDisplacement = desiredLinearVelocity / p_sampleRate;
        float desiredOrientation = Mathf.Atan2(desiredLinearDisplacement.x,
                desiredLinearDisplacement.z) * Mathf.Rad2Deg;
        Debug.Log(desiredLinearDisplacement); ;
        p_trajPositions[0] = float3.zero;
        for (int i = 1; i < p_trajPositions.Length; i++)
        {
            p_trajPositions[i] = p_trajPositions[i-1] + (float3)desiredLinearDisplacement*10;
        }

        /*
        var trajectoryGenerateJob = new TrajectoryGeneratorJob()
        {
            TrajectoryPositions = p_trajPositions,
            TrajectoryRotations = p_trajFacingAngles,
            NewTrajectoryPositions = m_newTrajPositions,
            DesiredLinearDisplacement = desiredLinearDisplacement,
            DesiredOrientation = desiredOrientation,
            MoveRate = 20 * a_deltaTime,
            TurnRate = 2* a_deltaTime
        };

        p_trajectoryGenerateJobHandle = trajectoryGenerateJob.Schedule();*/
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePrediction(Time.deltaTime);
        Debug.Log(p_trajPositions.Length);
        string positions = "Positions:";
        for(int i = 0; i < p_trajPositions.Length; i++)
        {
            positions += ", "+ p_trajPositions[i];
        }
        Debug.Log(positions);
    }
}
