using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager2v1 : MonoBehaviour
{
    [SerializeField] GameObject[] Players;
    [SerializeField] GameObject SoccerBall;

    private void Awake()
    {
        Players = GameObject.FindGameObjectsWithTag("Player");
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach(GameObject player in Players)
        {
            player.GetComponent<ActionLibrary>().MoveFromOnePositionToAnother(player.transform.position,SoccerBall.transform.position,true);
        }
    }
}
