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
    private float gamepadRotateSmoothing = 500f;
    private Camera cam;
    private Animator animator;
    private KeyboardInput keyboardInput;
    private ExitScenario exitScenario;
    private TimelineManager tlManager;

    private void Awake()
    {
        inputSystem = new InputSystem();
        cam = Camera.main;
        animator = GetComponent<Animator>();
        keyboardInput = GameObject.FindGameObjectWithTag("keyboard").GetComponent<KeyboardInput>();
        exitScenario = this.GetComponent<ExitScenario>();
        tlManager = GameObject.FindGameObjectWithTag("TimelineManager").GetComponent<TimelineManager>();
    }

    private void OnEnable()
    {
        move = inputSystem.PlayerControls.Move;
        look = inputSystem.PlayerControls.Look;
        inputSystem.PlayerControls.Pause.performed += ControllerPause;
        inputSystem.PlayerControls.Restart.performed += ControllerRestart;
        inputSystem.PlayerControls.Segment.performed += ControllerSegment;
        inputSystem.PlayerControls.Enable();
    }
    
    private void OnDisable()
    {
        inputSystem.PlayerControls.Pause.performed -= ControllerPause;
        inputSystem.PlayerControls.Restart.performed -= ControllerRestart;
        inputSystem.PlayerControls.Segment.performed -= ControllerSegment;
        inputSystem.PlayerControls.Disable();
    }

    private void FixedUpdate()
    {
        if (tlManager.Paused)
        {
            return;
        }
        Movement();
        Rotate();
    }

    private void ControllerPause(InputAction.CallbackContext ctx)
    {
        keyboardInput.HandlePause();
    }

    private void ControllerRestart(InputAction.CallbackContext ctx)
    {
        exitScenario.EndScenario();
    }
    
    private void ControllerSegment(InputAction.CallbackContext ctx)
    {
        keyboardInput.HandleSegment();
    }
    
    private void Movement()
    {
        float horizontalInput = move.ReadValue<Vector2>().x;
        float verticalInput = move.ReadValue<Vector2>().y;
        
        animator.SetFloat("VelX", horizontalInput * 2);
        animator.SetFloat("VelZ", verticalInput * 2);

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
            Vector3 playerDirection = Vector3.right * horizontalInput + Vector3.forward * verticalInput;

            if (playerDirection.sqrMagnitude > 0.0f)
            {
                Quaternion newRotation = Quaternion.LookRotation(playerDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation,
                    gamepadRotateSmoothing * Time.deltaTime);
            }
        }
    }
}
