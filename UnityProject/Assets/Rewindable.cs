using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rewindable : MonoBehaviour
{
    [SerializeField] bool RewindTransform = true;
 
    public void ApplySnippet(MomentSnippet snippet)
    {
        transform.position = snippet.position;
        transform.rotation = snippet.rotation;
    }
    
}
