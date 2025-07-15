from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Move to the side to create a passing angle for my teammate")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("Wait until I receive the ball from my teammate")
    do Wait() until λ_precondition_0(simulation(), None)
    do Speak("Get ball possession")
    do GetBallPossession(ball)
    if λ_precondition1(simulation(), None):    # Open path to goal for self
        do Speak("I have an open goal, shoot now")
        do Shoot(goal)
    else:    # Not an open shot, pass to teammate
        do Speak("Wait for my teammate to move or create an opening")
        do Wait() until λ_precondition_2(simulation(), None)
        do Speak("Pass the ball to my teammate")
        do Pass(teammate)


# Constraints, Targets, Preconditions, Terminations

# Move to side: Farther from opponent with an angle for pass from teammate
A1target_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 5.5, 'std': 1.0}, 'max': None, 'operator': 'greater_than'})
A2target_0 = HasPath({'passer': 'teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.1, 'std': 0.04}})
def λ_target0():
    # Move to a space: far from opponent and available for pass from teammate
    return (A1target_0.bool(simulation()) and A2target_0.bool(simulation()))

def λ_termination0(scene, sample):
    # When both constraints are satisfied (i.e., moved into space)
    return λ_target0()

# Wait until Coach receives the ball from pass
A1precondition_0 = HasBallPossession({'player': 'Coach'})
def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

# If open path to goal: Coach has ball + clear shot
A1precondition1 = HasBallPossession({'player': 'Coach'})
A2precondition1 = HasPath({'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 1.8, 'std': 0.1}})
def λ_precondition1(scene, sample):
    return (A1precondition1.bool(simulation()) and A2precondition1.bool(simulation()))

# If not open, wait until teammate is open for the pass to shoot
A1precondition_2 = HasPath({'passer': 'Coach', 'receiver': 'teammate', 'path_width': {'avg': 0.09, 'std': 0.03}})
A2precondition_2 = HasBallPossession({'player': 'Coach'})
def λ_precondition_2(scene, sample):
    return (A1precondition_2.bool(simulation()) and A2precondition_2.bool(simulation()))



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