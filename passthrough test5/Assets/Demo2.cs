using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Demo2 : MonoBehaviour
{
    /* This is not great way to do it, but having a bool check is probably best right now.
     Will be easier to port to scenic pipeline doing it this way of having a separate 
    script call functions in the player move script. */
    public MoveToSoccerBallAndTurn player1;
    public MoveToSoccerBallAndTurn player2;
    public player1Behavior player1behavior;
    public player2Behavior player2behavior;

    public enum player1Behavior
    {
        player1Idle1,
        player1MoveToBall,
        player1Idle2,
        player1KickBallToPos1,
        player1MoveToPos,
        player1WaitForBall,
        player1KickBallToPos2
    };
    
    public enum player2Behavior
    {
        player2Idle1,
        player2WaitForBall,
        player2KickBallToPos,
        player2Idle2
    };

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Player1Behavior();
        Player2Behavior();
    }

    void Player1Behavior()
    {
        if (player1.action == MoveToSoccerBallAndTurn.Action.NOT_TAKING_ACTION 
            || player1.action == MoveToSoccerBallAndTurn.Action.TAKING_RECURRING_ACTION)
        {
            switch (player1behavior)
            {
                case player1Behavior.player1Idle1:
                    StartCoroutine(player1.IdleForSec(1));
                    player1behavior = player1Behavior.player1MoveToBall;
                    break;
                case player1Behavior.player1MoveToBall:
                    player1.MoveToBallThenLook(player2.transform.position); //param takes in Vector3 where to look after
                    if (player1.distToBall == 0)
                    {
                        player1behavior = player1Behavior.player1Idle2;
                    }
                    break;
                case player1Behavior.player1Idle2:
                    StartCoroutine(player1.IdleForSec(3));
                    player1behavior = player1Behavior.player1KickBallToPos1;
                    break;
                case player1Behavior.player1KickBallToPos1:
                    player1.KickBall(player2.transform.position);
                    player1behavior = player1Behavior.player1MoveToPos;
                    break;
                case player1Behavior.player1MoveToPos:
                    player1.speed = 5; //making him faster here for debugging ease
                    Vector3 targetPos = new Vector3(0, 0, 10);
                    player1.MoveToPosThenLook(targetPos, player1.goal.position); //2nd param takes in Vector3 where to look after
                    if (player1.distToPos == 0)
                    {
                        player1behavior = player1Behavior.player1WaitForBall;
                    }
                    break;
                case player1Behavior.player1WaitForBall:
                    player1.WaitForBall();
                    if (player1.distToBall == 0)
                    {
                        player1behavior = player1Behavior.player1KickBallToPos2;
                    }
                    break;
                case player1Behavior.player1KickBallToPos2:
                    player1.KickBall(player1.goal.position);
                    break;
            }
        }

        
    }

    void Player2Behavior()
    {
        //Debug.Log("p2 dist: " + player2.distToBall);
        //Debug.Log("behavior: " + player2behavior);
        if (player2.action == MoveToSoccerBallAndTurn.Action.NOT_TAKING_ACTION 
            || player2.action == MoveToSoccerBallAndTurn.Action.TAKING_RECURRING_ACTION)
        {
            switch (player2behavior)
            {
                case player2Behavior.player2Idle1:
                    StartCoroutine(player2.IdleForSec(3));
                    player2behavior = player2Behavior.player2WaitForBall;
                    break;
                case player2Behavior.player2WaitForBall:
                    player2.WaitForBall();
                    if (player2.distToBall == 0)
                    {
                        player2behavior = player2Behavior.player2Idle2;
                    }
                    break;
                case player2Behavior.player2Idle2:
                    StartCoroutine(player2.IdleForSec(3));
                    player2behavior = player2Behavior.player2KickBallToPos;
                    break;
                case player2Behavior.player2KickBallToPos:
                    player2.KickBall(player1.transform.position);
                    break;
            }
        }
    }
    // public IEnumerator KickWait()
    // {
    //     //Debug.Log("in here");
    //     yield return new WaitForSeconds(3);
    //     player1MoveToPos = true;
    // }

    //player1 behavior
    //idle 1 second
    //MoveTo ball
    //idle 3 seconds
    //KickBallToPosition (player2)
    //idle 1 seconds
    //MoveTo (0, 0, 10)
    //WaitForBall
    //KickBallToPosition (goal)

    //player2 behavior
    //WaitForBall
    //idle 1 second
    //KickBallToPosition (0, 0, 10)
    
    //try enums
    //try have a list of actions and index through them
}