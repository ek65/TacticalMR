from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I'm getting into position to receive the ball from my teammate and assess the opponent's pressure.")
    do MoveToBallAndGetPossession()
    do Speak("Now that I have the ball, I will check the defender's position to decide my next move.")
    if C1_close_opponent.bool(simulation(), None):
        do Speak("The opponent is too close! I'll fake one way, create space, and then take a shot.")
        do MoveTo(λ_target0())
        do Idle() until λ_precondition1(simulation(), None)
        do Speak("I've created enough space. Now I'll shoot.")
        do Shoot(goal)
    elif C2_medium_opponent.bool(simulation(), None):
        do Speak("The opponent is at a medium distance. The safest play is a pass back to my teammate.")
        do Idle() until λ_precondition2(simulation(), None)
        do Pass(teammate)
    else:
        do Speak("The opponent is far away, giving me lots of space to attack the goal.")
        do MoveTo(λ_target3())
        do Idle() until λ_precondition4(simulation(), None)
        do Speak("I'm in a good position to score now.")
        do Shoot(goal)
    do Idle()

C1_close_opponent = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': None, 'max': {'avg': 2.0, 'std': 0.5}, 'operator': 'less_than'})
C2_medium_opponent = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 2.0, 'std': 0.5}, 'max': {'avg': 5.0, 'std': 0.5}, 'operator': 'within'})
A1_target0 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 3.5, 'std': 0.5}, 'operator': 'greater_than'})
A2_target0 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 10.0, 'std': 1.0}, 'operator': 'less_than'})
C1_precondition1 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.5, 'std': 0.5}})
C1_precondition2 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 2.5, 'std': 0.5}})
A1_target3 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 8.0, 'std': 1.0}, 'operator': 'less_than'})
C1_precondition4 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.5, 'std': 0.5}})

def λ_target0():
    cond = A1_target0 and A2_target0
    return cond.dist(simulation(), ego=True)

def λ_termination0(past_position, current_position):
    return False

def λ_precondition1(scene, sample):
    return C1_precondition1.bool(scene, sample)

def λ_termination1(past_position, current_position):
    return False

def λ_precondition2(scene, sample):
    return C1_precondition2.bool(scene, sample)

def λ_termination2(past_position, current_position):
    return False

def λ_target3():
    return A1_target3.dist(simulation(), ego=True)

def λ_termination3(past_position, current_position):
    return False

def λ_precondition4(scene, sample):
    return C1_precondition4.bool(scene, sample)

def λ_termination4(past_position, current_position):
    return False




# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do MoveToBallAndGetPossession()
    print("got ball")
    do Idle() for 10.0 seconds
    do Pass(ego)
    do Idle()

behavior OpponentFollowCoach():
    do Idle() for 1.0 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    while True:
        if distance from self to ego > 2.0:
            do MoveToBehavior(ego.position, distance=2.0)
        else:
            do Idle() for 0.1 seconds

# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "blue", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Coach ahead of teammate by coach_start_dist, with name "Coach", with team "blue", with behavior CoachBehavior()

# Place opponent ahead of coach (further from goal than coach)
opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)