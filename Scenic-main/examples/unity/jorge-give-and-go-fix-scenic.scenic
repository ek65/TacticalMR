from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model

import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# --- Coach Behavior ---
behavior CoachBehavior():
    do Speak("wait for 1 second before doing anything")
    do Idle() for 1 seconds

    do Speak("move to stay at least 6 meters from opponent until they are farther than 6 meters")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)

    do Speak("wait until ready to receive a pass")
    do Idle() until λ_precondition_0(simulation(), None)

    do Speak("move to the side of the field to create a passing angle")
    do MoveTo(λ_target2()) until λ_termination2(simulation(), None)

    do Speak("move to the ball and gain possession")
    do GetBallPossession(ball)

    do Speak("wait if there is no pressure from opponent")
    do Idle() until λ_precondition_1_3(simulation(), None)

    if λ_precondition1(simulation(), None):
        do Speak("move to maintain at least 6 meters from opponent and align left of them")
        do MoveTo(λ_target2()) until λ_termination2(simulation(), None)

        do Speak("wait until teammate is clear for passing")
        do Idle() until λ_precondition_2(simulation(), None)

        do Speak("pass the ball to a teammate")
        do Pass(teammate)
    else:
        do Speak("move towards the goal keeping within 5 meters of it")
        do MoveTo(λ_target4()) until λ_termination4(simulation(), None)

        do Speak("wait until ready to shoot with a clear path to goal")
        do Idle() until λ_precondition_4(simulation(), None)

        do Speak("attempt to shoot the ball towards the goal")
        do Shoot(goal)

# --- Constraints & Targets ---
A1termination_0 = DistanceTo({ 'from': 'opponent', 'to': 'Coach', 'min': {'avg': 5.92, 'std': 0.18}, 'operator': 'greater_than' })
A1target_0 = DistanceTo({ 'from': 'opponent', 'to': 'Coach', 'min': {'avg': 5.92, 'std': 0.18}, 'operator': 'greater_than' })
A1termination_2 = DistanceTo({ 'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.17, 'std': 0.0}, 'operator': 'greater_than' })
A2termination_2 = HasPath({ 'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.041, 'std': 0.0} })
A1target_2 = DistanceTo({ 'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.17, 'std': 0.0}, 'operator': 'greater_than' })
A2target_2 = HorizontalRelation({ 'obj': 'Coach', 'ref': 'opponent', 'relation': 'right', 'horizontal_threshold': {'avg': 3.0, 'std': 0.0} })

A1termination_4 = CloseTo({ 'obj': 'Coach', 'ref': 'goal', 'max': {'avg': 5.03, 'std': 0.015} })
A2termination_4 = HasBallPossession({ 'player': 'Coach' })
A3termination_4 = HasPath({ 'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0} })
A1target_4 = CloseTo({ 'obj': 'Coach', 'ref': 'goal', 'max': {'avg': 5.03, 'std': 0.015} })

A1precondition_0 = MakePass({ 'player': 'teammate' })
A1precondition_1 = Pressure({ 'player1': 'opponent', 'player2': 'Coach' })
A1precondition_2 = DistanceTo({ 'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.17, 'std': 0.0}, 'operator': 'greater_than' })
A2precondition_2 = HasPath({ 'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.041, 'std': 0.0} })
A1precondition_3 = Pressure({ 'player1': 'opponent', 'player2': 'teammate' })
A2precondition_3 = Pressure({ 'player1': 'opponent', 'player2': 'Coach' })
A1precondition_4 = HasBallPossession({ 'player': 'Coach' })
A2precondition_4 = HasPath({ 'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0} })

# --- Lambda Functions ---
def λ_target0(): return A1target_0.dist(simulation(), ego=True)
def λ_target2(): return A1target_2.dist(simulation(), ego=True) & A2target_2.dist(simulation(), ego=True)
def λ_target4(): return A1target_4.dist(simulation(), ego=True)

def λ_termination0(scene, sample): return A1termination_0.bool(simulation())
def λ_termination2(scene, sample): return A1termination_2.bool(simulation()) & A2termination_2.bool(simulation())
def λ_termination4(scene, sample): return A1termination_4.bool(simulation()) & A2termination_4.bool(simulation()) & A3termination_4.bool(simulation())

def λ_precondition0(scene, sample): return A1precondition_0.bool(simulation())
def λ_precondition_0(scene, sample): return λ_precondition0(simulation(), sample)
def λ_precondition1(scene, sample): return A1precondition_1.bool(simulation())
def λ_precondition3(scene, sample): return A1precondition_3.bool(simulation()) and not A2precondition_3.bool(simulation())
def λ_precondition_1_3(scene, sample): return λ_precondition1(simulation(), sample) or λ_precondition3(simulation(), sample)
def λ_precondition2(scene, sample): return A1precondition_2.bool(simulation()) & A2precondition_2.bool(simulation())
def λ_precondition_2(scene, sample): return λ_precondition2(simulation(), sample)
def λ_precondition4(scene, sample): return A1precondition_4.bool(simulation()) & A2precondition_4.bool(simulation())
def λ_precondition_4(scene, sample): return λ_precondition4(simulation(), sample)

# --- Additional Behaviors ---
behavior Follow(obj):
    while True:
        do MoveToBehavior(obj, distance=3, status=f"Follow {obj.name}")

def pressure(player1, player2):
    behav = player1.gameObject.behavior.lower()
    name = player2.name.lower()
    return 'follow' in behav and name in behav

behavior opponent1Behavior():
    do Idle() until teammate.gameObject.ballPossession
    do Follow(ball) until ego.gameObject.ballPossession
    do Follow(teammate)

A = HasPath({ 'obj1': 'teammate', 'obj2': 'coach', 'path_width': { 'avg': 2, 'std': 1 } })

behavior TeammateBehavior():
    passed = False
    try:
        do GetBallPossession(ball)
        do Idle()
    interrupt when A.bool(simulation()) and not passed and self.gameObject.ballPossession:
        do Idle() for 2.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point) until MakePass({ 'player': 'coach' }).bool(simulation())
        do Idle() for 0.5 seconds
        do GetBallPossession(ball)
        do Shoot(goal)
        passed = True

def teammateHasBallPossession():
    for obj in simulation().objects:
        if isinstance(obj, Player) and obj.team == "blue" and obj.gameObject.ballPossession:
            return True
    return False

behavior GetBehindAndReceiveBall(player, zone):
    do MoveToBehavior(point) until self.position.y > player.position.y + 2
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()

behavior ReceiveBall():
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()

# --- Scene Setup ---
teammate = new Player at (0,0), with name "teammate", with team "blue", with behavior TeammateBehavior()
ball = new Ball ahead of teammate by 1
opponent = new Player ahead of teammate by 5, facing toward teammate, with name "opponent", with team "red", with behavior opponent1Behavior()
ego = new Coach behind opponent by 5, facing toward teammate, with name "Coach", with team "blue", with behavior CoachBehavior()
goal = new Goal behind opponent by 10, facing away from ego

terminate when ego.gameObject.stopButton
