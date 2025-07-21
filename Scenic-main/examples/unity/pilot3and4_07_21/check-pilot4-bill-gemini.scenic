from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I need to get open for a pass, so I will move diagonally to a better position.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("Now that I'm open, I'll get ready to receive the ball from my teammate.")
    do StopAndReceiveBall()
    do Speak("Now that I have the ball, I will check if I can shoot or if I should pass back to my teammate.")
    do Idle() until λ_precondition1(simulation(), None)
    if λ_precondition_if(simulation(), None):
        do Speak("I have a clear path to the goal, so I will take the shot.")
        do Shoot(goal)
    else:
        do Speak("My shot is blocked, so I'll pass it back to my teammate who has a better angle.")
        do Idle() until λ_precondition_else(simulation(), None)
        do Pass(teammate)
    do Idle()

A1_target_0 = AtAngle({'player': 'Coach', 'ball': 'ball', 'left': {'theta': {'avg': 47.8, 'std': 22.0}, 'dist': {'avg': 4.47, 'std': 1.0}}, 'right': {'theta': {'avg': 47.8, 'std': 22.0}, 'dist': {'avg': 4.47, 'std': 1.0}}})
A1_term_0 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1_pre_1 = HasBallPossession({'player': 'Coach'})
A1_pre_if = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 3.0, 'std': 0.5}})
A1_pre_else = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 3.0, 'std': 0.5}})

def λ_target0():
    return A1_target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1_term_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1_pre_1.bool(simulation())

def λ_precondition_if(scene, sample):
    return A1_pre_if.bool(simulation())

def λ_precondition_else(scene, sample):
    return A1_pre_else.bool(simulation())



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