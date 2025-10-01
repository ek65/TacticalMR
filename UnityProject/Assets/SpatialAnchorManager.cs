using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using System.Linq;
using System;
using Fusion;
using UnityEngine.Serialization;

public class SpatialAnchorManager : NetworkBehaviour
{
	[SerializeField] private GameObject spatialAnchorPrefab;
	[SerializeField] public Transform player;
	[SerializeField] public Transform anchorPlacementLocation;
	[SerializeField] public List<ulong> userIDList;

	private bool placementMode = false;
	private OVRSpatialAnchor createdAnchor;
	private Transform anchorPlacementTransform;
	private OVRSpatialAnchor alignedAnchor;
	IList<OVRSpatialAnchor> spatialAnchorsList = new List<OVRSpatialAnchor>();
	static IList<Guid> uuids = new List<Guid>();
	private readonly HashSet<Guid> uuidsToLoad = new HashSet<Guid>();

	void Update()
	{
		Debug.Log("In SAM update");
		// if we have input authority
		if (HasInputAuthority)
		{
			Debug.Log("Has input authority");
			if (OVRInput.GetUp(OVRInput.Button.One)) //A
			{
				Debug.Log("Set placement mode to true");
				placementMode = true;
			}

			// if (OVRInput.GetUp(OVRInput.Button.Two)) //B
			// {
			//     Debug.Log("About to try to save anchor");
			//     // SA_manager.SaveAnchor();
			// }

			if (OVRInput.GetUp(OVRInput.Button.Three)) //X
			{
				Debug.Log("About to try to share anchor");
				ShareAnchor();
			}

			// if (OVRInput.GetUp(OVRInput.Button.Four)) //Y
			// {
			//     SA_manager.LoadAnchor();
			// }

			if (placementMode == true)
			{
				Debug.Log("Can place now");
				if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
				{
					Debug.Log("About to create anchor");
					StartCoroutine(CreateSaveShare());
					placementMode = false;
				}
			}
		}
	}


	/*public void CreateAnchor(Transform playerRightHand)
	{
	    Debug.Log("In CreateAnchor");
	    anchorPlacementTransform = playerRightHand;
	    GameObject newobj = Instantiate(spatialAnchorPrefab, anchorPlacementTransform.position, anchorPlacementTransform.rotation);
	    createdAnchor = newobj.GetComponent<OVRSpatialAnchor>();
	    Debug.Log("Anchor created");
	}*/

	public IEnumerator CreateSaveShare()
	{
		// CREATE
		anchorPlacementTransform = anchorPlacementLocation;
		GameObject newobj = Instantiate(spatialAnchorPrefab, anchorPlacementTransform.position,
			anchorPlacementTransform.rotation);
		createdAnchor = newobj.AddComponent<OVRSpatialAnchor>();

		while (!createdAnchor.Created)
		{
			yield return new WaitForEndOfFrame(); //keep checking
		}

		Debug.Log("Anchor is ready to be shared");
		spatialAnchorsList.Add(createdAnchor);
		Debug.Log("Num anchors: " + spatialAnchorsList.Count);
		OVRSpatialAnchor.SaveOptions saveOptions;
		saveOptions.Storage = OVRSpace.StorageLocation.Cloud;
		Debug.Log("Save options have been set");

		// save anchor and share in network if saved
		createdAnchor.Save(saveOptions, (createdAnchor, success) =>
			//OVRSpatialAnchor.Save(spatialAnchorsList, saveOptions, (collection, result) =>
		{
			Debug.Log("In save but not yet successful");
			if (success)
			{
				Debug.Log("SAVE SUCCESSFUL");
				uuids.Add(createdAnchor.Uuid);
				Debug.Log("Num anchors: " + uuids.Count);
				Debug.Log("About to share anchor");

				ShareAnchor();
			}
			else if (!success)
			{
				Debug.Log("SAVE FAILED");
			}
		});
	}


	/*public IEnumerator SaveAnchor()
	{
	    // configure save option to be cloud
	    while (!createdAnchor.Created && !createdAnchor.Localized)
	    {
	        yield return new WaitForEndOfFrame();
	    }

	    Debug.Log("In SaveAnchor");
	    OVRSpatialAnchor.SaveOptions saveOptions;
	    saveOptions.Storage = OVRSpace.StorageLocation.Cloud;
	    Debug.Log("Save options have been set");

	    // save anchor and share in network if saved
	    createdAnchor.Save(saveOptions, (createdAnchor, isSuccessful) =>
	    {
	        Debug.Log("In save but not yet successful");
	        if (isSuccessful)
	        {
	            Debug.Log("SAVE SUCCESSFUL");
	            // add to list of anchors
	            spatialAnchorsList.Add(createdAnchor);

	            // add uuid to a list of uuids
	            uuids.Add(createdAnchor.Uuid);
	            Debug.Log("About to share anchor");

	            ShareAnchor();
	        } else
	        {
	            Debug.Log("SAVE FAILED");
	        }
	    });
	}*/


	public void ShareAnchor()
	{
		// create list of people who shared anchor is shared with 
		ICollection<OVRSpaceUser> spaceUserList = new List<OVRSpaceUser>();

		// add people in room to list of people who shared anchor is shared with
		foreach (ulong userID in userIDList)
		{
			spaceUserList.Add(new OVRSpaceUser(userID));
		}

		// share with with people in list
		// the null is the action upon complete
		OVRSpatialAnchor.Share(new List<OVRSpatialAnchor> { createdAnchor }, spaceUserList, OnShareComplete);
		Debug.Log("ANCHOR SHARED");
	}

	private void OnShareComplete(ICollection<OVRSpatialAnchor> spatialAnchors, OVRSpatialAnchor.OperationResult result)
	{
		Debug.Log("on share complete");
		Debug.Log("HasInputAuthority: " + HasInputAuthority);
		// broadcast uuids list
		RPC_ShareAnchor(spatialAnchors.First().Uuid);
	}

	// Method to have clients share anchor with other clients
	[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
	public void RPC_ShareAnchor(Guid anchorId, RpcInfo info = default)
	{
		Debug.Log("in RPC_ShareAnchor, anchorId: " + anchorId.ToString());
		if (info.IsInvokeLocal) return; // don't execute on the client that called the RPC
		{
			Debug.Log("in RPC_ShareAnchor, we are not the client that called the RPC");
			uuids.Add(anchorId);
			LoadAnchor();
		}
	}

	public void LoadAnchor()
	{
		// convert uuids to read-only list
		var uuidsToLoad = uuids.ToList().AsReadOnly();
		Debug.Log("Getting UUIDs for loading:" + uuidsToLoad);

		// load anchor from cloud
		OVRSpatialAnchor.LoadUnboundAnchors(new OVRSpatialAnchor.LoadOptions()
		{
			StorageLocation = OVRSpace.StorageLocation.Cloud,
			Timeout = 10f,
			Uuids = uuidsToLoad
		}, OnLoadUnboundAnchorComplete);
	}

	private void OnLoadUnboundAnchorComplete(OVRSpatialAnchor.UnboundAnchor[] anchors)
	{
		Debug.Log("ANCHOR LOADED");
		anchors[0].Localize(OnLocalizeComplete, 10f);

		// LOCALIZE AND BIND ANCHORS (GENERAL WAY) but try above since we only have one anchor
		/*for(int i = 0; i < anchors.Length; i++)
		{
		    // anchors[i].Localize(OnLocalizeComplete, 10f);

		    var pose = anchors[i].Pose;
		    GameObject newGameObj = Instantiate(spatialAnchorPrefab, anchorPlacementTransform.position, anchorPlacementTransform.rotation);
		    OVRSpatialAnchor newAnchor = newGameObj.AddComponent<OVRSpatialAnchor>();
		    anchors[i].BindTo(newAnchor);
		}*/
	}

	private void OnLocalizeComplete(OVRSpatialAnchor.UnboundAnchor anchor, bool success)
	{
		Debug.Log("ANCHOR LOCALIZED");
		var pose = anchor.Pose;
		GameObject newGameObj = Instantiate(spatialAnchorPrefab, pose.position, pose.rotation);
		createdAnchor = newGameObj.AddComponent<OVRSpatialAnchor>();
		anchor.BindTo(createdAnchor);
		Debug.Log("ANCHOR BOUNDED");

		// AlignToAnchor();
		// Debug.Log("ANCHOR ALIGNED");
	}


	public void AlignToAnchor()
	{
		if (alignedAnchor != null)
		{
			player.position = Vector3.zero;
			player.eulerAngles = Vector3.zero;

			return;
			// yield return null;
		}

		var anchorTransform = createdAnchor.transform;

		if (player)
		{
			player.position = anchorTransform.InverseTransformPoint(Vector3.zero);
			player.eulerAngles = new Vector3(0, -anchorTransform.eulerAngles.y, 0);
		}

		/*if (playerHands)
		{
		    playerHands.localPosition = -player.position;
		    playerHands.localEulerAngles = -player.eulerAngles;
		}*/

		alignedAnchor = createdAnchor;
		Debug.Log("Anchor aligned");
	}
}