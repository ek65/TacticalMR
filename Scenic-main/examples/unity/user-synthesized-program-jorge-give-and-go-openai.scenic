from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
behavior CoachBehavior():
    do Speak("wait doing nothing for 1 second")
    do Idle() for 1 seconds
    do Speak("move close to an opponent within about 7 meters until path to teammate is clear")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("wait until teammate makes a pass and close to opponent within about 7 meters")
    do Idle() until λ_precondition_5(simulation(), None)
    do Speak("get possession of the ball")
    do GetBallPossession(ball)
    do Speak("wait until you have the ball")
    do Idle() until λ_precondition_6(simulation(), None)
    do Speak("move away from opponent beyond 6 meters until path to teammate or goal is clear")
    do MoveTo(λ_target2()) until λ_termination2(simulation(), None)
    do Speak("wait until there's a path to teammate or a clear shot at goal")
    do Idle() until λ_precondition_7_8(simulation(), None)
    do Speak("decide to pass to teammate if path is clear; else shoot at goal")
    if λ_precondition7(simulation(), None):
        do Pass(teammate)
    else:
        do Shoot(goal)
A1termination_0 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.020621956938491535, 'std': 0.022423568786138517}})
A1target_0 = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': 7.2387403381425335, 'std': 0.22515968377545553}})
A1termination_2 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.019687875229194313, 'std': 0.018061319655659516}})
A2termination_2 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1target_2 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 5.8960260415855075, 'std': 0.2032253606804975}, 'max': None, 'operator': 'greater_than'})
A1precondition_5 = MakePass({'player': 'teammate'})
A2precondition_5 = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': 7.237276146119641, 'std': 0.22546616036485115}})
A1precondition_6 = HasBallPossession({'player': 'Coach'})
A1precondition_7 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.04124484446505296, 'std': 0.0}})
A1precondition_8 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_5 = MakePass({'player': 'teammate'})
A2precondition_5 = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': 7.237276146119641, 'std': 0.22546616036485115}})
A1precondition_6 = HasBallPossession({'player': 'Coach'})
A1precondition_7 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.04124484446505296, 'std': 0.0}})
A1precondition_8 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
def λ_target0():
    return ~(A1target_0.dist(simulation(), ego=True))

def λ_target2():
    return A1target_2.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_termination1(scene, sample):
    return True

def λ_termination2(scene, sample):
    return A1termination_2.bool(simulation()) | A2termination_2.bool(simulation())

def λ_termination3(scene, sample):
    return True

def λ_termination4(scene, sample):
    return True

def λ_precondition5(scene, sample):
    return (A1precondition_5.bool(simulation()) & ~(A2precondition_5.bool(simulation())))

def λ_precondition_5(scene, sample):
    return λ_precondition5(simulation(), sample)

def λ_precondition6(scene, sample):
    return A1precondition_6.bool(simulation())

def λ_precondition_6(scene, sample):
    return λ_precondition6(simulation(), sample)

def λ_precondition7(scene, sample):
    return A1precondition_7.bool(simulation())

def λ_precondition8(scene, sample):
    return A1precondition_8.bool(simulation())

def λ_precondition_7_8(scene, sample):
    return λ_precondition7(simulation(), sample) or λ_precondition8(simulation(), sample)




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