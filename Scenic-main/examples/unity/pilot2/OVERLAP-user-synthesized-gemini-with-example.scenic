from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("The defender is on my teammate. I'll get into open space for a pass.")
    do MoveTo(λ_target0()) until λ_termination0(self)
    do Speak("Teammate is passing! I'm moving to get the ball.")
    do MoveToBallAndGetPossession()
    do Idle() until λ_precondition1(simulation(), None)
    do Speak("I have the ball. Dribbling forward to get past the defender.")
    do MoveTo(λ_target1()) until λ_termination1(self)
    do Idle() until λ_precondition2(simulation(), None)
    do Speak("I'm past the defender and have a clear shot. Taking it!")
    do Shoot(goal)
    do Idle()

A1target_0 = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'opponent', 'theta': {'avg': 50.0, 'std': 10.0}, 'dist': {'avg': 5.0, 'std': 1.0}})
A1termination_0 = MakePass({'player': 'teammate'})
A1precondition_1 = HasBallPossession({'player': 'Coach'})
A1target_1 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 8.0, 'std': 1.0}, 'operator': 'less_than'})
A1termination_1 = HeightRelation({'obj': 'Coach', 'ref': 'opponent', 'relation': 'above', 'height_threshold': {'avg': 0.5, 'std': 0.2}})
A1precondition_2 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 3.0, 'std': 0.5}})
A2precondition_2 = HasBallPossession({'player': 'Coach'})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(self):
    return A1termination_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_target1():
    return A1target_1.dist(simulation(), ego=True)

def λ_termination1(self):
    return A1termination_1.bool(simulation())

def λ_precondition2(scene, sample):
    return A1precondition_2.bool(simulation()) and A2precondition_2.bool(simulation())




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