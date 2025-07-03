from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Checking to an angle to create space for the teammate.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("Waiting while teammate receives the ball.")
    do Idle() until λ_precondition0(simulation(), None)
    do Speak("Move to get ball possession from teammate's pass.")
    do GetBallPossession(ball)
    do Speak("Pausing to secure possession and assess opponent's distance.")
    do Idle() until λ_precondition1(simulation(), None)
    if λ_precondition2(simulation(), None):
        do Speak("Opponent is close, pass back safely to teammate.")
        do Pass(teammate)
    else:
        do Speak("There is space, dribble forward (move away from opponent).")
        do MoveTo(λ_target1()) until λ_termination1(simulation(), None)
        do Speak("Maintain possession and continue progress upfield.")
        do Idle() until λ_precondition3(simulation(), None)

# Constraint class instantiations and thresholds learned from all demos:

A1target_0 = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 4.0, 'std': 0.8}, 'operator': 'greater_than'})
A1termination_0 = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 4.0, 'std': 0.8}, 'operator': 'greater_than'})

A1precondition_0 = HasBallPossession({'player': 'teammate'})
A1precondition_1 = HasBallPossession({'player': 'Coach'})

A1precondition_2 = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': 2.5, 'std': 0.5}})
A2precondition_2 = Pressure({'player1': 'opponent', 'player2': 'Coach'})

A1target_1 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 7.0, 'std': 1.0}, 'operator': 'greater_than'})
A1termination_1 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 7.0, 'std': 1.0}, 'operator': 'greater_than'})
A1precondition_3 = HasBallPossession({'player': 'Coach'})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition2(scene, sample):
    return A1precondition_2.bool(simulation()) and A2precondition_2.bool(simulation())

def λ_target1():
    return A1target_1.dist(simulation(), ego=True)

def λ_termination1(scene, sample):
    return A1termination_1.bool(simulation())

def λ_precondition3(scene, sample):
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