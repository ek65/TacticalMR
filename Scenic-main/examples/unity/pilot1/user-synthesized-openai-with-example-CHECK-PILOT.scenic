from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Let’s move wide and pull the defender away from the center.")
    do MoveTo(λ_target0())
    do Speak("Wait until teammate is ready to pass.")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Get in position to receive the ball.")
    do StopAndReceiveBall()
    do Speak("Wait until possession of the ball is secured.")
    do Idle() until λ_precondition_1(simulation(), None)
    do Speak("Make a quick pass back to teammate as the shooting lane opens.")
    do Pass(teammate)
    do Speak("Wait until teammate receives the ball.")
    do Idle() until λ_precondition_2(simulation(), None)
    do Speak("Let’s shoot! Give the teammate a chance to score.")
    do Idle() until λ_precondition_3(simulation(), None)
    do Shoot(goal)
    do Idle()

A1target_0 = DistanceTo({'from': 'Coach', 'to': 'center_zone', 'min': {'avg': 6.5, 'std': 0.7}, 'max': None, 'operator': 'greater_than'})
A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = HasBallPossession({'player': 'Coach'})
A1precondition_2 = HasBallPossession({'player': 'teammate'})
A1precondition_3 = HasPath({'obj1': 'teammate', 'obj2': 'goal', 'path_width': {'avg': 2.1, 'std': 0.1}})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination_MoveTo():
    # Terminate move when sufficiently far from the center zone (not necessarily in best passing position yet).
    return λ_target0()  # triggers as soon as that threshold is met

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_termination_ReceiveBall():
    # Terminate receive when possession is gained (does not imply full readiness)
    return A1precondition_1.bool(simulation())

def λ_precondition_1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_termination_Pass():
    # Terminate pass after action (not outcome)
    return True

def λ_precondition_2(scene, sample):
    return A1precondition_2.bool(simulation())

def λ_termination_WaitForTeammate():
    # Terminate when shooting lane appears (before actual shot)
    return A1precondition_3.bool(simulation())

def λ_precondition_3(scene, sample):
    return A1precondition_3.bool(simulation())



# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do MoveToBallAndGetPossession(ball)
    print("got ball")
    do Idle() for 5.0 seconds
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