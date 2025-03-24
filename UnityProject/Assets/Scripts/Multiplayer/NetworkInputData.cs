using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
	public Vector3 Direction; // player movement direction
	public bool TryIntercept; // player tries to intercept the ball
}
