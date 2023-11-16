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
	}
// ----- Shared Spatial Anchor -----


// ----- Scenic -----

	public GameObject ZMQManagerObject;
}