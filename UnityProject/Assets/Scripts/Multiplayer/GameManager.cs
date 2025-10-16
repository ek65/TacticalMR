using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using Oculus.Interaction;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game manager that handles networking setup, player spawning, and game mode coordination.
/// Supports three primary modes: Host (VR headset), Client (laptop observer), and Laptop Mode (single-player).
/// Manages Photon Fusion networking, Scenic integration, and cross-platform compatibility.
/// </summary>
public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
{
	#region Generic

	[Tooltip("Automatically decide host. Host should be headset, client should be laptop.")]
	public bool autoIsHost;

	[Tooltip("If this is checked, then the game will start as a host.")]
	public bool isHost;

	[Tooltip("If this is checked, runs in single-player mode with observer camera and Scenic enabled.")]
	public bool laptopMode;

	public GameObject _ObserverCamera;
	
	public PointableCanvasModule _pointableCanvasModule;

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

		// Laptop mode overrides host/client settings and acts as host
		if (laptopMode)
		{
			// Disable oculus pointer interaction
			_pointableCanvasModule.enabled = false;
			
			isHost = true;
			Debug.Log("Running in Laptop Mode (Single Player)");
			
			// Enable Scenic communication (like host)
			ZMQManagerObject.SetActive(true);
			
			// Enable Observer Camera (like client)
			_ObserverCamera.SetActive(true);
			
			// Start in single player mode
			if (_runner == null)
			{
				StartGame(GameMode.Single);
			}
		}
		else if (isHost) // host
		{
			Debug.Log("We are the host");
			
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
			Debug.Log("We are the client");
			
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
		if ((isHost || laptopMode) && Input.GetKeyDown(KeyCode.R))
		{
			// ball.transform.position = new Vector3(0, 1, 1);
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		if (isHost)
		{
			GameObject ground = GameObject.FindGameObjectWithTag("Ground");
			ground.GetComponent<MeshRenderer>().enabled = false;
			
			GameObject soccerField = GameObject.Find("Soccer Field");
			soccerField.GetComponent<MeshRenderer>().enabled = false;
		}
#endif
	}

	#endregion

	#region Networking with Photon Fusion

	public NetworkRunner _runner;

	public int sessionNum = 0;


	async void StartGame(GameMode mode)
	{
		Debug.Log("in StartGame with mode: " + mode);
    
		if (_runner == null)
		{
			_runner = gameObject.AddComponent<NetworkRunner>();
			_runner.AddCallbacks(this);
		}

		// In laptop mode or host mode, we provide input
		_runner.ProvideInput = (laptopMode || isHost);
		Debug.Log("we provide input: " + (laptopMode || isHost));
    
		// Build a NetworkSceneInfo from the current scene
		var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
		var sceneInfo = new NetworkSceneInfo();
		if (scene.IsValid)
		{
			sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
		}
    
		// Create unique session name
		string sessionName = laptopMode 
			? "LaptopMode" 
			: $"GameRoom_{sessionNum}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

		Debug.Log($"Starting game: Mode={mode}, Session={sessionName}");

		// Start or join session
		var result = await _runner.StartGame(new StartGameArgs()
		{
			GameMode = mode,
			SessionName = sessionName,
			Scene = scene,
			SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
		});
    
		if (!result.Ok)
		{
			Debug.LogError($"StartGame failed: {result.ShutdownReason}");
			Debug.LogError($"Error message: {result.ErrorMessage}");
		}
		else
		{
			Debug.Log($"StartGame successful. Mode={mode}, Session={sessionName}");
		}
	}

	[SerializeField] private NetworkPrefabRef _playerPrefab;
	private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
	
	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
		if (runner.IsServer)
		{
			Debug.Log($"Player joined: {player.PlayerId}");
		
			// Spawn only the host's network representation
			if (player == runner.LocalPlayer && isHost)
			{
				Vector3 spawnPosition = new Vector3(0, 0, 0);
				NetworkObject hostNetworkPlayer = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
				
				// Hide host's own networked player mesh locally
				Transform hostTransform = hostNetworkPlayer.gameObject.transform;
				Transform playerMesh = hostTransform.FindChildRecursive("Armature_Mesh");
				
				if (playerMesh != null)
				{
					playerMesh.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = false;
				}
				
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
		return;
	}
	
	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
	{
		Debug.LogError($"Shutdown: {shutdownReason}");
		if (runner.IsCloudReady == false)
		{
			Debug.LogError("Cloud was not ready");
		}
	}

	#endregion

	#region Unused INetworkRunnerCallbacks with debug logs


	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
	{
		return;
	}
	
	public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
	{
		return;
	}
	
	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
	{
		return;
	}

	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
	{
		return;
	}
	
	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
	{
		return;
	}

	public void OnConnectedToServer(NetworkRunner runner) => Debug.Log("OnConnectedToServer, we are " + (runner.IsServer ? "server" : "client"));

	public void OnDisconnectedFromServer(NetworkRunner runner) => Debug.Log("OnDisconnectedFromServer");

	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) => Debug.Log("OnConnectRequest from " + request.RemoteAddress);

	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) => Debug.Log("OnConnectFailed: " + reason);

	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) => Debug.Log("OnUserSimulationMessage: " + message);
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
	{
		return;
	}

	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) => Debug.Log("OnSessionListUpdated");

	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) => Debug.Log("OnCustomAuthenticationResponse");

	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) => Debug.Log("OnHostMigration (should not happen for TMR)");

	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) => Debug.Log("OnReliableDataReceived from player " + player);

	public void OnSceneLoadDone(NetworkRunner runner) => Debug.Log("OnSceneLoadDone");

	public void OnSceneLoadStart(NetworkRunner runner) => Debug.Log("OnSceneLoadStart");

	#endregion

	#region Scenic

	public GameObject ZMQManagerObject;

	#endregion
}