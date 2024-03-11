using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UltimateReplay.Formatters;

public class FloatingText : MonoBehaviour
{
    public Vector3 offset;
    
    void Update()
    {
        transform.localRotation = Quaternion.Euler(90,0,0); // lock rotation
        transform.rotation = Quaternion.Euler(90,0,0); // lock rotation

        transform.position = this.transform.parent.position + offset;
    }
    
    public void SetText(string text)
    {
        GetComponent<TextMesh>().text = "[ " + text + " ]";
    }
}