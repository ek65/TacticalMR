using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UltimateReplay.Formatters;

public class FloatingText : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 2, 1);
    
    void Update()
    {
        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        if (gm.isHost) // FOR VR VIEW ONLY
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
        else // FOR DESKTOP VIEW
        {
            transform.localRotation = Quaternion.Euler(90,0,0); // lock rotation
            transform.rotation = Quaternion.Euler(90,0,0); // lock rotation
        }

        // HumanInterface hI = GameObject.FindGameObjectWithTag("human").GetComponent<HumanInterface>();
        HumanInterface hI = transform.parent.GetComponent<HumanInterface>();
        if (gm.isHost && hI != null && hI.isVR)
        {
            this.gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
        
        transform.position = this.transform.parent.position + offset;
        if (hI != null && hI.isVR)
        {
            transform.position = hI.vrTransform.position + offset;
        }
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