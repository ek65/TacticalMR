using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
{
	[SerializeField] private SpatialAnchorManager SAmanager;
	#region Generic

	[Tooltip("If this is checked, and the game is running in the Unity Editor, then isHost will be checked.")]
	public bool autoIsHost;

	[Tooltip("If this is checked, then the game will start as a host.")]
	public bool isHost;

	public GameObject _ObserverCamera;

	void Start()
	{
		// print the current runtime type
		Debug.Log("Runtime type: " + UnityEngine.Application.platform);

		if (autoIsHost)
		{
			// if we're running in the Unity Editor
			#if UNITY_EDITOR
				isHost = true;
			#endif
		}

		Debug.Log("We are the " + (isHost ? "host" : "client"));

		if (isHost) // host
		{
			// enable `ZMQManager` to listen to Scenic
			ZMQManagerObject.SetActive(true);
			// switch to the `Observer Camera`
			_ObserverCamera.SetActive(true);
			// make a photon fusion room
			if (_runner == null)
			{
				StartGame(GameMode.Server);
			}
		}
		else // client
		{
			// Connect to photon fusion room
			if (_runner == null)
			{
				StartGame(GameMode.Client);
			}
			// place spacial anchor or load spatial anchor
		}
	}

	// Update is called once per frame

	#endregion

	#region Networking with Photon Fusion

	private NetworkRunner _runner;


	async void StartGame(GameMode mode)
	{
		// Create the Fusion runner and let it know that we will be providing user input
		Debug.Log("in StartGame");
		_runner = gameObject.AddComponent<NetworkRunner>();
		_runner.ProvideInput = !isHost; // server doesn't provide input
		Debug.Log("we provide input: " + !isHost);

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
		// The user's prefab has to be spawned by host
		if (runner.IsServer)
		{
			Debug.Log($"OnPlayerJoined. PlayerId: {player.PlayerId}");
			// Create a unique position for the player
			Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.DefaultPlayers) * 3, 1, 0);
			// We make sure to give the input authority to the connecting player for their user's object
			NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
			// Keep track of the player avatars so we can remove it when they disconnect
			_spawnedCharacters.Add(player, networkPlayerObject);
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
		var data = new NetworkInputData();

		// player movement
		if (Input.GetKey(KeyCode.W))
			data.Direction += Vector3.forward;

		if (Input.GetKey(KeyCode.S))
			data.Direction += Vector3.back;

		if (Input.GetKey(KeyCode.A))
			data.Direction += Vector3.left;

		if (Input.GetKey(KeyCode.D))
			data.Direction += Vector3.right;

		// player tries to intercept the ball
		data.TryIntercept = Input.GetKey(KeyCode.I);

		input.Set(data);
	}

	#endregion

	#region Unused INetworkRunnerCallbacks with debug logs

	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) => Debug.Log("OnInputMissing");

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

	#region Shared Spatial Anchor

	void Update()
    {
		Debug.Log("READY");
		if (OVRInput.GetUp(OVRInput.Button.One)) //A
		{
			Debug.Log("About to set placement mode to true");
			SAmanager.SetPlacementMode(true);
		}
		if (OVRInput.GetUp(OVRInput.Button.Two)) //B
		{
			Debug.Log("About to try to save anchor");
			SAmanager.SaveAnchor();
		}

		/* will not work yet */
		if (OVRInput.GetUp(OVRInput.Button.Three)) //X
		{
			SAmanager.ShareAnchor();
		}
		if (OVRInput.GetUp(OVRInput.Button.Four)) //Y
		{
			SAmanager.LoadAnchor();
		}
	}


	#endregion

	#region Scenic

	public GameObject ZMQManagerObject;

	#endregion
}
