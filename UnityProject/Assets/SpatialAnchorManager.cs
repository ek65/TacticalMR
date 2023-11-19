using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using System.Linq;
using System;

public class SpatialAnchorManager : MonoBehaviour
{

    // Spatial anchor; alternatively create a gameobject prefab with OVRSpatialAnchor attached to it
    [SerializeField] private OVRSpatialAnchor spatialAnchorPrefab;
    // human player
    [SerializeField] public Transform player;
    [SerializeField] public Transform playerRightHand;

    private OVRSpatialAnchor createdAnchor;
    private Transform anchorPlacementTransform;
    private OVRSpatialAnchor alignedAnchor;
    IList<OVRSpatialAnchor> spatialAnchorsList = new List<OVRSpatialAnchor>();
    IList<Guid> uuids = new List<Guid>();
    private readonly HashSet<Guid> uuidsToLoad = new HashSet<Guid>();

    // TODO: create a interface/manager for anchor-related networking functions
    // public static PhotonAnchorManager photon;

    //private RayInteractor _rayInteractor;


    void Start()
    {
        // EMPTY
    }

    void Update()
    {
        //var rayInteractorHoveringUI = _rayInteractor == null || (_rayInteractor != null && _rayInteractor.Candidate == null);
        //var shouldPlaceNewAnchor = isPlacementMode && OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) && rayInteractorHoveringUI;
    }

    void getAnchorPosition()
    {
        Ray ray = new Ray(playerRightHand.position, playerRightHand.forward);

        // Set the maximum raycast distance
        float maxRaycastDistance = 10f;

        // Declare a RaycastHit variable to store information about the hit
        RaycastHit hit;

        // Perform the raycast
        if (Physics.Raycast(ray, out hit, maxRaycastDistance))
        {
            // The ray hit something
            Transform hitTransform = hit.transform;

            // Do something with the hit transform (e.g., print its name)
            anchorPlacementTransform = hitTransform;
        }
    }


    public void CreateAnchor()
    {
        // TODO: define positino and rotation for placing anchor, use raycast?
        getAnchorPosition();
        // instantiate OVRSpatialANchor object at that position/rotation
        createdAnchor = Instantiate(spatialAnchorPrefab, anchorPlacementTransform.position, anchorPlacementTransform.rotation);
        Debug.Log("Anchor created");

    }

    /*void SaveAndShareAnchor(OVRSpatialAnchor spatialAnchor)
    {
        OVRSpatialAnchor.SaveOptions saveOptions;
        saveOptions.Storage = OVRSpace.StorageLocation.Cloud;
        OVRSpatialAnchor.Save(spatialAnchor, saveOptions, (collection, result) =>
        {
            if (result is not OVRSpatialAnchor.OperationResult.Success)
            {
                Debug.LogError(result);
                return;
            }

            OVRSpatialAnchor.Share(collection, users, (collection, result) =>
            {
                if (result is OVRSpatialAnchor.OperationResult.Success)
                {
                    Debug.Log("Success sharing anchor.");
                }
                else
                {
                    Debug.LogError(result);
                }
            });
        });
    }*/


    public void SaveAnchor()
    {
        // configure save option to be cloud
        OVRSpatialAnchor.SaveOptions saveOptions;
        saveOptions.Storage = OVRSpace.StorageLocation.Cloud;

        // save anchor and share in network if saved
        createdAnchor.Save(saveOptions, (spatialAnchor, isSuccessful) =>
        {
            if (isSuccessful)
            {
                Debug.Log("Anchor created");
                // add to list of anchors
                spatialAnchorsList.Add(spatialAnchor);

                // add uuid to a list of uuids
                uuids.Add(spatialAnchor.Uuid);

                ShareAnchor();
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


    /*private static void OnLoadUnboundAnchorComplete()
    {
    }*/



}