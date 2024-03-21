using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInput : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField] public float moveSpeed = 4f;
    public ExitScenario exitScenario;
    private Rigidbody rb;
    
    private TimelineManager timelineManager;
    private JSONToLLM jsonToLLM;
    // public GameObject userCircle;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
    }
    void Update()
    {
        // Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.y));
        // transform.LookAt(mousePos);
        //
        // if (userCircle == null)
        // {
        //     userCircle = GameObject.FindGameObjectWithTag("UserCircle");
        // }
        // else
        // {
        //     var temp = new Vector3(mousePos.x, 2f, mousePos.z);
        //     userCircle.transform.position = temp;
        // }

        // if (Input.GetKeyDown("shift"))
        // {
        //     moveSpeed = 2f;
        // } else {
        //     moveSpeed = 4f;
        // }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            exitScenario.EndScenario();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (timelineManager.Paused)
            {
                timelineManager.Unpause();
            }
            else
            {
                timelineManager.Pause();
                try
                {
                    jsonToLLM.PopulateSceneObjects();
                }
                catch(Exception e)
                {
                    Debug.LogError("Error populating scene objects: " + e.Message);
                }
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jsonToLLM.WriteJSON();
        }
    }

    void FixedUpdate()
    {
        if (timelineManager.Paused)
        {
            return;
        }
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        //Vector3 moveDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;

        //Vector3 velocity = moveDirection * moveSpeed;
        //rb.velocity = velocity;

        Vector3 forwardDirection = transform.forward;
        Vector3 movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized * moveSpeed;

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }
}
