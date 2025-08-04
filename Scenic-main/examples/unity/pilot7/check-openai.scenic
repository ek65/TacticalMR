from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 6, 'std': 0.3}, 'max': None, 'operator': 'greater_than'})
A1precondition_0 = HasBallPossession({'player': 'teammate'})
A1precondition_1 = MakePass({'player': 'teammate'})
A1precondition_2 = HasBallPossession({'player': 'Coach'})
A1precondition_3 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_4 = MovingTowards({'obj': 'teammate', 'ref': 'goal'})
A1precondition_5 = HasBallPossession({'player': 'teammate'})

A2target_1 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6, 'std': 0.3}, 'max': None, 'operator': 'greater_than'})
A2target_3 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 3, 'std': 0.2}})
A2target_4 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 3, 'std': 0.2}})

def lambda_target_0():
    # Move away from opponent by more than 6 meters to receive the pass
    return A1target_0.dist(simulation(), ego=True)

def lambda_target_1():
    # Move to be more than 6 meters from the opponent after receiving possession
    return A2target_1.dist(simulation(), ego=True)

def lambda_target_pass_to_teammate():
    # Pass only if there is a clear path to teammate, path width average 3 meters
    return A2target_3.bool(simulation())

def lambda_target_shoot_goal():
    # Shoot only if there is a clear path to goal, path width average 3 meters
    return A2target_4.bool(simulation())

def lambda_precondition_0():
    # Wait until teammate receives or gets possession
    return A1precondition_0.bool(simulation())

def lambda_precondition_1():
    # Wait until teammate makes a pass
    return A1precondition_1.bool(simulation())

def lambda_precondition_2():
    # Wait until Coach gets ball possession
    return A1precondition_2.bool(simulation())

def lambda_precondition_3():
    # Check if opponent is pressuring Coach
    return A1precondition_3.bool(simulation())

def lambda_precondition_4():
    # Wait until teammate moves towards goal
    return A1precondition_4.bool(simulation())

def lambda_precondition_5():
    # Wait until teammate receives the pass back
    return A1precondition_5.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Wait until teammate has the ball before taking action")
    do Idle() until lambda_precondition_0()
    do Speak("Move to a position at least 6 meters away from the opponent to be available for a pass")
    do MoveTo(lambda_target_0(), True)
    do Speak("Wait until teammate passes the ball to you")
    do Idle() until lambda_precondition_1()
    do Speak("Move to the ball and gain possession")
    do MoveToBallAndGetPossession()
    do Speak("Wait until you have ball possession")
    do Idle() until lambda_precondition_2()
    do Speak("Check if the opponent is pressuring you after you receive the ball")
    if lambda_precondition_3():
        do Speak("Opponent is pressuring you. Move at least 6 meters away from the opponent")
        do MoveTo(lambda_target_1(), False)
        do Speak("Wait until teammate is running towards the goal")
        do Idle() until lambda_precondition_4()
        do Speak("Make a pass back to your teammate if clear passing lane of 3 meters wide")
        if lambda_target_pass_to_teammate():
            do Pass(teammate)
            do Speak("Wait for teammate to receive the ball")
            do Idle() until lambda_precondition_5()
    else:
        do Speak("No pressure from opponent. Look for clear shooting lane to the goal, 3 meters wide")
        if lambda_target_shoot_goal():
            do Speak("Take a shot at the goal")
            do Shoot(goal)
    do Idle()

####Environment Behavior START####


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
        do GetBallPossession()
        do Idle()
    interrupt when (A.bool(simulation()) and not passed and self.gameObject.ballPossession):
        do Idle() for 2.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point) until MakePass({'player': 'coach'}).bool(simulation())
        do Idle() for 0.5 seconds
        do GetBallPossession()
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