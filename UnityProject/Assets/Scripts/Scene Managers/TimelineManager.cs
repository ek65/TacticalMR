using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;

public class MomentSnippet{
    public MomentSnippet(Vector3 pos, Quaternion quat)
    {
        position = pos;
        this.rotation = quat;
    }
    public Vector3 position;
    public Quaternion rotation;
    
}

[System.Serializable]
public class RewindableTimeSeries
{
    public List<Vector3> positions = new List<Vector3>();
    public List<Quaternion> rotations = new List<Quaternion>();
    public string currBehavior = "None";
    public string currAction = "None";    //TODO remove elements over x seconds ago
    public void RecordTransform(Transform t, string b, string a)
    {
        positions.Add(t.position);
        rotations.Add(t.rotation);
        currBehavior = b;
        currAction = a;
    }
    public MomentSnippet GetMomentSnippet(int TimeIndex)
    {
        return new MomentSnippet(positions[TimeIndex], rotations[TimeIndex]);
    }
}

public class TimelineManager : MonoBehaviour
{
    public List<Rewindable> rewindables;
    
    public Dictionary<GameObject, RewindableTimeSeries> Timeseries;
    public bool rewinding = false;
    public bool advancing = false;
    public bool Paused = false;
    public TextMeshProUGUI pauseBtnTxt;
    public TextMeshProUGUI pauseTxt;
    public TextMeshProUGUI posText;
    public int TimeIndex = 0;
    public Camera camera;
    public int GlobalTimeIndex;

  
    public int RewindTimeIndex = 0;
    public int maxRewindTimeIndex = 0;
    
    public int segmentCount = -1;
    public bool isRecordingSegment = false;

    // Start is called before the first frame update
    void Start()
    {
        Timeseries = new Dictionary<GameObject, RewindableTimeSeries>();
        rewindables = new List<Rewindable>();
        InstantiateScenicObject.Publish += InitializeOnScenicAdd;
        camera = Camera.main;
    }
    public void NotifyPauseStatus(bool pause)
    {

        if (pause&&!Paused)
        {
            
            Pause();
        }
        else if(!pause&&Paused)
        {
            Unpause();
        }
    }
public void InitializeTimeline()
    {
        TimeIndex = 0;
        RewindTimeIndex = 0;
        Reset();
        rewindables = FindObjectsOfType<Rewindable>().ToList();
  
        foreach (Rewindable r in rewindables)
        {
            Timeseries.Add(r.gameObject, new RewindableTimeSeries());
        }
        // InstantiateScenicObject.Pub(this.gameObject);
  
    }
    public void InitializeOnScenicAdd(ScenicObectAddEventArg eventArg)
    {
        Rewindable rewindable = eventArg.gameObject.GetComponent<Rewindable>();
        if (rewindable != null)
        {
            rewindables.Add(rewindable);
        }
 
        Timeseries.Add(eventArg.gameObject, new RewindableTimeSeries());
        
    }
    public void ClickPause()
    {
        

        pauseBtnTxt.text = Paused?"Pause":"Unpause";
        if (Paused)
        {
            Unpause();
        }
        else
        {
            Pause();
        }
   
    }
    public void Pause()
    {
        foreach (Rewindable r in rewindables)
        {
            if (r.Pausible)
            {
                r.Freeze();
            }
        }
        pauseTxt.text = "Scenic Called Pause...Everything paused except ego";
        Paused = true;
        maxRewindTimeIndex = 0;
    }
    public void Unpause()
    {
        foreach (Rewindable r in rewindables)
        {
            if (r.Pausible)
            {
                r.Unfreeze();
            }
        }
        // reset timeseries
        Timeseries = new Dictionary<GameObject, RewindableTimeSeries>();
        InitializeTimeline();
        // reset time index on unpause
        TimeIndex = 0;
        RewindTimeIndex = 0;
        pauseTxt.text = "Unpaused";
        Paused = false;
    }
    public void RaycastClick()
    {
        RaycastHit hit;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("Coach points at ? x: " + hit.transform.position.x + ", y: " + hit.transform.position.y);

            // Do something with the object that was hit by the raycast.
        }
    }
    //0.02 seconds, 50 frames per second
    public void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.LogError("test");
            InitializeTimeline();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            rewinding = true;
        }
        
        foreach (KeyValuePair<GameObject, RewindableTimeSeries> entry in Timeseries)
        {
            Debug.LogError(entry.Key.name);
            Debug.LogError(entry.Value);
        }

        if (Paused)
        {
            if (maxRewindTimeIndex < RewindTimeIndex)
            {
                maxRewindTimeIndex = RewindTimeIndex;
            }
        }
        else
        {
            TimeIndex += 1;
            GlobalTimeIndex += 1;
            RewindTimeIndex += 1;
            RecordData();
        }
        
        if (Paused && rewinding)
        {
            if(RewindTimeIndex <= 0)
            {
                //idk, stop??
                return;
            }
            
            RewindTimeIndex -= 1;
            foreach(Rewindable r in rewindables)
            {
                r.ApplySnippet(Timeseries[r.gameObject].GetMomentSnippet(RewindTimeIndex));
            }
        }
        else if (Paused && advancing)
        {
            if(RewindTimeIndex >= maxRewindTimeIndex)
            {
                //idk, stop??
                return;
            }
            
            RewindTimeIndex += 1;
            foreach(Rewindable r in rewindables)
            {
                r.ApplySnippet(Timeseries[r.gameObject].GetMomentSnippet(RewindTimeIndex));
            }
            
        }
        else if (Paused && !rewinding || !advancing)
        {
            return;
        }
        

    }
    public void RecordData()
    {
       foreach(Rewindable r in rewindables)
        {
            string behavior = "None";
            string action = "None";
            if (r.GetComponent<HumanInterface>() != null)
            {
                behavior = r.GetComponent<HumanInterface>().behavior;
                action = r.GetComponent<HumanInterface>().currAction;
            }
            else if (r.GetComponent<PlayerInterface>() != null)
            {
                behavior = r.GetComponent<PlayerInterface>().behavior;
                action = r.GetComponent<PlayerInterface>().currAction;
            }
            Timeseries[r.gameObject].RecordTransform(r.transform, behavior, action);
        }

    }
    public void Rewind()
    {
        foreach (Rewindable r in rewindables)
        {
            if (r.Pausible)
            {
                r.Unfreeze();
            }
        }
        rewinding = true;
    }


    public void Reset()
    {
        Timeseries = new Dictionary<GameObject, RewindableTimeSeries>();
        rewindables = new List<Rewindable>();
        segmentCount = -1;
        JSONDirectory jsonDirectory = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONDirectory>();
        jsonDirectory.ResetRecordingNum();
        
    }
    
    // Update is called once per frame
    void Update()
    {
        if (Paused)
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastClick();
            }
        }

    }
   
}
