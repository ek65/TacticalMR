using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Running : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;

    // Patroling
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange;

    //Attacking
    public float timeBetweenAttacks;
    bool alreadyAttacked;

    //States
    bool chasePlayerTrigger;

    private void Awake()
    {
        player = GameObject.Find("PlayerObj").transform;
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        ChasePlayer();
    }

    private void Patroling()
    {
        if (walkPointSet) Update();
    }

    private void ChasePlayer()
    {
        // make sure enemy doesn't move
        agent.isStopped = false;
        agent.SetDestination(player.position);
        transform.LookAt(player);
    }

    private void ResetAttack(){

    }

    private void AttackPlayer(){
        
    }

    private void StopEnemy(){
    
    }
}
