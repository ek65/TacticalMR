from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    #do Speak("Wait for space to be created and for teammate to receive the ball.")
    #do Idle() until λ_wait_teammate_possession(simulation(), None)
    do Speak("Check to an angle left or right to create space, away from the opponent.")
    do MoveTo(λ_angle_check())
    do Speak("Wait for the pass option or time for defender to react.")
    do Idle() until λ_pre_pass_option(simulation(), None)
    do Speak("Move to get possession from the teammate pass.")
    do GetBallPossession(ball)
    do Speak("Wait after receiving; decide based on opponent's pressure.")
    do Idle() until λ_after_receive(simulation(), None)
    if λ_pressure_tight(simulation(), None):
        do Speak("Opponent is tight, pass back to teammate quickly.")
        do Pass(teammate)
    else:
        do Speak("Opponent is far, take space and turn to dribble forward.")
        do MoveTo(λ_turn_dribble())
        do Speak("Wait for open space before making next action.")
        do Idle() until λ_termination_dribble(simulation(), None)

# Constraints from demonstrations

A_angle_check = AtAngle({'player': 'Coach', 'ball': 'ball',
    'left': {'theta': {'avg': 36.5, 'std': 8.1}, 'dist': {'avg': 8.2, 'std': 1.4}},
    'right': {'theta': {'avg': 33.2, 'std': 7.7}, 'dist': {'avg': 7.9, 'std': 1.0}}
})
A_pressure_tight = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': 3.7, 'std': 0.7}})
A_pressure_loose = DistanceTo({'to': 'opponent', 'from': 'Coach', 'operator': 'greater_than', 'min': {'avg': 7.9, 'std': 1.8}})
A_teammate_receives = HasBallPossession({'player': 'teammate'})
A_make_pass = MakePass({'player': 'teammate'})
A_coach_receive = HasBallPossession({'player': 'Coach'})
A_turn_dribble_forward = DistanceTo({'from': 'Coach', 'to': 'goal', 'operator': 'greater_than', 'min': {'avg': 12.5, 'std': 4.5}})

# λ functions

def λ_wait_teammate_possession(scene, sample):
    # Wait until the teammate has the ball
    return A_teammate_receives.bool(simulation())

def λ_angle_check():
    # Move at an angle away from the opponent (at left or right angle from ball).
    return A_angle_check.dist(simulation(), ego=True)

def λ_pre_pass_option(scene, sample):
    # Wait until a pass option is available (teammate makes pass)
    return A_make_pass.bool(simulation())

def λ_after_receive(scene, sample):
    # Wait until coach receives the ball (possession changes)
    return A_coach_receive.bool(simulation())

def λ_pressure_tight(scene, sample):
    # Is opponent close (tight pressure)? If so, pass back to teammate.
    return A_pressure_tight.bool(simulation())

def λ_turn_dribble():
    # Move forward (dribble) if opponent is far.
    return A_turn_dribble_forward.dist(simulation(), ego=True)

def λ_termination_dribble(scene, sample):
    # Terminate after making initial forward dribble move;
    # stop dribbling if environment changes, e.g., if pressured again or time passes
    # (Not actual goal of dribble)
    # Here we simply end after a fixed number of steps for illustration.
    return simulation().currentTime > 3



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
    do Idle() for 5.0 seconds
    do Pass(ego)
    do Idle()

behavior OpponentFollowCoach():
    do Idle() for 0.5 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    while True:
        do MoveToBehavior(ego.position)
        do Idle() for 0.1 seconds

# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "green", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Coach ahead of teammate by coach_start_dist, with name "Coach", with team "blue", with behavior CoachBehavior()

# Place opponent ahead of coach (further from goal than coach)
opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)