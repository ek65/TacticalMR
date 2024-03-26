using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewArrowGenerator : MonoBehaviour
{
    public LineRenderer lineRenderer;

    public Transform startPos;
    public Transform endPos;
    
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer.positionCount = 2;
    }

    public void GenerateArrow()
    {
        lineRenderer.positionCount = 2;
        
        lineRenderer.SetPosition(0, startPos.position);
        lineRenderer.SetPosition(1, endPos.position);
    }
    
    public void SetStartPos(Transform pos)
    {
        startPos = pos;
    }
    
    public void SetEndPos(Transform pos)
    {
        endPos = pos;
    }
}
