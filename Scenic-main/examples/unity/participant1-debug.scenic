from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
behavior CoachBehavior():
    do Speak("wait and do nothing for 1 second")
    do Idle() for 1 seconds
    do Speak("move to the zone labeled C5 until you reach it")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("wait until there is an opportunity to pass")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("go to the ball and take control of it")
    do GetBallPossession(ball)
    do Speak("idle until a clear path opens to goal or pressure prevents it")
    do Idle() until λ_precondition_1_2(simulation(), None)
    if λ_precondition1(simulation(), None):
        do Speak("shoot the ball towards the goal")
        do Shoot(goal)
    else:
        do Speak("continue waiting until specific conditions around teammate and goal are met")
        do Idle() until λ_termination3(simulation(), None)
        do Speak("wait until in a position to pass to teammate")
        do Idle() until λ_precondition_3(simulation(), None)
        do Speak("pass the ball to a teammate")
        do Pass(teammate)
A1termination_0 = InZone({'obj': 'coach', 'zone': ['C5', 'C5']})
A1target_0 = InZone({'obj': 'coach', 'zone': ['D2', 'D2']})
A1termination_3 = CloseTo({'obj': 'teammate', 'ref': 'goal', 'max': {'avg': 17.61382602476891, 'std': 0.04837727970416239}})
A2termination_3 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.060529360381963285, 'std': 0.05214726348522526}})
A3termination_3 = Pressure({'player1': 'opponent', 'player2': 'teammate'})
A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_2 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_3 = CloseTo({'obj': 'teammate', 'ref': 'goal', 'max': {'avg': 17.61382602476891, 'std': 0.04837727970416239}})
A2precondition_3 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.060529360381963285, 'std': 0.05214726348522526}})
A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_2 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_3 = CloseTo({'obj': 'teammate', 'ref': 'goal', 'max': {'avg': 17.61382602476891, 'std': 0.04837727970416239}})
A2precondition_3 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.060529360381963285, 'std': 0.05214726348522526}})
def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_termination1(scene, sample):
    return 

def λ_termination2(scene, sample):
    return 

def λ_termination3(scene, sample):
    return (A1termination_3.bool(simulation()) & A2termination_3.bool(simulation()) & A3termination_3.bool(simulation()))

def λ_termination4(scene, sample):
    return 

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_0(scene, sample):
    return λ_precondition0(simulation(), sample)

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition2(scene, sample):
    return ~(A1precondition_2.bool(simulation()))

def λ_precondition_1_2(scene, sample):
    return λ_precondition1(simulation(), sample) or λ_precondition2(simulation(), sample)

def λ_precondition3(scene, sample):
    return (A1precondition_3.bool(simulation()) & A2precondition_3.bool(simulation()))

def λ_precondition_3(scene, sample):
    return λ_precondition3(simulation(), sample)




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
        print("DEBUG: TeammateBehavior started.")

        do GetBallPossession(ball)
        print(f"DEBUG: [After GetBallPossession] Ball possession: {self.gameObject.ballPossession}")

        do Idle() for 0.5 seconds
        print("DEBUG: [Idle] Inserted delay after GetBallPossession")

        # ✅ Print each condition individually before interrupt
        hasPath = A.bool(simulation())
        possession = self.gameObject.ballPossession
        print(f"DEBUG: [Interrupt check] A.bool: {hasPath}")
        print(f"DEBUG: [Interrupt check] not passed: {not passed}")
        print(f"DEBUG: [Interrupt check] possession: {possession}")

        do Idle()
        print("DEBUG: [Idle] Waiting for interrupt...")

    interrupt when (A.bool(simulation()) and not passed and self.gameObject.ballPossession):
        print(f"DEBUG: [INTERRUPT TRIGGERED] A.bool: {A.bool(simulation())}, passed: {passed}, possession: {self.gameObject.ballPossession}")

        do Idle() for 2.5 seconds
        print("DEBUG: [Interrupt] Preparing to pass.")

        print("DEBUG: [Interrupt] Attempting to pass to Coach.")
        do Pass(ego, slow=False)
        print("DEBUG: [Interrupt] Pass executed.")

        do Idle() for 0.5 seconds
        print("DEBUG: [Interrupt] Short wait after pass.")

        take StopAction()
        print("DEBUG: [Interrupt] Stopped movement.")

        point = new Point at (0,10,0)
        print(f"DEBUG: [Interrupt] Moving to point {point} until Coach makes pass.")

        do MoveToBehavior(point) until MakePass({'player': 'coach'}).bool(simulation())
        print("DEBUG: [Interrupt] Reached point or detected Coach made pass.")

        do Idle() for 0.5 seconds
        print("DEBUG: [Interrupt] Waiting briefly after movement.")

        print("DEBUG: [Interrupt] Trying to regain ball possession.")
        do GetBallPossession(ball)
        print(f"DEBUG: [Interrupt] Ball possession after re-get: {self.gameObject.ballPossession}")

        print("DEBUG: [Interrupt] Attempting to shoot at goal.")
        do Shoot(goal)
        print("DEBUG: [Interrupt] Shot executed.")

        passed = True
        print("DEBUG: [Interrupt] Marked 'passed = True'. Interrupt sequence complete.")

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