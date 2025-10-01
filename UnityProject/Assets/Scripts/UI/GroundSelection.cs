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
    public GameObject placedGroundHighlighter;

    private Camera cam;
    private RaycastHit raycastHit;
    
    private KeyboardInput keyboardInput;
    private TimelineManager tlManager;

    private void Start()
    {
        cam = Camera.main;
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
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
#if UNITY_ANDROID && !UNITY_EDITOR
        // android raycast
        GameObject human = GameObject.FindGameObjectWithTag("human");
        if (human == null)
        {
            return;
        }
        if (human != null && human.GetComponent<HumanInterface>().isVR && !human.GetComponent<HumanInterface>().isViewer)
        {
            Ray ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
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
        
    }
    
    public void ClearGroundHighlights()
    {
        GameObject human = GameObject.FindGameObjectWithTag("human");
        if (human != null)
        {
            human.GetComponent<HumanInterface>().xMark = Vector3.zero;
        }
        Destroy(placedGroundHighlighter);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost)
        {
            if (keyboardInput.canClick)
            {
                if (placedGroundHighlighter != null)
                {
                    Destroy(placedGroundHighlighter);
                }
                GameObject go = Instantiate(newGroundHighlighter, raycastHit.point, Quaternion.identity);
                placedGroundHighlighter = go;
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
    
#if UNITY_ANDROID && !UNITY_EDITOR
    public void OnRayClick()
    {
        if (keyboardInput.canClick)
        {
            if (placedGroundHighlighter != null)
            {
                Destroy(placedGroundHighlighter);
            }
            
            // GameObject go = Instantiate(newGroundHighlighter, raycastHit.point, Quaternion.identity);
            NetworkRunner runner = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>()._runner;
            NetworkObject temp = runner.Spawn(newGroundHighlighter, raycastHit.point, Quaternion.identity);
            
            GameObject go = temp.gameObject;
            
            placedGroundHighlighter = go;
            go.GetComponent<Collider>().enabled = true;
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
