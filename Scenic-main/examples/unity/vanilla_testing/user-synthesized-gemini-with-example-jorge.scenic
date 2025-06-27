from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("My teammate is under pressure. I'll move to the side to create an open passing lane for them.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("The pass is on its way. I'm moving to control the ball.")
    do GetBallPossession()
    do Speak("I have the ball. I'm waiting for an opportunity to either pass or shoot.")
    do Idle() until λ_precondition_1_or_2(simulation(), None)
    if λ_precondition1(simulation(), None):
        do Speak("The opponent is marking me, but my teammate is open. I will make a through pass.")
        do Pass(teammate)
    elif λ_precondition2(simulation(), None):
        do Speak("I am free and have space. I will move toward the goal for a shot.")
        do MoveTo(goal) until λ_termination2(simulation(), None)
        do Speak("I'm in a good position to score. I am taking the shot now!")
        do Shoot(goal)

A1termination_0 = MakePass({'player': 'teammate'})
A1precondition_1 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A2precondition_1 = MovingTowards({'obj': 'teammate', 'ref': 'goal'})
A3precondition_1 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 1.5, 'std': 0.3}})
A1precondition_2 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1termination_2 = HasBallPossession({'player': 'Coach'})
A2termination_2 = DistanceTo({'from': 'Coach', 'to': 'goal', 'operator': 'less_than', 'max': {'avg': 9.0, 'std': 1.0}})
A3termination_2 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.5}})

def λ_target0():
    return (Point at Range(-7, -5) @ Range(5, 7)) or (Point at Range(5, 7) @ Range(5, 7))

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation()) and A2precondition_1.bool(simulation()) and A3precondition_1.bool(simulation())

def λ_precondition2(scene, sample):
    return not A1precondition_2.bool(simulation())

def λ_precondition_1_or_2(scene, sample):
    return λ_precondition1(scene, sample) or λ_precondition2(scene, sample)

def λ_termination2(scene, sample):
    return A1termination_2.bool(simulation()) and A2termination_2.bool(simulation()) and A3termination_2.bool(simulation())


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