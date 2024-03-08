using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
public class MomentSnippet{
    public MomentSnippet(Vector3 pos, Quaternion quat)
    {
        position = pos;
        this.rotation = quat;
    }
    public Vector3 position;
    public Quaternion rotation;
    
}
public class RewindableTimeSeries
{
    private List<Vector3> positions = new List<Vector3>();
    private List<Quaternion> rotations = new List<Quaternion>();
    //TODO remove elements over x seconds ago
    public void RecordTransform(Transform t)
    {
        positions.Add(t.position);
        rotations.Add(t.rotation);
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
    public bool Initialized = false;
    public bool Paused = false;
    public TextMeshProUGUI pauseBtnTxt;
    public TextMeshProUGUI posText;
    public int TimeIndex = 0;
    public Camera camera;

  
    public int RewindTimeIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
      
    }
    public void InitializeTimeline()
    {
        rewindables = FindObjectsOfType<Rewindable>().ToList();
        Timeseries = new Dictionary<GameObject, RewindableTimeSeries>();
        foreach (Rewindable r in rewindables)
        {
            Timeseries.Add(r.gameObject, new RewindableTimeSeries());
        }
        Initialized = true;
    }
    public void ClickPause()
    {

        pauseBtnTxt.text = Paused?"Pause":"Unpause";
        Paused = Paused?false:true;
        Time.timeScale = Paused?1:0;
   
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
            InitializeTimeline();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            rewinding = true;
        }
        if (!Initialized)
        {
            return;
        }
        if (Paused) { Time.timeScale = 0; return; }
        if (rewinding)
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
        else
        {
            TimeIndex += 1;
            RewindTimeIndex += 1;
            RecordData();

        }

    }
    public void RecordData()
    {
       foreach(Rewindable r in rewindables)
        {
            Timeseries[r.gameObject].RecordTransform(r.transform);
        }

    }
    public void Rewind()
    {
        rewinding = true;
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
