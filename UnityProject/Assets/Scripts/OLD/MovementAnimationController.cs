using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementAnimationController : MonoBehaviour
{
    //Create Instance
    public static MovementAnimationController intance;

    Animator animator;
    float velx = 0.0f;  // sideways direction, w.r.t local coordinates
    float velz = 0.0f;  // forward direction, w.r.t local coordinates

    public float acceleration = 2.0f;
    public float deceleration = 2.0f;

    public float maxWalkVelocity = 0.75f;
    public float maxRunVelocity = 2.0f;

    public float translateSpeedFactor = 1.5f;

    bool forwardKey;
    bool leftKey;
    bool rightKey;
    bool runKey;
    float currMaxVelocity;

    public void Awake()
    {
        intance = this;
    }
    public bool isTranslationAllowed = true;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // get key input
        forwardKey = Input.GetKey(KeyCode.W);
        leftKey = Input.GetKey(KeyCode.A);
        rightKey = Input.GetKey(KeyCode.D);
        runKey = Input.GetKey(KeyCode.LeftShift);


        currMaxVelocity = runKey ? maxRunVelocity : maxWalkVelocity;

        ChangeVelocity();
        ResetVelocity();
        LockVelocity();

        if (isTranslationAllowed)
            TranslateCharacter();

        animator.SetFloat("VelX", velx);
        animator.SetFloat("VelZ", velz);
    }

    void ChangeVelocity()
    {
        // increase velocity in forward direction
        if (forwardKey && velz < currMaxVelocity)
        {
            velz += Time.deltaTime * acceleration;
        }
        // increase velocity in left direction
        if (leftKey && velx > -currMaxVelocity)
        {
            velx -= Time.deltaTime * acceleration;
        }
        // increase velocity in right direction
        if (rightKey && velx < currMaxVelocity)
        {
            velx += Time.deltaTime * acceleration;
        }

        // bring down forward velocity
        if (!forwardKey && velz > 0.0f)
        {
            velz -= Time.deltaTime * deceleration;
        }
        // bring down left velocity
        if (!leftKey && velx < 0.0f)
        {
            velx += Time.deltaTime * deceleration;
        }
        // bring down right velocity
        if (!rightKey && velx > 0.0f)
        {
            velx -= Time.deltaTime * deceleration;
        }
    }

    void ResetVelocity()
    {
        // reset forward velocity
        if (!forwardKey && velz < 0.0f)
        {
            velz = 0.0f;
        }
        // reset sideway velocity
        if (!leftKey && !rightKey && velx != 0.0f && (velx > -0.05f && velx < 0.05f))
        {
            velx = 0.0f;
        }
    }

    void LockVelocity()
    {
        // lock forward
        if (forwardKey && runKey && velz > currMaxVelocity)
        {
            velz = currMaxVelocity;     // locking velocity at max run velocity
        }
        else if (forwardKey && velz > currMaxVelocity)
        {
            velz -= Time.deltaTime * deceleration;  // decelerate to max walk velocity
            if (velz > currMaxVelocity && velz < (currMaxVelocity + 0.05f))
            {
                velz = currMaxVelocity;     // round off to max walk velocity, prevents jitter
            }
        }
        else if (forwardKey && velz < currMaxVelocity && velz > (currMaxVelocity - 0.05f))
        {
            velz = currMaxVelocity;     // round off to max walk velocity
        }

        // lock left
        if (leftKey && runKey && velx < -currMaxVelocity)
        {
            velx = -currMaxVelocity;     // locking velocity at max run velocity
        }
        else if (leftKey && velx < -currMaxVelocity)
        {
            velx += Time.deltaTime * deceleration;  // decelerate to max walk velocity
            if (velx < -currMaxVelocity && velx > (-currMaxVelocity - 0.05f))
            {
                velx = -currMaxVelocity;     // round off to max walk velocity, prevents jitter
            }
        }
        else if (leftKey && velx > -currMaxVelocity && velx < (-currMaxVelocity + 0.05f))
        {
            velx = -currMaxVelocity;     // round off to max walk velocity
        }

        // lock right
        if (rightKey && runKey && velx > currMaxVelocity)
        {
            velx = currMaxVelocity;     // locking velocity at max run velocity
        }
        else if (rightKey && velx > currMaxVelocity)
        {
            velx -= Time.deltaTime * deceleration;  // decelerate to max walk velocity
            if (velx > currMaxVelocity && velx < (currMaxVelocity + 0.05f))
            {
                velx = currMaxVelocity;     // round off to max walk velocity, prevents jitter
            }
        }
        else if (rightKey && velx < currMaxVelocity && velx > (currMaxVelocity - 0.05f))
        {
            velx = currMaxVelocity;     // round off to max walk velocity
        }
    }

    public Vector3 movement;
    void TranslateCharacter()
    {
        movement = Vector3.zero;
        if (forwardKey)
        {
            movement += transform.forward;
            movement = new Vector3(velx * movement.x, 0, velz * movement.z);
        }
        if (rightKey)
        {
            movement += transform.right;
            movement = new Vector3(velx * movement.x, 0, velz * movement.z);
        }
        if (leftKey)
        {
            movement -= transform.right;
            movement = new Vector3(velx * movement.x * -1.0f, 0, velz * movement.z * -1.0f);
        }

        if (runKey)
            translateSpeedFactor = 1.5f;

        transform.Translate(movement * Time.deltaTime * translateSpeedFactor);
    }
}
