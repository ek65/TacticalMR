using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Demo3 : MonoBehaviour
{
    /* This is not great way to do it, but having a bool check is probably best right now.
     Will be easier to port to scenic pipeline doing it this way of having a separate 
    script call functions in the player move script. */
    public MoveToSoccerBallAndTurn player1;
    public MoveToSoccerBallAndTurn player2;
    public MoveToSoccerBallAndTurn player3;
    public player1Behavior player1behavior;
    public player2Behavior player2behavior;
    public player3Behavior player3behavior;

    public enum player1Behavior
    {
        player1Idle1,
        player1DribbleTo,
        player1KickBallToPos
    };
    
    public enum player2Behavior
    {
        player2Idle1,
        player2MoveToPos,
        player2WaitForBall,
        player2KickBallToPos
    };
    
    //teammate
    public enum player3Behavior
    {
        player3Idle1,
        player3MoveToPos,
        player3Idle2
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
        Player3Behavior();
    }

    void Player1Behavior()
    {
        if (player1.action == MoveToSoccerBallAndTurn.Action.NOT_TAKING_ACTION 
            || player1.action == MoveToSoccerBallAndTurn.Action.TAKING_RECURRING_ACTION)
        {
            switch (player1behavior)
            {
                case player1Behavior.player1Idle1:
                    StartCoroutine(player1.IdleForSec(3));
                    player1behavior = player1Behavior.player1DribbleTo;
                    break;
                case player1Behavior.player1DribbleTo:
                    Vector3 targetPos = new Vector3(-10, 0, 10);
                    player1.TravelWithBall(targetPos, player2.transform.position);
                    if (player1.distToPos == 0)
                    {
                        player1behavior = player1Behavior.player1KickBallToPos;
                    }
                    break;
                case player1Behavior.player1KickBallToPos:
                    player1.KickBall(player2.transform.position);
                    break;
            }
        }

        
    }

    void Player2Behavior()
    {
        if (player2.action == MoveToSoccerBallAndTurn.Action.NOT_TAKING_ACTION 
            || player2.action == MoveToSoccerBallAndTurn.Action.TAKING_RECURRING_ACTION)
        {
            switch (player2behavior)
            {
                case player2Behavior.player2Idle1:
                    StartCoroutine(player2.IdleForSec(1.5f));
                    player2behavior = player2Behavior.player2MoveToPos;
                    break;
                case player2Behavior.player2MoveToPos:
                    Vector3 targetPos = new Vector3(3, 0, 5);
                    player2.MoveToPosThenLook(targetPos, player2.goal.transform.position); //2nd param takes in Vector3 where to look after
                    if (player2.distToPos == 0)
                    {
                        player2behavior = player2Behavior.player2WaitForBall;
                    }
                    break;
                case player2Behavior.player2WaitForBall:
                    player2.WaitForBall();
                    if (player2.distToBall <= 0.1f)
                    {
                        player2behavior = player2Behavior.player2KickBallToPos;
                    }
                    break;
                case player2Behavior.player2KickBallToPos:
                    player2.KickBall(player2.goal.position);
                    break;
            }
        }
    }


    void Player3Behavior()
    {
        if (player3.action == MoveToSoccerBallAndTurn.Action.NOT_TAKING_ACTION 
            || player3.action == MoveToSoccerBallAndTurn.Action.TAKING_RECURRING_ACTION)
        {
            switch (player3behavior)
            {
                case player3Behavior.player3Idle1:
                    StartCoroutine(player3.IdleForSec(2));
                    player3behavior = player3Behavior.player3MoveToPos;
                    break;
                case player3Behavior.player3MoveToPos: //TODO: make function that actually follows ball; right now its just going to a preset position
                    Vector3 targetPos = new Vector3(-10, 0, 5);
                    player3.MoveToPosThenLook(targetPos, player3.ball.transform.position); //2nd param takes in Vector3 where to look after
                    if (player3.distToPos == 0)
                    {
                        player3behavior = player3Behavior.player3Idle2;
                    }
                    break;
            }
        }
    }
}