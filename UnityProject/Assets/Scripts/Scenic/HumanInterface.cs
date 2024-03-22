using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using System.Linq;
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

    private List<GameObject> circleObjects;
    private List<GameObject> arrowObjects;
    // private GameObject circle1;
    // private GameObject arrow0;

    private GameObject coach; // TODO: make scenic tag strings/names so can easily assign these
    
    // Start is called before the first frame update
    void Start()
    {
        exitScene = GetComponent<ExitScenario>();
        source = GetComponent<AudioSource>();
        tlManager = FindObjectOfType<TimelineManager>();
        npc = GameObject.FindGameObjectWithTag("Character").GetComponent<ConvaiNPC>();
        objectList = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<ObjectsList>();
        coach = objectList.scenicPlayers[5];
        circleObjects = new List<GameObject>();
        arrowObjects = new List<GameObject>();
        SpawnCircle(coach.transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (circleObjects[0] != null)
        {
            var temp = new Vector3(coach.transform.position.x, 2f, coach.transform.position.z);
            circleObjects[0].transform.position = temp;
        }
        if (npc.currResponse != null)
        {
            Debug.LogError("current response: " + npc.currResponse);
        }
        if (!circleSpawned && ContainsAll(npc.currResponse, "closest", "opponent"))
        {
            // var closest = 
            SpawnCircle(objectList.scenicPlayers[1].transform.position); // hardcoded 2nd opponent position
            circleSpawned = true;
        } 
        if (!arrowSpawned && npc.currResponse == "You move in, keeping yourself within a meter of them.")
        {
            SpawnArrow(coach.transform.position);
            arrowSpawned = true;
        }
        
        if (tlManager.Paused == false && circleObjects.Count > 1 || arrowObjects.Count > 0)
        {
            if (circleObjects.Count > 1)
            {
                for (int i = 1; i < circleObjects.Count; i++)
                {
                    Destroy(circleObjects[i]);
                }
                circleObjects.RemoveRange(1, circleObjects.Count - 1);
            }
            circleSpawned = false;
            arrowSpawned = false;
        }
    }
    
    public static bool ContainsAll(string source, params string[] values)
    {
        Debug.LogError("values: " + values[0]);
        return values.All(x => source.Contains(x));
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
    
    public void SpawnCircle(Vector3 pos)
    {
        pos = new Vector3(pos.x, 2f, pos.z);
        GameObject circle = Instantiate(circleGenerator, pos, circleGenerator.transform.rotation);
        circleObjects.Add(circle);
        if (circleObjects.Count == 1) // 0th circle should always be the one circling the coach
        {
            circle.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
        }
        else if (circleObjects.Count > 1)
        {
            circle.GetComponent<Renderer>().material.SetColor("_Color", Color.red);

        }
    }

    public void SpawnArrow(Vector3 pos)
    {
        pos = new Vector3(pos.x, 2f, pos.z + 1.25f);
        GameObject arrow = Instantiate(arrowGenerator, pos, arrowGenerator.transform.rotation);
        arrow.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
        // arrow0 = arrow;
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
