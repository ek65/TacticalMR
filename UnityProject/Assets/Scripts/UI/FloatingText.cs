using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UltimateReplay.Formatters;

public class FloatingText : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 2, 1);
    
    void Update()
    {
        transform.localRotation = Quaternion.Euler(90,0,0); // lock rotation
        transform.rotation = Quaternion.Euler(90,0,0); // lock rotation
        
        // FOR VR VIEW ONLY
        // transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

        transform.position = this.transform.parent.position + offset;
    }
    
    public void SetText(string text)
    {
        GetComponent<TextMesh>().text = "[ " + text + " ]";
    }
    
    public void SetText2(string text)
    {
        GetComponent<TextMesh>().text = text;
    }
}