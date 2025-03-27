using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks
{
	#region Generic

	[Tooltip("Automatically decide host. Host should be headset, client should be laptop.")]
	public bool autoIsHost;

	[Tooltip("If this is checked, then the game will start as a host.")]
	public bool isHost;

	public GameObject _ObserverCamera;

	// [Tooltip("The sole OVRCameraRig in the scene. Multiple OVRCameraRigs will cause problems.")]
	// public GameObject _OVRCR;
	//
	// public Transform _OriginalTransform;
	//
	// public Transform _ParentTransform;
	//
	// public GameObject ball;

	void Start()
	{
		// print the current runtime type
		Debug.Log("Runtime type: " + UnityEngine.Application.platform);

		if (autoIsHost)
		{
			isHost = true;
			// if we're running in the Unity Editor
			#if UNITY_EDITOR
				isHost = false;
			#endif
		}

		Debug.Log("We are the " + (isHost ? "host" : "client"));

		if (isHost) // host
		{
			// enable `ZMQManager` to listen to Scenic
			ZMQManagerObject.SetActive(true);
			
			// disable the Observer Camera
			_ObserverCamera.SetActive(false);

			// make a photon fusion room
			if (_runner == null)
			{
				StartGame(GameMode.Host);
			}
		}
		else // client
		{
			// switch to the Observer Camera
			_ObserverCamera.SetActive(true);
			
			// Connect to photon fusion room
			if (_runner == null)
			{
				StartGame(GameMode.Client);
			}
		}
	}

	private void Update()
	{
		if (isHost && Input.GetKeyDown(KeyCode.R))
		{
			// ball.transform.position = new Vector3(0, 1, 1);
		}
	}

	#endregion

	#region Networking with Photon Fusion

	[HideInInspector]
	public NetworkRunner _runner;


	async void StartGame(GameMode mode)
	{
		// Create the Fusion runner and let it know that we will be providing user input
		Debug.Log("in StartGame");
		_runner = gameObject.AddComponent<NetworkRunner>();
		_runner.ProvideInput = isHost; // server does provide input
		Debug.Log("we provide input: " + isHost);

		// Start or join (depends on gamemode) a session with a specific name
		await _runner.StartGame(new StartGameArgs()
		{
			GameMode = mode,
			SessionName = "GameRoom",
			Scene = SceneManager.GetActiveScene().buildIndex,
			SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
		});
	}

	[SerializeField] private NetworkPrefabRef _playerPrefab;
	private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
		// // The user's prefab has to be spawned by host
		// if (runner.IsServer)
		// {
		// 	Debug.Log($"OnPlayerJoined. PlayerId: {player.PlayerId}");
		// 	// Create a unique position for the player
		// 	Vector3 spawnPosition =
		// 		new Vector3((player.RawEncoded % runner.Config.Simulation.DefaultPlayers) * 3, 1, 5);
		// 	// Vector3 spawnPosition =
		// 	// 	new Vector3(0,0,0);
		// 	// We make sure to give the input authority to the connecting player for their user's object
		// 	NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
		// 	// Keep track of the player avatars so we can remove it when they disconnect
		// 	_spawnedCharacters.Add(player, networkPlayerObject);
		// }

		if (runner.IsServer)
		{
			Debug.Log($"Player joined: {player.PlayerId}");
		
			// Spawn only the host’s network representation
			if (player == runner.LocalPlayer && isHost)
			{
				Vector3 spawnPosition = new Vector3(0, 0, 0);
				NetworkObject hostNetworkPlayer = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
				
				// Hide host's own networked player mesh locally
				// // hostNetworkPlayer.gameObject.SetActive(false);
				// Transform hostTransform = hostNetworkPlayer.gameObject.transform;
				// Transform childWithTag = null;
				// for (int i = 0; i < hostTransform.childCount; i++)
				// {
				// 	if (hostTransform.GetChild(i).CompareTag("NetworkedPlayerMesh"))
				// 	{
				// 		childWithTag = hostTransform.GetChild(i);
				// 		break;
				// 	}
				// }
				//
				// if (childWithTag != null)
				// {
				// 	childWithTag.gameObject.SetActive(false);
				// }
				
				
				_spawnedCharacters.Add(player, hostNetworkPlayer);
			}
			else if (player != runner.LocalPlayer)
			{
				// Clients are not allowed their own avatars, only viewing host
				Debug.Log("Client joined, no local avatar spawned, only observer mode active.");
			}
		}
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		// Find and remove the players avatar (only host would have stored the spawned game object)
		if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
		{
			runner.Despawn(networkObject);
			_spawnedCharacters.Remove(player);
		}
	}

	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
		// var data = new NetworkInputData();
		//
		// // player movement
		// if (Input.GetKey(KeyCode.W))
		// 	data.Direction += Vector3.forward;
		//
		// if (Input.GetKey(KeyCode.S))
		// 	data.Direction += Vector3.back;
		//
		// if (Input.GetKey(KeyCode.A))
		// 	data.Direction += Vector3.left;
		//
		// if (Input.GetKey(KeyCode.D))
		// 	data.Direction += Vector3.right;
		//
		// // player tries to intercept the ball
		// data.TryIntercept = Input.GetKey(KeyCode.I);
		//
		// input.Set(data);
		return;
	}

	#endregion

	#region Unused INetworkRunnerCallbacks with debug logs


	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
	{
		return;
	}

	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) => Debug.Log("Shutdown: " + shutdownReason);

	public void OnConnectedToServer(NetworkRunner runner) => Debug.Log("OnConnectedToServer, we are " + (runner.IsServer ? "server" : "client"));

	public void OnDisconnectedFromServer(NetworkRunner runner) => Debug.Log("OnDisconnectedFromServer");

	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) => Debug.Log("OnConnectRequest from " + request.RemoteAddress);

	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) => Debug.Log("OnConnectFailed: " + reason);

	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) => Debug.Log("OnUserSimulationMessage: " + message);

	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) => Debug.Log("OnSessionListUpdated");

	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) => Debug.Log("OnCustomAuthenticationResponse");

	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) => Debug.Log("OnHostMigration (should not happen for TMR)");

	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) => Debug.Log("OnReliableDataReceived from player " + player);

	public void OnSceneLoadDone(NetworkRunner runner) => Debug.Log("OnSceneLoadDone");

	public void OnSceneLoadStart(NetworkRunner runner) => Debug.Log("OnSceneLoadStart");

	#endregion

	// #region Shared Spatial Anchor
	//
	// bool QuestIDUploaded = false;
	// private List<ulong> ClientsQuestIDList = new List<ulong>();
	//
	// void Update()
	// {
	// 	if (!isHost && !QuestIDUploaded && !_runner.IsServer)
	// 	{
	// 		Debug.Log("About to upload QuestID");
	// 		// send local user's quest ID to the server
	// 		Oculus.Platform.Core.Initialize();
	// 		var userRequest = Oculus.Platform.Users.GetLoggedInUser();
	// 		userRequest.OnComplete((Message<User> message) =>
	// 		{
	// 			if (message.IsError)
	// 			{
	// 				Debug.Log("Error getting user: " + message.GetError().Message);
	// 				return;
	// 			}
	//
	// 			ulong OculusID = message.Data.ID;
	// 			Debug.Log("User ID: " + OculusID.ToString());
	// 			//ClientsQuestIDList.Add(ClientsQuestIDList.Count + 1, OculusID); // 1-indexed playerNum
	// 			ClientsQuestIDList.Add(OculusID);
	// 			QuestIDUploaded = true;
	// 			// RPC_Add_User(player, userID);
	// 		});
	// 	}
	//
	// 	// print ClientsQuestIDList
	// 	var index = 1;
	// 	foreach (var item in ClientsQuestIDList)
	// 	{
	// 		Debug.Log("playerNum: " + index + ", QuestID: " + item);
	// 		index++;
	// 	}
	// }
	//
	// #endregion

	#region Scenic

	public GameObject ZMQManagerObject;

	#endregion
}
