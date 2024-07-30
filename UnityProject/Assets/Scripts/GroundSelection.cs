using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GroundSelection : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject groundHighlighter;

    private Camera cam;
    private RaycastHit raycastHit;
    
    private KeyboardInput keyboardInput;

    private void Start()
    {
        cam = Camera.main;
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
    }

    private void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out raycastHit))
        {
            groundHighlighter.transform.position = raycastHit.point;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (keyboardInput.canClick && keyboardInput.segmentCount > 0)
        {
            GameObject go = Instantiate(groundHighlighter, raycastHit.point, Quaternion.identity);
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
    
}
