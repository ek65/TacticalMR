using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Fusion;
using Oculus.Interaction;
using OpenAI.Samples.Chat;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utilities.Extensions;
using Whisper.Samples;
using Vector3 = UnityEngine.Vector3;

public class KeyboardInput : MonoBehaviour
{
    [Header("Movement / Speed")]
    [SerializeField] public float moveSpeed = 4f;
    [Header("Scene References")]
    public TimelineManager timelineManager;
    private GameManager gameManager;
    private ProgramSynthesisManager programSynthesisManager;

    public Rigidbody rb;
    public Vector3 movement;

    void Start()
    {
        timelineManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        programSynthesisManager = GameObject.FindGameObjectWithTag("ProgramSynthesisManager").GetComponent<ProgramSynthesisManager>();

        Debug.Log("KeyboardInput script initialized");
    }

    void Update()
    {
        // Press E to restart
        if (Input.GetKeyDown(KeyCode.E) && gameObject.CompareTag("keyboard"))
        {
            // jsonDirectory.DoNotSaveDemonstrationButton();
            programSynthesisManager.HandleRestart();
        }

        // Press B to toggle segment
        if (Input.GetKeyDown(KeyCode.B) && gameObject.CompareTag("keyboard"))
        {
            programSynthesisManager.HandleSegment();
        }

        // Press P to pause/unpause
        if (Input.GetKeyDown(KeyCode.P) && gameObject.CompareTag("keyboard"))
        {
            programSynthesisManager.HandlePause();
        }

        // Annotation clicks
        // if (canClick)
        // {
        //     if (isAnnotationMode && Input.GetMouseButtonDown(0))
        //     {
        //         StartCoroutine(programSynthesis.HandleClickWithDelay(programSynthesis.HandleAnnotationMode));
        //     }
        //     if (isPositionMode && Input.GetMouseButtonDown(0))
        //     {
        //         StartCoroutine(programSynthesis.HandleClickWithDelay(programSynthesis.HandlePositionMode));
        //     }
        // }

        // Debug: Press K 
        // if (Input.GetKeyDown(KeyCode.K))
        // {
        //     foreach (var ann in annotation)
        //     {
        //         if (ann.Value is GameObject obj)
        //         {
        //             Debug.Log($"Annotation Key {ann.Key}, Object: {obj.name}");
        //         }
        //         else if (ann.Value is Vector3 vector)
        //         {
        //             Debug.Log($"Annotation Key {ann.Key}, Position: {vector}");
        //         }
        //     }
        // }
    }
    
    // For annotation clicks
    public void HandlePositionClick()
    {
        StartCoroutine(programSynthesisManager.HandleClickWithDelay(programSynthesisManager.HandlePositionMode));
    }

    public void HandleAnnotationClick()
    {
        StartCoroutine(programSynthesisManager.HandleClickWithDelay(programSynthesisManager.HandleAnnotationMode));
    }
    
    void FixedUpdate()
    {
// #if UNITY_EDITOR
        if (rb == null)
        {
            GameObject coachObject = GameObject.FindGameObjectWithTag("human");
            if (coachObject == null)
            {
                return;
            }
            if (coachObject != null)
            {
                rb = coachObject.GetComponent<Rigidbody>();
                Debug.Log("found human rigidbody");
            }
        }
        if (rb == null) return;
        if (timelineManager.Paused) return;

        float horizontalInput = 0f;
        float verticalInput = 0f;
        
        if (gameManager.laptopMode)
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
        }
        
        Vector3 forwardDirection = transform.forward;
        movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized * moveSpeed;
        
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
// #endif
    }
}