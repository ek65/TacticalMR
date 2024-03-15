using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using Convai.Scripts;

public class HumanInterface : MonoBehaviour
{
    private int localTick;  // NOTE: This is not the true tick and is what we will use to internally record a timestep.

    private ExitScenario exitScene;
    private AudioSource source;
    public ActionAPI actionAPI;
    public GameObject arrowGenerator;
    public GameObject circleGenerator;
    
    private TimelineManager tlManager;
    private ConvaiNPC npc;
    private ObjectsList objectList;

    private bool circleSpawned = false;
    private bool arrowSpawned = false;

    private GameObject circle0;
    private GameObject circle1;
    private GameObject arrow0;

    private GameObject coach;
    
    // Start is called before the first frame update
    void Start()
    {
        exitScene = GetComponent<ExitScenario>();
        source = GetComponent<AudioSource>();
        tlManager = FindObjectOfType<TimelineManager>();
        npc = GameObject.FindGameObjectWithTag("Character").GetComponent<ConvaiNPC>();
        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        coach = objectList.scenicPlayers[5];
        SpawnCircle0(coach.transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (circle0 != null)
        {
            var temp = new Vector3(coach.transform.position.x, 2f, coach.transform.position.z);
            circle0.transform.position = temp;
        }
        if (npc.currResponse != null)
        {
            Debug.LogError("current response: " + npc.currResponse);
        }
        if (!circleSpawned && npc.currResponse == "When you're on the field and you notice you're the one closest to the opponent with the ball, that's your cue to press.")
        {
            SpawnCircle1(objectList.scenicPlayers[1].transform.position); // hardcoded 2nd opponent position
            circleSpawned = true;
        } 
        if (!arrowSpawned && npc.currResponse == "You move in, keeping yourself within a meter of them.")
        {
            SpawnArrow(coach.transform.position);
            arrowSpawned = true;
        }
        
        if (tlManager.Paused == false && circleSpawned && arrowSpawned)
        {
            Destroy(circle1);
            Destroy(arrow0);
            circleSpawned = false;
            arrowSpawned = false;
        }
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
    
    public void SpawnCircle0(Vector3 pos)
    {
        pos = new Vector3(pos.x, 2f, pos.z);
        GameObject circle = Instantiate(circleGenerator, pos, circleGenerator.transform.rotation);
        circle.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
        circle0 = circle;
    }
    
    public void SpawnCircle1(Vector3 pos)
    {
        pos = new Vector3(pos.x, 2f, pos.z);
        GameObject circle = Instantiate(circleGenerator, pos, circleGenerator.transform.rotation);
        circle.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
        circle1 = circle;
    }
    
    public void SpawnArrow(Vector3 pos)
    {
        pos = new Vector3(pos.x, 2f, pos.z + 1.25f);
        GameObject arrow = Instantiate(arrowGenerator, pos, arrowGenerator.transform.rotation);
        arrow.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
        arrow0 = arrow;
    }
    
    public void ApplyMovement(ScenicMovementData data)
    {
        localTick += 1;
        // Debug.Log(GetComponent<Rigidbody>().velocity);
        if (localTick < 4)
        {
            return;
        }

        
        if (data.actionFunc != null)
        {
            Type type = actionAPI.GetType();
            MethodInfo method = type.GetMethod(data.actionFunc);
            
            Debug.LogError("im in here");

            method.Invoke(actionAPI, data.actionArgs.ToArray());
        }
        else //idle
        {
            Debug.LogError("im in here2");
            actionAPI.stopMovement = true;
        }
    }
}
