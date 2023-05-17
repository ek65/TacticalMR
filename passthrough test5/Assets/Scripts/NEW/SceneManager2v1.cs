using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager2v1 : MonoBehaviour
{
    public static SceneManager2v1 instance;

    [SerializeField] GameObject[] AIPlayers;

    public GameObject SoccerBall;
    public GameObject UserPlayer;
    public bool isBallPosessed = false;
    bool passed = true;
    private void Awake()
    {
        instance = this;
        AIPlayers = GameObject.FindGameObjectsWithTag("AI Player");
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach(GameObject player in AIPlayers)
        {
            player.GetComponent<ActionLibrary>().MoveFromOnePositionToAnother(player.transform.position, SoccerBall.transform.position, true);
        }
    }

    private void Update()
    {
        if (isBallPosessed && passed)
        {
            Debug.Log("Pass To teammate");
            
            passed = false;
        }
    }
}
