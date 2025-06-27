from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Wait while teammate is under pressure and has ball possession.")
    do Idle() until λ_precondition0(simulation(), None)
    do Speak("Move to a wide, open space to create a passing angle.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("Wait for the pass and get ball possession.")
    do Idle() until λ_precondition1(simulation(), None)
    do Speak("Move to the ball and get possession.")
    do GetBallPossession(ball)
    do Speak("Decide: if opponent pressures, pass to teammate; otherwise, go for goal.")
    if λ_precondition2(simulation(), None):
        do Speak("Opponent is pressuring; move further from opponent.")
        do MoveTo(λ_target1()) until λ_termination1(simulation(), None)
        do Speak("Prepare to pass to teammate making run towards goal.")
        do Idle() until λ_precondition3(simulation(), None)
        do Speak("Pass to your teammate for a scoring chance.")
        do Pass(teammate)
    else:
        do Speak("No pressure, move up field towards goal for shooting opportunity.")
        do MoveTo(λ_target2()) until λ_termination2(simulation(), None)
        do Speak("Wait until close to goal for a clear shot.")
        do Idle() until λ_precondition4(simulation(), None)
        do Speak("Shoot at goal!")
        do Shoot(goal)

A1target_0 = HasPath({'passer': 'teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.15, 'std': 0.04}})
A2target_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 4.5, 'std': 1.1}, 'max': None, 'operator': 'greater_than'})
A1termination_0 = HasPath({'passer': 'teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.15, 'std': 0.04}})
A2termination_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 4.5, 'std': 1.1}, 'max': None, 'operator': 'greater_than'})
A1target_1 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.0, 'std': 0.5}, 'max': None, 'operator': 'greater_than'})
A1termination_1 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.0, 'std': 0.5}, 'max': None, 'operator': 'greater_than'})
A1target_2 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 6.0, 'std': 0.5}, 'operator': 'less_than'})
A2target_2 = HasPath({'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 1.2, 'std': 0.1}})
A1termination_2 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 6.0, 'std': 0.5}, 'operator': 'less_than'})
A2termination_2 = HasBallPossession({'player': 'Coach'})
A1precondition_0 = HasBallPossession({'player': 'teammate'})
A2precondition_0 = DistanceTo({'from': 'opponent', 'to': 'teammate', 'min': None, 'max': {'avg': 2.8, 'std': 0.7}, 'operator': 'less_than'})
A1precondition_1 = HasPath({'passer': 'teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.15, 'std': 0.04}})
A2precondition_1 = HasBallPossession({'player': 'Coach'})
A1precondition_2 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': None, 'max': {'avg': 2.7, 'std': 0.8}, 'operator': 'less_than'})
A1precondition_3 = MovingTowards({'obj': 'teammate', 'ref': 'goal'})
A1precondition_4 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.4, 'std': 0.35}, 'operator': 'less_than'})
A2precondition_4 = HasBallPossession({'player': 'Coach'})

def λ_target0():
    return A1target_0.bool(simulation()) and A2target_0.bool(simulation())

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation()) and A2termination_0.bool(simulation())

def λ_target1():
    return A1target_1.bool(simulation())

def λ_termination1(scene, sample):
    return A1termination_1.bool(simulation())

def λ_target2():
    return A1target_2.bool(simulation()) and A2target_2.bool(simulation())

def λ_termination2(scene, sample):
    return A1termination_2.bool(simulation()) and A2termination_2.bool(simulation())

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(simulation()) and A2precondition_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation()) or A2precondition_1.bool(simulation())

def λ_precondition2(scene, sample):
    return A1precondition_2.bool(simulation())

def λ_precondition3(scene, sample):
    return A1precondition_3.bool(simulation())

def λ_precondition4(scene, sample):
    return A1precondition_4.bool(simulation()) and A2precondition_4.bool(simulation())


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