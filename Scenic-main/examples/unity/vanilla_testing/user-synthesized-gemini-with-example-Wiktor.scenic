from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("My teammate is pressured. I need to move to create a passing lane.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("I'm in a good position now. I'll wait for my teammate to pass the ball.")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("The pass is on its way. I'll get the ball.")
    do GetBallPossession(ball)
    do Speak("I have the ball. I will wait to see how the opponent reacts.")
    do Idle() until λ_precondition_1_or_2(simulation(), None)
    if λ_precondition1(simulation(), None):
        do Speak("The opponent is pressuring me, so I'll pass the ball back to my open teammate.")
        do Pass(teammate)
    else:
        do Speak("The opponent is staying on my teammate, so I'll move toward the goal to shoot.")
        do MoveTo(goal) until λ_termination2(simulation(), None)
        do Speak("I am in a good position to score. I will shoot the ball now.")
        do Shoot(goal)

A1target_0 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 1.0, 'std': 0.2}})
A1termination_0 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 1.0, 'std': 0.2}})
A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1termination_2 = DistanceTo({'from': 'Coach', 'to': 'goal', 'operator': 'less_than', 'max': {'avg': 9.5, 'std': 1.0}})
A2termination_2 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 1.5, 'std': 0.5}})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition2(scene, sample):
    return not A1precondition_1.bool(simulation())

def λ_precondition_1_or_2(scene, sample):
    return λ_precondition1(scene, sample) or λ_precondition2(scene, sample)

def λ_termination2(scene, sample):
    return A1termination_2.bool(simulation()) and A2termination_2.bool(simulation())


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