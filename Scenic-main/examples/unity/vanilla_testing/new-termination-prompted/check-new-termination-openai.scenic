from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Waiting and creating space for the teammate to receive the ball")
    do Idle() until λ_termination_0(simulation(), None)
    do Speak("Check at an angle to create space, opponent will follow")
    do MoveTo(λ_target0())
    do Speak("Wait for the teammate to be ready and receive the pass")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Make yourself available and receive the pass from the teammate")
    do ReceiveBall()
    do Speak("Wait for ball possession before next decision")
    do Idle() until λ_precondition_1(simulation(), None)
    if λ_precondition_2(simulation(), None):
        do Speak("Opponent is close, immediately pass back to teammate")
        do Pass(teammate)
    else:
        do Speak("Opponent is far, turn and dribble forward")
        do MoveTo(λ_target1())
        do Speak("After gaining space, make a pass forward")
        do Pass(λ_target_pass_forward())

def λ_target0():
    # Check to an angle (left or right) from ball and goal, with respect to the opponent's position.
    return Overlap('Coach', 'ball', 'goal', 'opponent').dist(simulation(), ego=True)

def λ_target1():
    # Move upfield (towards goal), away from opponent, taking advantage of space.
    return HeightRelation('Coach', 'above', ref='opponent').dist(simulation(), ego=True)

def λ_target_pass_forward():
    # Pass forward into open space or advanced teammate (could be goal or zone, proxy as goal for now)
    return goal  # The pass is made towards goal area

def λ_termination_0(scene, sample):
    # Wait until teammate has the ball (begin of play step).
    return HasBallPossession('teammate')

def λ_precondition_0(scene, sample):
    # Wait until teammate passes to Coach (pass is executed).
    return MakePass('teammate')

def λ_precondition_1(scene, sample):
    # Wait until Coach has clear possession after receiving the pass.
    return HasBallPossession('Coach')

def λ_precondition_2(scene, sample):
    # Check: is opponent close (tight pressure), if so, Coach must pass back.
    return CloseTo('Coach', 'opponent', max={'avg': 2.5, 'std': 0.25}).bool(simulation())




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