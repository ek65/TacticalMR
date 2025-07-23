from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Moving behind my teammate into open space to prepare for a shot.")
    do MoveTo(λ_target_0())
    do Speak("Wait until I receive the pass from teammate.")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Move to get possession of the ball.")
    do MoveToBallAndGetPossession()
    do Speak("Wait until I have the ball.")
    do Idle() until λ_precondition_1(simulation(), None)
    do Speak("Dribble forward past the defender towards the goal.")
    do MoveTo(λ_target_1())
    do Speak("Wait until I have advanced past the defender and am close to goal.")
    do Idle() until λ_precondition_2(simulation(), None)
    do Speak("Take a shot on the goal.")
    do Shoot(goal)
    do Idle()

# Constraint definitions
A1target_0 = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 2.2, 'std': 0.2}, 'max': {'avg': 3.2, 'std': 0.2}, 'operator': 'within'})
A2target_0 = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'opponent', 'theta': {'avg': 37, 'std': 4}, 'dist': {'avg': 2.8, 'std': 0.2}})
A1target_1 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.3, 'std': 0.2}, 'operator': 'less_than'})
A2target_1 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 1.6, 'std': 0.1}})
A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = HasBallPossession({'player': 'Coach'})
A1precondition_2 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.3, 'std': 0.2}, 'operator': 'less_than'})

def λ_target_0():
    cond = A1target_0 and A2target_0
    return cond.dist(simulation(), ego=True)

def λ_target_1():
    cond = A1target_1 and A2target_1
    return cond.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition_2(scene, sample):
    return A1precondition_2.bool(simulation())





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
    do MoveToBallAndGetPossession(ball)
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