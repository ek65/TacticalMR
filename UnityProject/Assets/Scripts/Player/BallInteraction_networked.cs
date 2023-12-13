using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class BallInteraction_networked : NetworkBehaviour
{
	public float kickForce; // = 20f;
	public float maxSpeed; // = 5f;
	public Vector2 xBoundary; // = new Vector2(-2f, 2f);
	public Vector2 yBoundary; // = new Vector2(0.2f, 10f);
	public Vector2 zBoundary; // = new Vector2(-5f, 5f);

	void Update()
	{
		var position = transform.position;
		position = new Vector3(
			Mathf.Clamp(position.x, xBoundary.x, xBoundary.y),
			Mathf.Clamp(position.y, yBoundary.x, yBoundary.y),
			Mathf.Clamp(position.z, zBoundary.x, zBoundary.y)
		);
		transform.position = position;

		// limit speed of ball
		var currentSpeed = GetComponent<Rigidbody>().velocity;
		if (currentSpeed.magnitude > maxSpeed)
		{
			GetComponent<Rigidbody>().velocity = currentSpeed.normalized * maxSpeed;
		}
	}

	public void OnTriggerEnter(Collider col)
	{
		var position = col.transform.position;
		Debug.Log("Ball collision detected with " + col.gameObject.name + " at " + position.ToString("F3"));
		// if (col.gameObject.GetComponent<NetworkObject>())
		// {
		var forceDir = transform.position - position;

		Debug.Log("Collision detected with " + col.gameObject.name + " at " + position.ToString("F3"));
		Debug.Log("Sending RPC_Kick_Ball with direction " + forceDir.ToString("F3") + " and magnitude " +
		          kickForce.ToString("F3"));

		RPC_Kick_Ball(forceDir, kickForce);
		// }
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	void RPC_Kick_Ball(Vector3 direction, float magnitude, RpcInfo info = default)
	{
		Debug.Log("RPC_Kick_Ball called from " + info.Source);
		if (info.IsInvokeLocal) return; // don't execute on the client that called the RPC

		Debug.Log("Kicking ball in direction " + direction.ToString("F3"));
		this.gameObject.GetComponent<Rigidbody>().AddForce(direction.normalized * magnitude, ForceMode.Impulse);
	}
}
