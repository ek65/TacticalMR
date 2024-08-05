using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class Rewindable : MonoBehaviour
{
    [SerializeField] bool RewindTransform = true;
    [SerializeField] public bool Pausible = true;
    private Vector3 savedVelocity;
    private Vector3 savedPosition;
    private Vector3 savedAngularVelocity;
    private RigidbodyConstraints savedConstraints;
    private bool isKinematic;
    private bool Paused = false;
 
    public void ApplySnippet(MomentSnippet snippet)
    {
        transform.position = snippet.position;
        transform.rotation = snippet.rotation;
    }
    public void Update()
    {
        if (Paused)
        {
            transform.position = savedPosition;
        }
    }
    //reason for abstraction: generic solution that works both for character and soccer ball
    public void Freeze()
    {
        try
        {
            FreezePhysics();
        }
        catch(Exception e)
        {
            Debug.Log("Cant freeze physics lol");
        }
        try
        {
            FreezeAnimation();
        }
        catch (Exception e)
        {
            Debug.Log("Cant freeze animation lol");
        }
        Paused = true;

        
    }
    public void Unfreeze()
    {
        try
        {
            UnfreezePhysics();
        }
        catch (Exception e)
        {
            Debug.Log("Cant unfreeze physics lol");
        }
        try
        {
            UnfreezeAnimation();
        }
        catch (Exception e)
        {
            Debug.Log("Cant unfreeze animation lol");
        }
        Paused = false;
    }
    private void FreezePhysics()
    {
        Rigidbody r = GetComponent<Rigidbody>();

        savedVelocity = r.velocity;
        r.velocity = Vector3.zero;
        isKinematic = r.isKinematic;
        r.isKinematic = true;
        savedAngularVelocity = r.angularVelocity;
        savedConstraints = r.constraints;

        savedPosition = transform.position;
        
        r.constraints = RigidbodyConstraints.FreezePosition;
        r.constraints = RigidbodyConstraints.FreezeRotation;

    }
    private void UnfreezePhysics()
    {
        Rigidbody r = GetComponent<Rigidbody>();
        
        r.constraints = savedConstraints;
        
        r.velocity = savedVelocity;
        r.isKinematic = isKinematic;
        r.angularVelocity = savedAngularVelocity;


    }
    //TODO: need to be changed if u switch to Motion Matching systm
    private void FreezeAnimation()
    {
        GetComponent<Animator>().enabled = false;
        GetComponent<RichAI>().enabled = false;
    }
    private void UnfreezeAnimation()
    {
        GetComponent<Animator>().enabled = true;
        GetComponent<RichAI>().enabled = true;
    }
}
