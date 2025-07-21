from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("My teammate has the ball. I will make an overlapping run to create space and offer a passing option.")
    do Idle() until λ_precondition0()
    do MoveTo(λ_target0(), terminate_when=λ_termination0)
    do Speak("He sees me! Now I'll get in position to receive the pass from my teammate.")
    do StopAndReceiveBall()
    do Speak("Okay, I have the ball. Let's see if I should shoot or pass it back.")
    do Idle() until λ_precondition1()
    if λ_condition_shoot():
        do Speak("I have space in front of me! I'll drive toward the goal for a shot.")
        do MoveTo(λ_target1(), terminate_when=λ_termination1)
        do Speak("The defender is closing in. This is my chance to take a shot!")
        do Shoot(goal)
    else:
        do Speak("The defender is too close. I'll pass it back to my teammate who should be open now.")
        do Idle() until λ_precondition2()
        do Pass(teammate)
    do Idle()

A_precondition0 = HasBallPossession({'player': 'teammate'})
A_target0 = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'opponent', 'theta': {'avg': 60, 'std': 10}, 'dist': {'avg': 5.5, 'std': 0.5}})
A_termination0 = MakePass({'player': 'teammate'})
A_precondition1 = HasBallPossession({'player': 'Coach'})
A_condition_shoot = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 4.0, 'std': 0.5}, 'operator': 'greater_than'})
A_target1 = DistanceTo({'from': 'Coach', 'to': 'goal', 'max': {'avg': 9.0, 'std': 1.0}, 'operator': 'less_than'})
A_termination1 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A_precondition2 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 1.5, 'std': 0.25}})

def λ_precondition0():
    return A_precondition0.bool(simulation())

def λ_target0():
    return A_target0.dist(simulation(), ego=True)

def λ_termination0(val):
    return A_termination0.bool(simulation())

def λ_precondition1():
    return A_precondition1.bool(simulation())

def λ_condition_shoot():
    return A_condition_shoot.bool(simulation())

def λ_target1():
    return A_target1.dist(simulation(), ego=True)

def λ_termination1(val):
    return A_termination1.bool(simulation())

def λ_precondition2():
    return A_precondition2.bool(simulation())




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