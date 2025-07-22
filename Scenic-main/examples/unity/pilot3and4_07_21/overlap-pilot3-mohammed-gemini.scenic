from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####


A1_target_0 = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'opponent', 'theta': {'avg': 50.0, 'std': 5.0}, 'dist': {'avg': 5.7, 'std': 0.5}})
A1_term_0 = MakePass({'player': 'teammate'})
A1_precondition_1 = HasBallPossession({'player': 'Coach'})
A1_precondition_2 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A2_precondition_2 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 2.0, 'std': 0.2}})
A1_target_1 = DistanceTo({'from': 'Coach', 'to': 'goal', 'operator': 'less_than', 'max': {'avg': 8.0, 'std': 1.0}})
A1_term_1 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'operator': 'less_than', 'max': {'avg': 4.0, 'std': 0.5}})
A1_precondition_3 = HasBallPossession({'player': 'Coach'})
A2_precondition_3 = DistanceTo({'from': 'Coach', 'to': 'goal', 'operator': 'less_than', 'max': {'avg': 8.5, 'std': 1.0}})
A3_precondition_3 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.5, 'std': 0.2}})

def λ_target0():
    return A1_target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1_term_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1_precondition_1.bool(simulation())

def λ_precondition2(scene, sample):
    return A1_precondition_2.bool(simulation()) and A2_precondition_2.bool(simulation())

def λ_target1():
    return A1_target_1.dist(simulation(), ego=True)

def λ_termination1(scene, sample):
    return A1_term_1.bool(simulation())

def λ_precondition3(scene, sample):
    return A1_precondition_3.bool(simulation()) and A2_precondition_3.bool(simulation()) and A3_precondition_3.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I'll run beyond my teammate to create a passing option and unsettle the defense.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("He sees my run. I'll stop and get ready to receive the pass.")
    do StopAndReceiveBall()
    do Speak("Now I have the ball. I'll check if the defender is closing me down or leaving space.")
    do Idle() until λ_precondition1(simulation(), None)
    if λ_precondition2(simulation(), None):
        do Speak("The defender is too close. I'll make a safe pass back to my teammate.")
        do Pass(teammate)
    else:
        do Speak("I have space. I'll drive forward towards the goal for a shot.")
        do MoveTo(λ_target1()) until λ_termination1(simulation(), None)
        do Speak("Now I'm in a good position. Taking the shot!")
        do Idle() until λ_precondition3(simulation(), None)
        do Shoot(goal)
    do Idle()



####Environment Behavior START####
opponent_y_distance = Uniform(3, 5)
opponent_x_distance = Uniform(-2, 2)
ego_x_distance = Uniform(-2, 2)
ego_y_distance = Uniform(-1, -2)

# Ensure teammate and opponent are on the same side
#require (opponent_x_distance < 0 and ego_x_distance < 0) or (opponent_x_distance >= 0 and ego_x_distance >= 0)

behavior Follow(obj):
    while ego.position.y > 1:
        do MoveToBehavior(obj, distance = 2, status = f"Follow {obj.name}")

behavior TeammateBehavior():
    do Idle() for 1 seconds
    do MoveToBallAndGetPossession()
    do Idle() for 10 seconds
    do Idle() until ego.position.y > 2
    do Pass(ego, slow=False) until (distance from opponent to ego) <= 3
    do DribbleTo(goal) until (distance from opponent to ego) > 3
    do Idle() for 2 seconds
    

behavior DefenderBehavior():
    do Idle() for 1 seconds
    do Idle() until ego.position.y > 1
    while True:
        if distance from self to ego > 2.0:
            do MoveToBehavior(ego.position, distance=2.0)
        else:
            do Idle() for 0.1 seconds   
    

teammate = new Player at (0, 0, 0),
      with behavior TeammateBehavior(), with name "teammate", with team "blue"

ball = new Ball ahead of teammate by 1

ego = new Coach at (ego_x_distance, ego_y_distance, 0), with name "Coach", with team "blue", with behavior CoachBehavior()

opponent = new Player at (0, Uniform(4, 6), 0), with name "opponent",
            with behavior DefenderBehavior(), with team "red"

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)