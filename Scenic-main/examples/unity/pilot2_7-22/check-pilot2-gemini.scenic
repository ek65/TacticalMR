from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1_target_0 = AtAngle({'player': 'Coach', 'ball': 'ball', 'left': {'theta': {'avg': 30.0, 'std': 5.0}, 'dist': {'avg': 7.0, 'std': 1.0}}, 'right': {'theta': {'avg': 30.0, 'std': 5.0}, 'dist': {'avg': 7.0, 'std': 1.0}}})
A1_precondition_0 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.2}})
A2_precondition_0 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 1.8, 'std': 0.3}, 'max': None, 'operator': 'greater_than'})

def λ_target0():
    return A1_target_0.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    return A1_precondition_0.bool(simulation()) and A2_precondition_0.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I'll move to create an angle for a pass and get some space from the defender.")
    do MoveTo(λ_target0())
    do Speak("I'm in position now, waiting for the ball from my teammate.")
    do StopAndReceiveBall()
    if λ_precondition_0(simulation(), None):
        do Speak("The path to the goal is clear and I have enough space. I'm going to shoot.")
        do Shoot(goal)
    else:
        do Speak("The defender is too close or blocking the shot. I'll pass it back safely.")
        do Pass(teammate)
    do Idle()
####Environment Behavior START####


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
####Environment Behavior START####
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