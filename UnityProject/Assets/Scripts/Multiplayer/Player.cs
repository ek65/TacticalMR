using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
	public Transform _head;
	public Transform _body;

	private void Awake()
	{
		_head = FindObjectOfType<GameManager>()._HeadTransform;
	}

	public void Update()
	{
		if (HasInputAuthority) // only the client that owns the player can send input
		{
			var position = _head.position;
			var rotation = _head.rotation;
			Debug.Log("Has input authority, sending RPC_Sync_Headset_Transform for " + position.ToString("F3") +
			          " to all clients");
			RPC_Sync_Headset_Transform(position, rotation);
		}
	}

	[Rpc(RpcSources.InputAuthority, RpcTargets.All)] // the RPC is called by a client, and is executed on all clients
	public void RPC_Sync_Headset_Transform(Vector3 transform, Quaternion rotation, RpcInfo info = default)
	{
		Debug.Log("RPC_Sync_Headset_Transform called from " + info.Source);
		// if (info.IsInvokeLocal) return; // don't execute on the client that called the RPC

		Debug.Log("setting " + info.Source + " headset transform to " + transform.ToString("F3"));
		_body.position = transform;
		_body.rotation = rotation;
	}
}