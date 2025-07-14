from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    # 1. Coach moves to a position to receive a pass from the teammate.
    do Speak("I'm gonna move to the side to create an angle of pass.")
    do MoveTo(λ_target_MoveToReceive()) until λ_termination_MoveToReceive(simulation(), None)

    # 2. Coach gains possession of the ball.
    do Speak("Now I have the ball, and I'm ready to act.")
    do GetBallPossession(ball)

    # 3. Decision point: Shoot immediately if conditions are met.
    if λ_precondition_CanShootImmediately(simulation(), None):
        do Speak("I have an open goal, I'm going to shoot the ball!")
        do Shoot(goal)
    else:
        # 4. If not an immediate shot, wait for teammate to get into position or get open.
        do Speak("I'll wait for my teammate to get open or move into a good position.")
        do Idle() until λ_termination_WaitForTeammate(simulation(), None)

        # 5. After waiting, pass the ball to the teammate.
        do Speak("My teammate is open, I'm passing it to my teammate!")
        do Pass(teammate)

# Constraint Instantiations
A0_termination_CoachCloseToBall = CloseTo({'obj': 'Coach', 'ref': 'ball', 'max': {'avg': 0.5, 'std': 0.0}})

A1_pre_CanShoot_CoachHasBall = HasBallPossession({'player': 'Coach'})
A1_pre_CanShoot_CloseToGoal = DistanceTo({'from': 'Coach', 'to': 'goal', 'max': {'avg': 10.0, 'std': 0.0}, 'operator': 'less_than'})
A1_pre_CanShoot_PathToGoal = HasPath({'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})

A2_term_WaitForTeammate_Moving = MovingTowards({'obj': 'teammate', 'ref': 'goal'})
A2_term_WaitForTeammate_Path = HasPath({'passer': 'Coach', 'receiver': 'teammate', 'path_width': {'avg': 1.0, 'std': 0.0}})
A2_term_WaitForTeammate_CoachHasBall = HasBallPossession({'player': 'Coach'})

# Lambda functions
def λ_target_MoveToReceive():
    """
    Target function for MoveTo: The Coach moves to the ball's current position, anticipating a pass.
    """
    return ball.position

def λ_termination_MoveToReceive(scene, sample):
    """
    Termination condition for MoveToReceive: Coach is close to the ball.
    """
    return A0_termination_CoachCloseToBall.bool(simulation())

def λ_precondition_CanShootImmediately(scene, sample):
    """
    Precondition for deciding to shoot immediately:
    Coach has the ball, is close to the goal, and has a clear path to the goal.
    """
    return (A1_pre_CanShoot_CoachHasBall.bool(simulation()) and
            A1_pre_CanShoot_CloseToGoal.bool(simulation()) and
            A1_pre_CanShoot_PathToGoal.bool(simulation()))

def λ_termination_WaitForTeammate(scene, sample):
    """
    Termination condition for Idle (waiting for teammate):
    Teammate is moving towards the goal, there is a clear pass path to the teammate,
    and the Coach still has ball possession.
    """
    return (A2_term_WaitForTeammate_Moving.bool(simulation()) and
            A2_term_WaitForTeammate_Path.bool(simulation()) and
            A2_term_WaitForTeammate_CoachHasBall.bool(simulation()))


def movesToward(player1, player2):
    dist1 = distance from player1.prevPosition to player2.prevPosition
    dist2 = distance from player1.position to player2.position
    return dist2 < dist1

behavior Follow(obj):
    while True:
        do MoveToBehavior(obj, distance = 3, status = f"Follow {obj.name}")

def pressure(player1, player2):
    """
    Returns True if player1 is pressuring player2, False otherwise.
    """
    behav = player1.gameObject.behavior.lower()
    name = player2.name.lower()
    print(f"player1: {player1.name}, player2: {player2.name}, behavior: {behav}")
    if 'follow' in behav and name in behav:
        return True
    return False

behavior opponent1Behavior():
    do Idle() until teammate.gameObject.ballPossession
    do Follow(ball) until ego.gameObject.ballPossession
    # do Uniform(Follow(ego), Follow(teammate))
    do Follow(teammate)
    # print("opponent follows ego")
    # do Follow(ego)

A = HasPath({'obj1': 'teammate', 'obj2': 'coach', 'path_width':{'avg': 2, 'std':1}})

behavior TeammateBehavior():
    passed = False
    try:
        do GetBallPossession(ball)
        do Idle()
    interrupt when (A.bool(simulation()) and not passed and self.gameObject.ballPossession):
        do Idle() for 2.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point) until MakePass({'player': 'coach'}).bool(simulation())
        do Idle() for 0.5 seconds
        do GetBallPossession(ball)
        do Shoot(goal)
        passed = True

def teammateHasBallPossession():
    for obj in simulation().objects:
        if isinstance(obj, Player) and obj.team == "blue" and obj.gameObject.ballPossession:
            return True
    return False

behavior GetBehindAndReceiveBall(player, zone): # similar logic as inzone
    do MoveToBehavior(point) until self.position.y > player.position.y + 2
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()

behavior ReceiveBall():
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()
 

teammate = new Player at (0,0), 
                with name "teammate",
                with team "blue",
                with behavior TeammateBehavior()

ball = new Ball ahead of teammate by 1

opponent = new Player ahead of teammate by 5,
                    facing toward teammate,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()

ego = new Coach behind opponent by 5, 
            facing toward teammate,
            with name "Coach",
            with team "blue",
            with behavior CoachBehavior()

goal = new Goal behind opponent by 10, facing away from ego

terminate when (ego.gameObject.stopButton)