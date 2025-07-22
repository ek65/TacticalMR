from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1_DistanceTo_opponent = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 2.0, 'std': 0.25}, 'max': None, 'operator': 'greater_than'})
A2_DistanceTo_teammate = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': None, 'max': {'avg': 3.0, 'std': 0.2}, 'operator': 'less_than'})
A3_HasBall_teammate = HasBallPossession({'player': 'teammate'})
A4_MovingTowards_goal = MovingTowards({'obj': 'teammate', 'ref': 'goal'})
A5_MakePass_teammate = MakePass({'player': 'teammate'})
A6_HasBall_Coach = HasBallPossession({'player': 'Coach'})
A7_DistanceTo_opponent_close = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': None, 'max': {'avg': 2.0, 'std': 0.2}, 'operator': 'less_than'})
A8_HasPath_Coach_teammate = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 1.1, 'std': 0.1}})
A9_HasPath_Coach_goal = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 1.0, 'std': 0.1}})

def λ_target_move_away_opponent():
    # Move away from opponent by >2m and get close to the teammate (<3m)
    cond = A1_DistanceTo_opponent and A2_DistanceTo_teammate
    return cond.dist(simulation(), ego=True)

def λ_precondition_teammate_gets_ball(scene, sample):
    return A3_HasBall_teammate.bool(simulation())

def λ_precondition_teammate_move_towards_goal(scene, sample):
    return A4_MovingTowards_goal.bool(simulation())

def λ_precondition_teammate_passed(scene, sample):
    return A5_MakePass_teammate.bool(simulation())

def λ_precondition_Coach_get_possession(scene, sample):
    return A6_HasBall_Coach.bool(simulation())

def λ_precondition_opponent_close(scene, sample):
    return A7_DistanceTo_opponent_close.bool(simulation())

def λ_precondition_clear_path_to_teammate(scene, sample):
    return A8_HasPath_Coach_teammate.bool(simulation())

def λ_precondition_clear_path_to_goal(scene, sample):
    return A9_HasPath_Coach_goal.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Move away from the defender and closer to your teammate for space.")
    do MoveTo(λ_target_move_away_opponent())
    do Speak("Wait until your teammate receives the ball.")
    do Idle() until λ_precondition_teammate_gets_ball(simulation(), None)
    do Speak("Wait for your teammate to start moving toward the goal.")
    do Idle() until λ_precondition_teammate_move_towards_goal(simulation(), None)
    do Speak("Wait until the teammate passes you the ball.")
    do Idle() until λ_precondition_teammate_passed(simulation(), None)
    do Speak("Move to the ball and get possession as it's passed.")
    do MoveToBallAndGetPossession()
    do Speak("Wait to confirm Coach has possession of the ball.")
    do Idle() until λ_precondition_Coach_get_possession(simulation(), None)
    if λ_precondition_opponent_close(simulation(), None):
        do Speak("Opponent is very close, pass it back to the teammate for safety.")
        do Pass(teammate)
    elif λ_precondition_clear_path_to_goal(simulation(), None):
        do Speak("Turn and shoot toward the goal, defender is not too close.")
        do Shoot(goal)
    elif λ_precondition_clear_path_to_teammate(simulation(), None):
        do Speak("Pass the ball back to your teammate, path is open.")
        do Pass(teammate)
    else:
        do Speak("Stop and receive the ball until a decision is made.")
        do StopAndReceiveBall()
    do Idle()



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