from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Wait to observe teammate's possession and opponent pressure")
    do Wait() until λ_precondition_0(simulation(), None)
    do Speak("Move to right or left to create a pass angle for teammate")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("Wait and be ready to receive the pass from teammate")
    do Wait() until λ_precondition_1(simulation(), None)
    do Speak("Get ball possession after the teammate's pass")
    do GetBallPossession(ball)
    do Speak("Wait for opponent's choice: pressure you or teammate")
    do Wait() until λ_precondition_2(simulation(), None)
    if λ_precondition2a(simulation(), None):
        do Speak("Pass to teammate as opponent is pressuring you")
        do Pass(teammate)
    else:
        do Speak("Go for goal and attempt to score")
        do MoveTo(λ_target1()) until λ_termination1(simulation(), None)
        do Speak("Pass ball into goal")
        do Pass(goal)

A1precondition_0 = HasBallPossession({'player': 'teammate'})
A2precondition_0 = MovingTowards({'obj': 'opponent', 'ref': 'teammate'})
A1target_0 = HasPath({'passer': 'teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.16, 'std': 0.03}})
A2target_0 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 3.0, 'std': 0.6}, 'operator': 'greater_than'})
A1termination_0 = HasPath({'passer': 'teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.16, 'std': 0.03}})
A2termination_0 = DistanceTo({'from': 'Coach', 'to': 'teammate', 'max': {'avg': 6.5, 'std': 0.6}, 'operator': 'less_than'})
A1precondition_1 = HasPath({'passer': 'teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.16, 'std': 0.03}})
A2precondition_1 = HasBallPossession({'player': 'teammate'})
A1precondition_2 = HasBallPossession({'player': 'Coach'})
A2precondition_2 = MovingTowards({'obj': 'opponent', 'ref': 'Coach'})
A3precondition_2 = MovingTowards({'obj': 'opponent', 'ref': 'teammate'})
A1target_1 = HasPath({'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 0.22, 'std': 0.07}})
A2target_1 = DistanceTo({'from': 'Coach', 'to': 'goal', 'max': {'avg': 6.0, 'std': 1.0}, 'operator': 'less_than'})
A1termination_1 = HasPath({'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 0.22, 'std': 0.07}})
A2termination_1 = DistanceTo({'from': 'Coach', 'to': 'goal', 'max': {'avg': 6.0, 'std': 1.0}, 'operator': 'less_than'})

def λ_target0():
    return (A1target_0.__call__(simulation(), None) and A2target_0.__call__(simulation(), None))

def λ_termination0(scene, sample):
    return (A1termination_0.__call__(scene, sample) and A2termination_0.__call__(scene, sample))

def λ_precondition_0(scene, sample):
    return (A1precondition_0.__call__(scene, sample) and A2precondition_0.__call__(scene, sample))

def λ_precondition_1(scene, sample):
    return (A1precondition_1.__call__(scene, sample) and A2precondition_1.__call__(scene, sample))

def λ_precondition_2(scene, sample):
    return (A1precondition_2.__call__(scene, sample) and (A2precondition_2.__call__(scene, sample) or A3precondition_2.__call__(scene, sample)))

def λ_precondition2a(scene, sample):
    return (A1precondition_2.__call__(scene, sample) and A2precondition_2.__call__(scene, sample))

def λ_target1():
    return (A1target_1.__call__(simulation(), None) and A2target_1.__call__(simulation(), None))

def λ_termination1(scene, sample):
    return (A1termination_1.__call__(scene, sample) and A2termination_1.__call__(scene, sample))



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