from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("I'll check into open space to give my teammate a passing option.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("The pass is coming. I'll move to get the ball.")
    do GetBallPossession(ball)
    do Speak("Now I have the ball. Let me assess the opponent's position.")
    do Idle() until λ_precondition_1_or_2(simulation(), None)
    if λ_precondition1(simulation(), None):
        do Speak("The opponent is too close. I'll pass the ball back to my teammate.")
        do Pass(teammate)
    elif λ_precondition2(simulation(), None):
        do Speak("I have enough space, so I will dribble forward toward the goal.")
        do MoveTo(λ_target2()) until λ_termination2(simulation(), None)
        do Speak("I'm in a good shooting position. I'll take the shot now.")
        do Shoot(goal)

A1target_0 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 2.76, 'std': 1.1}})
A1termination_0 = MakePass({'player': 'teammate'})
A1precondition_1 = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': 3.5, 'std': 0.55}})
A1precondition_2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'operator': 'greater_than', 'min': {'avg': 4.5, 'std': 1.2}})
A1target_2 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.5}})
A1termination_2 = HasBallPossession({'player': 'Coach'})
A2termination_2 = DistanceTo({'from': 'Coach', 'to': 'goal', 'operator': 'less_than', 'max': {'avg': 6.0, 'std': 1.0}})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition2(scene, sample):
    return A1precondition_2.bool(simulation())

def λ_precondition_1_or_2(scene, sample):
    return λ_precondition1(scene, sample) or λ_precondition2(scene, sample)

def λ_target2():
    return A1target_2.dist(simulation(), ego=True)

def λ_termination2(scene, sample):
    return A1termination_2.bool(simulation()) and A2termination_2.bool(simulation())




# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do GetBallPossession(ball)
    print("got ball")
    do Idle() for 1.0 seconds
    do Pass(coach)
    do Idle()

behavior OpponentFollowCoach():
    do Idle() for 0.5 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    while True:
        do MoveToBehavior(coach.position)
        do Idle() for 0.1 seconds

# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "green", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Coach ahead of teammate by coach_start_dist, with name "Coach", with team "blue", with behavior CoachBehavior()

# Place opponent ahead of coach (further from goal than coach)
opponent = new Player ahead of coach by opponent_dist, facing toward coach, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 10, 0)