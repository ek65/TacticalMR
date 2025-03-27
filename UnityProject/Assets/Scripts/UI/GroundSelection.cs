using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;

public class GroundSelection : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject groundHighlighter;
    public GameObject newGroundHighlighter;
    public List<GameObject> placedGroundHighlighters;

    private Camera cam;
    private RaycastHit raycastHit;
    
    private KeyboardInput keyboardInput;
    private TimelineManager tlManager;

    private void Start()
    {
        cam = Camera.main;
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        placedGroundHighlighters= new List<GameObject>();
    }

    private void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

#if UNITY_EDITOR
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            groundHighlighter.GetComponent<Collider>().enabled = false;
            if (Physics.Raycast(ray, out raycastHit))
            {
                if (raycastHit.transform.gameObject.CompareTag("Ground"))
                {
                    groundHighlighter.transform.position = raycastHit.point;
                }
        
            }
        }
#endif
#if UNITY_ANDROID
        // android raycast
        GameObject human = GameObject.FindGameObjectWithTag("human");
        if (human != null && human.GetComponent<HumanInterface>().isVR)
        {
            Ray ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
            if (Physics.Raycast(ray, out raycastHit))
            {
                if (raycastHit.transform.gameObject.CompareTag("Ground"))
                {
                    groundHighlighter.transform.position = raycastHit.point;
                }
            }
        }
#endif
        
    }
    
    public void ClearGroundHighlights()
    {
        foreach (GameObject go in placedGroundHighlighters)
        {
            Destroy(go);
        }
        placedGroundHighlighters.Clear();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            if (keyboardInput.canClick)
            {
                GameObject go = Instantiate(newGroundHighlighter, raycastHit.point, Quaternion.identity);
                placedGroundHighlighters.Add(go);
                go.GetComponent<Collider>().enabled = true;
                keyboardInput.HandlePositionClick();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            groundHighlighter.SetActive(true);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            groundHighlighter.SetActive(false);
        }
    }
    
#if UNITY_ANDROID
    public void OnRayClick()
    {
        if (keyboardInput.canClick)
        {
            // GameObject go = Instantiate(newGroundHighlighter, raycastHit.point, Quaternion.identity);
            NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
            NetworkObject temp = runner.Spawn(newGroundHighlighter, raycastHit.point, Quaternion.identity);
            
            GameObject go = temp.gameObject;
            
            placedGroundHighlighters.Add(go);
            // go.GetComponent<Collider>().enabled = true;
            RPC_RayClick(temp);
            keyboardInput.HandlePositionClick();
        }
        // Debug.Log("Ray Clicked");
        // GameObject go = Instantiate(newGroundHighlighter, raycastHit.point, Quaternion.identity);
    }
    
    public void OnRayEnter()
    {
        groundHighlighter.SetActive(true);

    }
    
    public void OnRayExit()
    {
        groundHighlighter.SetActive(false);
    }
#endif
    
    private void RPC_RayClick(NetworkObject obj)
    {
        GameObject go = obj.gameObject;
        
        go.GetComponent<Collider>().enabled = true;
    }
    
}
