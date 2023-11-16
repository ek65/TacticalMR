using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
{
	void Start()
	{
		// print the current runtime type
		Debug.Log("Runtime type: " + Application.platform);

		// Detect if we're the host
#if UNITY_EDITOR
		isHost = true;
#endif
		Debug.Log("We are the " + (isHost ? "host" : "client"));

		if (isHost)
		{
			// enable `ZMQManager` to listen to Scenic
			ZMQManagerObject.SetActive(true);
			// switch to the `TopDownDebugCamera`
			GameObject.Find("TopDownDebugCamera").GetComponent<Camera>().enabled = true;
			// make a photon fusion room
			if (_runner == null)
			{
				StartGame(GameMode.Host);
			}
		}
		else
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
	void Update() { }

// ----- Networking with Photon Fusion -----

	public bool isHost;

	private NetworkRunner _runner;

	// --- Networking with Photon Fusion ---

	async void StartGame(GameMode mode)
	{
		// Create the Fusion runner and let it know that we will be providing user input
		_runner = gameObject.AddComponent<NetworkRunner>();
		_runner.ProvideInput = !isHost; // server doesn't provide input

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
		if (runner.IsServer)
		{
			// Create a unique position for the player
			Vector3 spawnPosition = new Vector3((player.RawEncoded%runner.Config.Simulation.DefaultPlayers)*3,1,0);
			NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
			// Keep track of the player avatars so we can remove it when they disconnect
			_spawnedCharacters.Add(player, networkPlayerObject);
		}
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		// Find and remove the players avatar
		if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
		{
			runner.Despawn(networkObject);
			_spawnedCharacters.Remove(player);
		}
	}
// ----- Shared Spatial Anchor -----


// ----- Scenic -----

	public GameObject ZMQManagerObject;
}