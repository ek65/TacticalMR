using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using System.Linq;
using System;

public class SpatialAnchorManager : MonoBehaviour
{

    // Spatial anchor; alternatively create a gameobject prefab with OVRSpatialAnchor attached to it
    [SerializeField] private GameObject spatialAnchorPrefab;
    // human player
    [SerializeField] public Transform player;
    [SerializeField] public Transform playerRightHand;

    private bool placementMode = false;
    private OVRSpatialAnchor createdAnchor;
    private Transform anchorPlacementTransform;
    private OVRSpatialAnchor alignedAnchor;
    IList<OVRSpatialAnchor> spatialAnchorsList = new List<OVRSpatialAnchor>();
    IList<Guid> uuids = new List<Guid>();
    private readonly HashSet<Guid> uuidsToLoad = new HashSet<Guid>();


    void Start() { }


    public void SetPlacementMode(bool setting)
    {
        placementMode = setting;
        Debug.Log("Placement mode set");
    }


    void Update()
    {
        if (placementMode == true)
        {
            Debug.Log("Can place now");
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
            {
                Debug.Log("About to create anchor");
                //CreateAnchor();
                //CreateSaveShare();
                StartCoroutine(CreateSaveShare());
                //placementMode = false;
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
        anchorPlacementTransform = playerRightHand;
        GameObject newobj = Instantiate(spatialAnchorPrefab, anchorPlacementTransform.position, anchorPlacementTransform.rotation);
        createdAnchor = newobj.AddComponent<OVRSpatialAnchor>();

        /*if (!createdAnchor.Localized)
        {
            createdAnchor.Localize();
        }*/

        Debug.Log("ANCHOR CREATED");
        Debug.Log("Waiting to see if anchor is ready to be shared");
        Debug.Log("UUID: " + createdAnchor.Uuid);
        Debug.Log("Created: " + createdAnchor.Created);
        Debug.Log("PendingCreation: " + createdAnchor.PendingCreation);
        Debug.Log("Localized: " + createdAnchor.Localized);
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

                //ShareAnchor();
            }
            else if (!success)
            {
                Debug.Log("SAVE FAILED");
            }
        });
    }


    public IEnumerator SaveAnchor()
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
    }


    public void ShareAnchor()
    {
        // TODO: get users in room
        string[] userIds = new string[10]; // dummy array to prevent errors rn
        //var userIds = photon.GetUserList().Select(userId => userId.ToString()).ToArray(); // actual list from photon

        // create list of people who shared anchor is shared with 
        ICollection<OVRSpaceUser> spaceUserList = new List<OVRSpaceUser>();

        // add people in room to list of people who shared anchor is shared with
        foreach (string strUsername in userIds)
        {
            spaceUserList.Add(new OVRSpaceUser(ulong.Parse(strUsername)));
        }

        // share with with people in list
        // the null is the action upon complete
        OVRSpatialAnchor.Share(new List<OVRSpatialAnchor> { createdAnchor }, spaceUserList, null);
        Debug.Log("Anchor shared");
    }

    /*private static void OnShareComplete(ICollection<OVRSpatialAnchor> spatialAnchors)
    {  
    }*/

    public void LoadAnchor()
    {
        // convert to read-only list
        var uuidsToLoad = uuids.ToList().AsReadOnly();

        // load anchor from cloud
        // the null is action upon complete
        OVRSpatialAnchor.LoadUnboundAnchors(new OVRSpatialAnchor.LoadOptions()
        {
            StorageLocation = OVRSpace.StorageLocation.Cloud,
            Timeout = 0,
            Uuids = uuidsToLoad
        }, null);
        Debug.Log("Anchor loaded");
    }


    // WIP, ienum or void
    // public void AlignToAnchor(OVRSpatialAnchor anchor)

    public void AlignToAnchor(/*Transform player*/)
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

    /*private static void OnLoadUnboundAnchorComplete()
    {
    }*/



}