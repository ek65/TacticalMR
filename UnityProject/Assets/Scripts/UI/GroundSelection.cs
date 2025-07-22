using System;
using System.Collections;
using System.Collections.Generic;
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
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
#if UNITY_EDITOR
        groundHighlighter.GetComponent<Collider>().enabled = false;
        if (Physics.Raycast(ray, out raycastHit))
        {
            if (raycastHit.transform.gameObject.CompareTag("Ground"))
            {
                groundHighlighter.transform.position = raycastHit.point;
            }
            
        }
#endif
#if UNITY_ANDROID
        /*// android raycast
        if (GameObject.FindGameObjectWithTag("human") != null)
        {
            ray = GameObject.FindGameObjectWithTag("RightRay").GetComponent<RayInteractor>().Ray;
            if (Physics.Raycast(ray, out raycastHit))
            {
                if (raycastHit.transform.gameObject.CompareTag("Ground"))
                {
                    groundHighlighter.transform.position = raycastHit.point;
                }
            }
        }*/
#endif
        
    }
    
    public void ClearGroundHighlights()
    {
        GameObject human = GameObject.FindGameObjectWithTag("human");
        if (human != null)
        {
            human.GetComponent<HumanInterface>().xPos = Vector3.zero;
        }
        Destroy(placedGroundHighlighter);
    }

    public void OnPointerClick(PointerEventData eventData)
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        groundHighlighter.SetActive(true);

    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        groundHighlighter.SetActive(false);
    }
    
#if UNITY_ANDROID
    public void OnRayClick()
    {
        if (keyboardInput.canClick)
        {
            GameObject go = Instantiate(newGroundHighlighter, raycastHit.point, Quaternion.identity);
            // go.GetComponent<Collider>().enabled = true;
            // keyboardInput.HandlePositionClick();
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
    
}
