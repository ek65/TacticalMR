from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("I will make an overlap run to get into an open position for a pass.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("Now that I'm in a good spot, I'll wait for the pass from my teammate.")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("The pass is on its way, time to get the ball.")
    do GetBallPossession(ball)
    do Speak("I have the ball. Let me assess the defender's position before I decide what to do next.")
    do Idle() until λ_precondition_1(simulation(), None) or λ_precondition_2(simulation(), None)
    if λ_precondition1(simulation(), None):
        do Speak("The defender is far away, so I have space to dribble towards the goal.")
        do MoveTo(goal)
    else:
        do Speak("The defender is too close. The best option is to pass the ball back to my teammate.")
        do Pass(teammate)

A1target_0 = HeightRelation({'obj': 'Coach', 'relation': 'above', 'ref': 'teammate', 'height_threshold': {'avg': 2.980644941329956, 'std': 1.0968987941741943}})
A2target_0 = HorizontalRelation({'obj': 'Coach', 'relation': 'left', 'ref': 'teammate', 'horizontal_threshold': {'avg': 5.86249303817749, 'std': 2.378939628601074}})
A1termination_0 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 1.0, 'std': 0.0}})
A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = DistanceTo({'to': 'defender1', 'from': 'Coach', 'operator': 'greater_than', 'min': {'avg': 3.7289562225341797, 'std': 1.0267431735992432}})
A1precondition_2 = CloseTo({'obj': 'Coach', 'ref': 'defender1', 'max': {'avg': 2.7681384086608887, 'std': 0.8193026781082153}})

def λ_target0():
    return A1target_0 and A2target_0

def λ_termination0(scene, sample):
    return A1termination_0.bool(scene)

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(scene)

def λ_precondition_1(scene, sample):
    return A1precondition_1.bool(scene)

def λ_precondition_2(scene, sample):
    return A1precondition_2.bool(scene)




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
    do GetBallPossession(ball)
    do Idle() until ego.position.y > 2
    do Pass(ego, slow=False) until (distance from opponent to ego) <= 3
    do DribbleTo(goal) until (distance from opponent to ego) > 3
    do Idle() for 2 seconds
    

behavior DefenderBehavior():
    do Idle() for 1 seconds
    do Idle() until ego.position.y > 1
    do Follow(ego) until ego.gameObject.ballPossession
    

teammate = new Player at (0, 0, 0),
      with behavior TeammateBehavior(), with name "teammate"

ball = new Ball ahead of teammate by 1

ego = new Coach at (ego_x_distance, ego_y_distance, 0), with name "Coach", with team "blue", with behavior CoachBehavior()

opponent = new Player at (0, Uniform(4, 6), 0), with name "defender1",
            with behavior DefenderBehavior()

goal = new Goal at (0, 10, 0)