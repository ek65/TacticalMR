using System;
using System.Collections.Generic;
using Fusion;
using Oculus.Movement.Effects;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

public class Player : NetworkBehaviour
{
	public Transform _head;
	public Transform _body;

	public Transform origHip;
	public Transform mirrorHip;

	[System.Serializable]
	public class MirroredTransformPair
	{
		/// <summary>
		/// The name of the mirrored transform pair.
		/// </summary>
		[HideInInspector] public string Name;

		/// <summary>
		/// The original transform.
		/// </summary>
		public Transform OriginalTransform;

		/// <summary>
		/// The mirrored transform.
		/// </summary>
		public Transform MirroredTransform;
	}
	
	/// <summary>
	/// Returns the original transform.
	/// </summary>
	public Transform OriginalTransform => _transformToCopy;

	/// <summary>
	/// Returns the mirrored transform.
	/// </summary>
	public Transform MirroredTransform => _myTransform;

	/// <summary>
	/// The transform which transform values are being mirrored from.
	/// </summary>
	[SerializeField]
	public Transform _transformToCopy;

	/// <summary>
	/// The target transform which transform values are being mirrored to.
	/// </summary>
	[SerializeField]
	public Transform _myTransform;

	/// <summary>
	/// The array of mirrored transform pairs.
	/// </summary>
	[SerializeField]
	public MirroredTransformPair[] _mirroredTransformPairs;

	public List<Transform> _mirroredTransformList;
	
	private void Awake()
	{
		_head = FindObjectOfType<GameManager>()._HeadTransform;
		origHip = FindObjectOfType<GameManager>().hipTransform;
		
		_transformToCopy = FindObjectOfType<GameManager>()._OriginalTransform;
		
		PopulateMirror();
            
		Assert.IsNotNull(_transformToCopy);
		Assert.IsNotNull(_myTransform);
		foreach (var mirroredTransformPair in _mirroredTransformPairs)
		{
			Assert.IsNotNull(mirroredTransformPair.OriginalTransform);
			Assert.IsNotNull(mirroredTransformPair.MirroredTransform);
		}
	}

	private void Start()
	{
		if (_transformToCopy == null)
		{
			_transformToCopy = FindObjectOfType<GameManager>()._OriginalTransform;
		}
	}
	
	public void Update()
	{
		if (_transformToCopy == null)
		{
			_transformToCopy = FindObjectOfType<GameManager>()._OriginalTransform;
		}
		// mirrorHip.position = origHip.position;
		// mirrorHip.rotation = origHip.rotation;
		// Debug.LogError(mirrorHip.rotation);
		// Debug.LogError(origHip.rotation);
		
		if (HasInputAuthority) // only the client that owns the player can send input
		{
			// var position = _head.position;
			// var rotation = _head.rotation;
			var position = origHip.localPosition;
			var rotation = origHip.localRotation;
			Debug.Log("Has input authority, sending RPC_Sync_Headset_Transform for " + position.ToString("F3") +
			          " to all clients");
			// RPC_Sync_Transform(position, rotation);
			// RPC_Sync_Transform();
		}
	}
	
	private void PopulateMirror()
	{
		var childTransforms = new List<Transform>(_myTransform.GetComponentsInChildren<Transform>(true));
		childTransforms.Remove(_myTransform.transform); // removes the parent transform
		_mirroredTransformList = childTransforms;
		_mirroredTransformPairs = new MirroredTransformPair[childTransforms.Count];
		int i = 0;
		foreach (var transform in childTransforms)
		{
			var originalTransform =
				this.OriginalTransform.transform.FindChildRecursive(transform.name);
			if (originalTransform != null)
			{
				MirroredTransformPair pair = new MirroredTransformPair();
				pair.OriginalTransform = originalTransform;
				pair.MirroredTransform = transform;
				_mirroredTransformPairs[i] = pair;
			}
			else
			{
				Debug.LogError($"Missing a mirrored transform for: {transform.name}");
			}
			i++;
		}
	}

	public void LateUpdate()
	{
		// _myTransform.localPosition = _transformToCopy.localPosition;
		// _myTransform.localRotation = _transformToCopy.localRotation;
		// // Debug.LogError(_myTransform.localRotation);
		// // Debug.LogError(_transformToCopy.localRotation);
		// foreach (var transformPair in _mirroredTransformPairs)
		// {
		// 	transformPair.MirroredTransform.localPosition = transformPair.OriginalTransform.localPosition;
		// 	transformPair.MirroredTransform.localRotation = transformPair.OriginalTransform.localRotation;
		// }
		
		if (HasInputAuthority) // only the client that owns the player can send input
		{
			_myTransform.localPosition = _transformToCopy.localPosition;
			_myTransform.localRotation = _transformToCopy.localRotation;
			
			foreach (var transformPair in _mirroredTransformPairs)
			{
				var pos = transformPair.OriginalTransform.localPosition;
				var rot = transformPair.OriginalTransform.localRotation;
				var name = transformPair.OriginalTransform.gameObject.name;
				RPC_Mirror(name, pos, rot);
			}
		}
	}
	
	
	[Rpc(RpcSources.InputAuthority, RpcTargets.All)] // the RPC is called by a client, and is executed on all clients
	public void RPC_Mirror(String name, Vector3 position, Quaternion rotation, RpcInfo info = default)
	{
		Debug.Log("RPC_Mirror called from " + info.Source);

		foreach (var mirroredTransform in _mirroredTransformList)
		{
			if (mirroredTransform.name == name)
			{
				mirroredTransform.localPosition = position;
				mirroredTransform.localRotation = rotation;
			}

		}
		
		
		// _myTransform.localPosition = _transformToCopy.localPosition;
		// _myTransform.localRotation = _transformToCopy.localRotation;
		// foreach (var transformPair in _mirroredTransformPairs)
		// {
		// 	transformPair.MirroredTransform.localPosition = transformPair.OriginalTransform.localPosition;
		// 	transformPair.MirroredTransform.localRotation = transformPair.OriginalTransform.localRotation;
		// 	
		// 	if (transformPair.OriginalTransform.gameObject.name == "Hips")
		// 	{
		// 		Debug.LogError("hip original rotation in RPC call: " + transformPair.OriginalTransform.localRotation);
		// 		Debug.LogError("hip mirrored rotation in RPC call: " + transformPair.MirroredTransform.localRotation);
		// 	}
		// }
	}

	[Rpc(RpcSources.InputAuthority, RpcTargets.All)] // the RPC is called by a client, and is executed on all clients
	public void RPC_Sync_Transform(Vector3 position, Quaternion rotation, RpcInfo info = default)
	{
		Debug.Log("RPC_Sync_Headset_Transform called from " + info.Source);
		// if (info.IsInvokeLocal) return; // don't execute on the client that called the RPC

		Debug.Log("setting " + info.Source + " headset transform to " + position.ToString("F3"));
		// _body.position = position;
		// _body.rotation = rotation;
		mirrorHip.localPosition = position;
		mirrorHip.localRotation = rotation;

		// MirrorObject mirrorObject = GetComponentInChildren<MirrorObject>();
		// foreach (var mirroredTransform in mirrorObject._mirroredTransformList)
		// {
		// 	mirroredTransform.position = mirroredTransform.position;
		// 	mirroredTransform.rotation = mirroredTransform.rotation;
		// }
	}
}