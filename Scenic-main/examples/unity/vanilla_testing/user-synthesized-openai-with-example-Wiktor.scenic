from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Wait for the teammate to receive the ball.")
    do Idle() until λ_precondition0(simulation(), None)
    do Speak("Opponent is pressuring your teammate, move right to open a passing angle.")
    #do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do MoveTo(λ_target_move_side()) until λ_termination_side(simulation(), None)
    do Speak("Hold your position and wait for the pass from your teammate.")
    do Idle() until λ_precondition1(simulation(), None)
    do Speak("Get possession when the pass is made.")
    do GetBallPossession(ball)
    do Speak("Wait and read the opponent's move after you get the ball.")
    do Idle() until λ_precondition2(simulation(), None)
    if λ_precondition3(simulation(), None):
        do Speak("Opponent is staying with teammate, move to score a goal.")
        do MoveTo(λ_target1()) until λ_termination1(simulation(), None)
        do Speak("Take a shot on goal.")
        do Shoot(goal)
    else:
        do Speak("Opponent is pressuring you, pass back to your teammate.")
        do Pass(teammate)

A1target_0 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 0.35, 'std': 0.09}})
A1termination_0 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 0.35, 'std': 0.09}})
A1precondition_0 = HasBallPossession({'player': 'teammate'})
A1precondition_1 = MakePass({'player': 'teammate'})
A1precondition_2 = HasBallPossession({'player': 'Coach'})
A1precondition_3 = Pressure({'player1': 'opponent', 'player2': 'teammate'})
A1target_1 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 0.37, 'std': 0.11}})
A1termination_1 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 0.37, 'std': 0.11}})

A1target_move_side = HorizontalRelation({'obj': 'Coach', 'relation': 'right', 'ref': 'teammate', 'horizontal_threshold': {'avg': 5.0, 'std': 0.2}})
A1termination_side = HorizontalRelation({'obj': 'Coach', 'relation': 'right', 'ref': 'teammate', 'horizontal_threshold': {'avg': 5.0, 'std': 0.2}})

def λ_target_move_side():
    return A1target_move_side.dist(simulation(), ego=True)

def λ_termination_side(scene, sample):
    return A1termination_side.bool(simulation())

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition2(scene, sample):
    return A1precondition_2.bool(simulation())

def λ_precondition3(scene, sample):
    return not A1precondition_3.bool(simulation())

def λ_target1():
    return A1target_1.dist(simulation(), ego=True)

def λ_termination1(scene, sample):
    return A1termination_1.bool(simulation())


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

A = HasPath({'obj1': 'teammate', 'obj2': 'coach', 'path_width':{'avg': 10, 'std':5}})

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