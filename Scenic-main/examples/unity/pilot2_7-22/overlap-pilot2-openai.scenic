from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1_overlap = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'opponent', 'theta': {'avg': 34.0, 'std': 6.0}, 'dist': {'avg': 6.5, 'std': 1.0}})
A1_receive = HasBallPossession({'player': 'Coach'})
A1_pass_ready = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.5, 'std': 0.4}})
A1_advance = DistanceTo({'from': 'goal', 'to': 'Coach', 'min': None, 'max': {'avg': 6.0, 'std': 0.4}, 'operator': 'less_than'})
A2_advance = HasBallPossession({'player': 'Coach'})
A1_shoot_ready = HasBallPossession({'player': 'Coach'})
A2_shoot_ready = DistanceTo({'from': 'goal', 'to': 'Coach', 'min': None, 'max': {'avg': 2.5, 'std': 0.3}, 'operator': 'less_than'})

def λ_target_overlap():
    return A1_overlap.dist(simulation(), ego=True)

def λ_precondition_receive(scene, sample):
    return A1_receive.bool(simulation())

def λ_precondition_pass_ready(scene, sample):
    return A1_pass_ready.bool(simulation())

def λ_target_advance():
    cond = A1_advance and A2_advance
    return cond.dist(simulation(), ego=True)

def λ_precondition_shoot_ready(scene, sample):
    return A1_shoot_ready.bool(simulation()) and A2_shoot_ready.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Move to overlap and find space behind the teammate.")
    do MoveTo(λ_target_overlap())
    do Speak("Wait to receive the ball from teammate.")
    do Idle() until λ_precondition_receive(simulation(), None)
    do Speak("Move into open space with a run, preparing to shoot.")
    do MoveTo(λ_target_advance())
    do Speak("Wait until ready to shoot: close and with possession.")
    do Idle() until λ_precondition_shoot_ready(simulation(), None)
    do Speak("Shoot towards the goal to finish the attack!")
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