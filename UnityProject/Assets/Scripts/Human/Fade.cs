using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Fade : MonoBehaviour
{
    [SerializeField] public Image blackout;
    public bool fade;
    public float fadeRate;
    
    // Start is called before the first frame update
    void Start()
    {
        fade = false; //turn to false later
        fadeRate = 10.0f;
        if (blackout == null)
        {
            blackout = GameObject.FindGameObjectWithTag("MainCamera").GetComponentInChildren<Image>();
        }
    }

    private void Update()
    {
        if (blackout == null)
        {
            blackout = GameObject.FindGameObjectWithTag("MainCamera").GetComponentInChildren<Image>();
        }
    }

    void LateUpdate()
    {
        if (!gameObject.CompareTag("human"))
        {
            return;
        }
        Color currentColor = blackout.color;
        if (fade)
        {
            currentColor.a = Mathf.Lerp(currentColor.a, 1.0f, fadeRate * Time.deltaTime);
            if (currentColor.a >= 0.999f)
            {
                fade = false;
                currentColor.a = 0.0f;
            }
        }
        blackout.color = currentColor;
    }
    
    public void StartFadeAndMove(Vector3 pos, Quaternion rot)
    {
        fade = true;
        StartCoroutine(UpdateFade());
        HumanInterface p = GetComponent<HumanInterface>();
        p.SetTransform(pos, rot);
    }
    
    public void StartFadeAndMove2(GameObject go, Vector3 pos)
    {
        fade = true;
        StartCoroutine(UpdateFade());
        HumanInterface p = GetComponent<HumanInterface>();
        p.SetTransform2(go, pos);
    }
    
    IEnumerator UpdateFade()
    {
        yield return new WaitForSeconds(0.1f);
    }

}
