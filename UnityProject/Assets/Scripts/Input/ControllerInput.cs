using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerInput : MonoBehaviour
{
    private InputSystem inputSystem;
    private InputAction move;
    private InputAction look;

    public Rigidbody rb;
    public float movementForce = 4f;
    private Vector3 forceDirection = Vector3.zero;

    private float controllerDeadzone = 0.1f;
    private float gamepadRotateSmoothing = 250f;
    public Camera cam;
    private Animator animator;
    private KeyboardInput keyboardInput;
    private ExitScenario exitScenario;
    private JSONToLLM jsonToLLM; 
    private TimelineManager tlManager;
    // private ScenarioManager scenarioManager;

    public Vector3 playerDirection;

    private void Awake()
    {
        jsonToLLM = GameObject.FindGameObjectWithTag("ScenicManager").GetComponent<JSONToLLM>();
        inputSystem = new InputSystem();
        cam = Camera.main;
        animator = GetComponent<Animator>();
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        exitScenario = this.GetComponent<ExitScenario>();
        tlManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
        // scenarioManager = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>();
    }

    private void OnEnable()
    {
        move = inputSystem.PlayerControls.Move;
        look = inputSystem.PlayerControls.Look;
        inputSystem.PlayerControls.Pause.performed += ControllerPause; // A Button
        inputSystem.PlayerControls.Restart.performed += ControllerRestart; // Y Button
        inputSystem.PlayerControls.Segment.performed += ControllerSegment; // X Button
        // soccer actions
            inputSystem.PlayerControls.Intercept.performed += ControllerIntercept; // Left Trigger
            inputSystem.PlayerControls.Pass.performed += ControllerPass; // Right Trigger
            inputSystem.PlayerControls.ThroughPass.performed += ControllerThroughPass; // Right Bumper 
        // factory actions
            inputSystem.PlayerControls.PickUp.performed += ControllerPickUp; // Left Trigger 
            inputSystem.PlayerControls.PutDown.performed += ControllerPutDown; // Right Trigger
            inputSystem.PlayerControls.Enable();
    }
    
    private void OnDisable()
    {
        inputSystem.PlayerControls.Pause.performed -= ControllerPause;
        inputSystem.PlayerControls.Restart.performed -= ControllerRestart;
        inputSystem.PlayerControls.Segment.performed -= ControllerSegment;
        // soccer actions
        inputSystem.PlayerControls.Intercept.performed -= ControllerIntercept;
        inputSystem.PlayerControls.Pass.performed -= ControllerPass;
        inputSystem.PlayerControls.ThroughPass.performed -= ControllerThroughPass;
        // factory actions
        inputSystem.PlayerControls.PickUp.performed -= ControllerPickUp;
        inputSystem.PlayerControls.PutDown.performed -= ControllerPutDown;
        inputSystem.PlayerControls.Disable();
    }

    private void FixedUpdate()
    {
        // if (tlManager.Paused)
        // {
        //     return;
        // }
        Movement();
        Rotate();
    }

    private void ControllerPause(InputAction.CallbackContext ctx)
    {
        keyboardInput.HandlePause();
    }

    private void ControllerRestart(InputAction.CallbackContext ctx)
    {
        keyboardInput.HandleRestart();
    }
    
    private void ControllerSegment(InputAction.CallbackContext ctx)
    {
        keyboardInput.HandleSegment();
    }
    
    // Forcibly Take Possession of Ball from nearby player
    private void ControllerIntercept(InputAction.CallbackContext ctx)
    {
        if (tlManager.Paused)
        {
            return;
        }
        HumanInterface humanInterface = this.GetComponent<HumanInterface>();
        humanInterface.ForciblyGainPossession();
    }
    
    private void ControllerPass(InputAction.CallbackContext ctx)
    {
        if (tlManager.Paused)
        {
            return;
        }
        HumanInterface humanInterface = this.GetComponent<HumanInterface>();
        humanInterface.PassToPlayer();
    }
    
    private void ControllerThroughPass(InputAction.CallbackContext ctx)
    {
        if (tlManager.Paused)
        {
            return;
        }
        HumanInterface humanInterface = this.GetComponent<HumanInterface>();
        humanInterface.ThroughPass();
    }
    
    private void ControllerPickUp(InputAction.CallbackContext ctx)
    {
        Debug.Log("in Controller pickup");
        if (tlManager.Paused)
        {
            return;
        }
        HumanInterface humanInterface = this.GetComponent<HumanInterface>();
        humanInterface.PickUp();
    }
    
    private void ControllerPutDown(InputAction.CallbackContext ctx)
    {
        Debug.Log("in Controller putdown");

        if (tlManager.Paused)
        {
            return;
        }
        HumanInterface humanInterface = this.GetComponent<HumanInterface>();
        humanInterface.PutDown();
    }
    
    private void Movement()
    {
        float horizontalInput = move.ReadValue<Vector2>().x;
        float verticalInput = move.ReadValue<Vector2>().y;
        
        HumanInterface humanInterface = this.GetComponent<HumanInterface>();
        if (!humanInterface.actionAPI.alreadyInAnimation)
        {
            animator.SetFloat("VelX", horizontalInput * 2);
            animator.SetFloat("VelZ", verticalInput * 2);
        }
        
        forceDirection += (cam.transform.up * verticalInput + cam.transform.right * horizontalInput).normalized;
        
        rb.AddForce(forceDirection * movementForce, ForceMode.Impulse);
        forceDirection = Vector3.zero;
    }

    private void Rotate()
    {
        float horizontalInput = look.ReadValue<Vector2>().x;
        float verticalInput = look.ReadValue<Vector2>().y;
        
        if (Mathf.Abs(horizontalInput) > controllerDeadzone || Mathf.Abs(verticalInput) > controllerDeadzone)
        {
            playerDirection = Vector3.right * horizontalInput + Vector3.forward * verticalInput;

            if (playerDirection.sqrMagnitude > 0.0f)
            {
                Quaternion newRotation = Quaternion.LookRotation(playerDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation,
                    gamepadRotateSmoothing * Time.deltaTime);
            }
        }
    }
}