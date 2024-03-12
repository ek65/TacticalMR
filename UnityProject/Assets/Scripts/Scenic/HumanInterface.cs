using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanInterface : MonoBehaviour
{
    private ExitScenario exitScene;
    private AudioSource source;
    
    // Start is called before the first frame update
    void Start()
    {
        exitScene = GetComponent<ExitScenario>();
        source = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayAudioClip()
    {
        source.PlayOneShot(source.clip);
    }
    
    public void SetTransform(Vector3 pos)
    {
        source.PlayOneShot(source.clip);
        this.transform.position = pos;
        Debug.LogWarning("Local: I am transforming to: " + pos.ToString());

    }
}
