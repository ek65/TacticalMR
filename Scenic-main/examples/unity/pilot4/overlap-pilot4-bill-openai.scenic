from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Get ready to overlap the teammate. Move to overlap position.")
    do MoveTo(λ_target0())
    do Speak("Wait until you have ball possession from the teammate.")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Move to ball and get possession after overlap.")
    do MoveToBallAndGetPossession()
    do Speak("Pause to evaluate next attacking action.")
    do Idle() for 1 seconds
    do Speak("Check if opponent blocks direct path to goal, otherwise prepare for goal attack.")
    do Idle() until λ_precondition_1(simulation(), None)
    if λ_precondition1(simulation(), None):
        do Speak("Pass the ball to the teammate in a better position.")
        do Pass(teammate)
        do Speak("Wait until you receive the return pass from the teammate.")
        do Idle() until λ_precondition_2(simulation(), None)
        do Speak("Move to ball and get possession for quick attack.")
        do MoveToBallAndGetPossession()
        do Speak("Look for shot opportunity. Move close to the goal.")
        do Idle() until λ_precondition_3(simulation(), None)
        do Speak("Take a shot at the goal.")
        do Shoot(goal)
    else:
        do Speak("Go for goal directly and shoot.")
        do Shoot(goal)
    do Idle()

# -------- Constraint and lambda definitions --------

A1target_0 = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 31.0, 'std': 4.0},
    'dist': {'avg': 8.0, 'std': 1.0}
})

A1precondition_0 = HasBallPossession({'player': 'Coach'})
A1precondition_1 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.5}})
A2precondition_1 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 5.5, 'std': 0.5}, 'max': None, 'operator': 'greater_than'})
A1precondition_2 = HasBallPossession({'player': 'Coach'})
A1precondition_3 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 7.5, 'std': 0.7}, 'operator': 'less_than'})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition1(scene, sample):
    # True if Coach has a clear path to goal and not tightly marked by opponent
    return (A1precondition_1.bool(simulation()) and A2precondition_1.bool(simulation()))

def λ_precondition_1(scene, sample):
    # Wait until there's a clear path to goal, or until you need to pass due to pressure
    return λ_precondition1(scene, sample)

def λ_precondition_2(scene, sample):
    # Wait until Coach regains ball possession after teammate's return pass
    return A1precondition_2.bool(simulation())

def λ_precondition_3(scene, sample):
    # Wait until Coach is close enough to the goal to shoot
    return A1precondition_3.bool(simulation())





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