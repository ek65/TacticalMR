using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GroundSelection : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject groundHighligher;

    private Camera cam;
    private RaycastHit raycastHit;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out raycastHit))
        {
            groundHighligher.transform.position = raycastHit.point;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameObject go = Instantiate(groundHighligher, raycastHit.point, Quaternion.identity);
        go.GetComponent<Collider>().enabled = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        groundHighligher.SetActive(true);

    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        groundHighligher.SetActive(false);
    }
    
}
