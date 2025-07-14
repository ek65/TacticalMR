from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("wait for 1 second doing nothing")
    do Idle() for 1 seconds
    do Speak("move away from opponent by more than 6 meters with a clear path to teammate")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("wait until opponent pressures you or you have a clear path to pass")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("move to ball and get possession")
    do GetBallPossession(ball)
    do Speak("wait until opponent pressures you, you have a clear path, or not pressured")
    do Idle() until λ_precondition_1_2_3(simulation(), None)
    if λ_precondition1(simulation(), None):
        do Speak("move more than 6 meters away from opponent")
        do MoveTo(λ_target2()) until λ_termination2(simulation(), None)
        do Speak("wait until teammate moves towards goal")
        do Idle() until λ_precondition_4(simulation(), None)
        do Speak("pass the ball to teammate")
        do Pass(teammate)
    if λ_precondition2(simulation(), None):
        do Speak("pass the ball to teammate")
        do Pass(teammate)
    else:
        do Speak("move close to goal less than 5 meters with possession")
        do MoveTo(λ_target5()) until λ_termination5(simulation(), None)
        do Speak("wait until possession and near to goal")
        do Idle() until λ_precondition_5(simulation(), None)
        do Speak("take a shot towards the goal")
        do Shoot(goal)

A1termination_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 6.399477695297064, 'std': 0.8416729364595561}, 'max': None, 'operator': 'greater_than'})
A2termination_0 = HasPath({'passer': 'teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.043342957402023236, 'std': 0.04454568506693753}})
A1target_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 6.399477695297064, 'std': 0.8416729364595561}, 'max': None, 'operator': 'greater_than'})
A1termination_2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.172259899611368, 'std': 0.0}, 'max': None, 'operator': 'greater_than'})
A1target_2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.172259899611368, 'std': 0.0}, 'max': None, 'operator': 'greater_than'})
A1termination_5 = HasBallPossession({'player': 'Coach'})
A2termination_5 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.025909493366715, 'std': 0.015410097852564864}, 'operator': 'less_than'})
A3termination_5 = HasPath({'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1target_5 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.025909493366715, 'std': 0.015410097852564864}, 'operator': 'less_than'})
A2target_5 = CloseTo({'obj': 'Coach', 'ref': 'ball', 'max': {'avg': 11.941602839093648, 'std': 0.01539784416917822}})
A1precondition_0 = HasPath({'passer': 'teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.043342957402023236, 'std': 0.04454568506693753}})
A1precondition_1 = MovingTowards({'obj': 'opponent', 'ref': 'Coach'})
A2precondition_1 = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': 7.551545991065808, 'std': 0.0}})
A1precondition_2 = MovingTowards({'obj': 'opponent', 'ref': 'Coach'})
A2precondition_2 = HasPath({'passer': 'Coach', 'receiver': 'teammate', 'path_width': {'avg': 0.11392247846714056, 'std': 0.0}})
A1precondition_3 = MovingTowards({'obj': 'opponent', 'ref': 'teammate'})
A1precondition_4 = MovingTowards({'obj': 'teammate', 'ref': 'goal'})
A1precondition_5 = HasBallPossession({'player': 'Coach'})
A2precondition_5 = DistanceTo({'from': 'goal', 'to': 'Coach', 'min': None, 'max': {'avg': 5.025909493366715, 'std': 0.015410097852564864}, 'operator': 'less_than'})
A3precondition_5 = HasPath({'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_target2():
    return A1target_2.dist(simulation(), ego=True)

def λ_target5():
    return (A1target_5.bool(simulation()) and A2target_5.bool(simulation()))

def λ_termination0(scene, sample):
    return (A1termination_0.bool(scene, sample) and A2termination_0.bool(scene, sample))

def λ_termination1(scene, sample):
    return False # Not used, but keeping for consistency if it was in the original hidden example

def λ_termination2(scene, sample):
    return A1termination_2.bool(scene, sample)

def λ_termination4(scene, sample):
    return False # Not used, but keeping for consistency if it was in the original hidden example

def λ_termination3(scene, sample):
    return False # Not used, but keeping for consistency if it was in the original hidden example

def λ_termination5(scene, sample):
    return (A1termination_5.bool(scene, sample) and A2termination_5.bool(scene, sample) and A3termination_5.bool(scene, sample))

def λ_termination6(scene, sample):
    return False # Not used, but keeping for consistency if it was in the original hidden example

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(scene, sample)

def λ_precondition_0(scene, sample):
    return λ_precondition0(scene, sample)

def λ_precondition1(scene, sample):
    return (A1precondition_1.bool(scene, sample) and ~(A2precondition_1.bool(scene, sample)))

def λ_precondition2(scene, sample):
    return (A1precondition_2.bool(scene, sample) and A2precondition_2.bool(scene, sample))

def λ_precondition3(scene, sample):
    return ~(A1precondition_3.bool(scene, sample))

def λ_precondition_1_2_3(scene, sample):
    return λ_precondition1(scene, sample) or λ_precondition2(scene, sample) or λ_precondition3(scene, sample)

def λ_precondition4(scene, sample):
    return A1precondition_4.bool(scene, sample)

def λ_precondition_4(scene, sample):
    return λ_precondition4(scene, sample)

def λ_precondition5(scene, sample):
    return (A1precondition_5.bool(scene, sample) and A2precondition_5.bool(scene, sample) and A3precondition_5.bool(scene, sample))

def λ_precondition_5(scene, sample):
    return λ_precondition5(scene, sample)


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