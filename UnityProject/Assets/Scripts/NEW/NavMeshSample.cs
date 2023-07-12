using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;
public class NavMeshSample : MonoBehaviour
{
    public NavMeshAgent agent;
    public ThirdPersonCharacter character;
    public Transform Destiny;
    private void Start()
    {
        agent.updateRotation = false;

        agent.SetDestination(Destiny.position);

        StartCoroutine(Move(agent));
    }

    IEnumerator Move(NavMeshAgent agent)
    {
        while(agent.SetDestination(Destiny.position)) {
            if (agent.remainingDistance > agent.stoppingDistance)
                character.Move(agent.desiredVelocity, false, false);
            else
                character.Move(Vector3.zero, false, false);
            yield return null;
        }
    }
}


