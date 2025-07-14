from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Wait and get in good position to receive the ball")
    #do Idle() until λ_possession_coach(simulation(), None)
    do GetBallPossession(ball)
    do Speak("Get possession of the ball and prepare to pass")
    do GetBallPossession(ball)
    do Speak("Evaluate passing options and wait until best option available")
    do Idle() until λ_pass_option_ready(simulation(), None)
    do Speak("Pass forward or to the teammate with most space")
    do Pass(λ_best_pass_dest(simulation(), None))
    do Idle() for 4 seconds
    do Speak("Wait for teammate to receive the ball")
    do Idle() until λ_teammate_has_ball(simulation(), None)
    do Speak("Move to support the new ball carrier as a passing option")
    do MoveTo(λ_support_new_carrier(simulation(), None))
    do Speak("Idle in support position for a few moments")
    do Idle() for 2 seconds

A_has_ball = HasBallPossession({'player': 'Coach'})
A_right_striker_open = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 2.8, 'std': 0.25}})
A_left_striker_open = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 1.3, 'std': 0.3}})
A_left_winger_open = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger', 'path_width': {'avg': 3.6, 'std': 0.4}})
A_right_winger_open = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 2.8, 'std': 0.25}})
A_left_striker_receives = HasBallPossession({'player': 'LeftStriker'})
A_right_striker_receives = HasBallPossession({'player': 'RightStriker'})
A_left_winger_receives = HasBallPossession({'player': 'LeftWinger'})
A_right_winger_receives = HasBallPossession({'player': 'RightWinger'})

A_move_support_LS = DistanceTo({'from': 'Coach', 'to': 'LeftStriker', 'min': {'avg': 4.4, 'std': 0.7}, 'max': {'avg': 10.1, 'std': 1.6}, 'operator': 'within'})
A_move_support_RS = DistanceTo({'from': 'Coach', 'to': 'RightStriker', 'min': {'avg': 4.6, 'std': 0.9}, 'max': {'avg': 10.3, 'std': 2.1}, 'operator': 'within'})
A_move_support_LW = DistanceTo({'from': 'Coach', 'to': 'LeftWinger', 'min': {'avg': 4.5, 'std': 0.8}, 'max': {'avg': 9.8, 'std': 1.7}, 'operator': 'within'})
A_move_support_RW = DistanceTo({'from': 'Coach', 'to': 'RightWinger', 'min': {'avg': 4.5, 'std': 0.8}, 'max': {'avg': 10.0, 'std': 1.6}, 'operator': 'within'})

def λ_possession_coach(scene, sample):
    # Waits until Coach has the ball
    return A_has_ball.bool(simulation())

def λ_pass_option_ready(scene, sample):
    # Waits for a strong passing option (teammate not blocked, with path open)
    return (
        A_right_striker_open.bool(simulation()) or
        A_left_striker_open.bool(simulation()) or
        A_left_winger_open.bool(simulation()) or
        A_right_winger_open.bool(simulation())
    )

def λ_best_pass_dest(scene, sample):
    # Passes forward, prioritizing unblocked options (priority: strikers ahead, then wingers)
    if A_right_striker_open.bool(simulation()):
        return RightStriker
    elif A_left_striker_open.bool(simulation()):
        return LeftStriker
    elif A_left_winger_open.bool(simulation()):
        return LeftWinger
    elif A_right_winger_open.bool(simulation()):
        return RightWinger
    else:
        return LeftWinger # fallback to left winger

def λ_teammate_has_ball(scene, sample):
    # Teammate successfully receives the ball
    return (
        A_right_striker_receives.bool(simulation()) or
        A_left_striker_receives.bool(simulation()) or
        A_left_winger_receives.bool(simulation()) or
        A_right_winger_receives.bool(simulation())
    )

def λ_support_new_carrier(scene, sample):
    # Move to support whoever received the ball
    if A_right_striker_receives.bool(simulation()):
        return A_move_support_RS.dist(simulation(), ego=True)
    elif A_left_striker_receives.bool(simulation()):
        return A_move_support_LS.dist(simulation(), ego=True)
    elif A_left_winger_receives.bool(simulation()):
        return A_move_support_LW.dist(simulation(), ego=True)
    elif A_right_winger_receives.bool(simulation()):
        return A_move_support_RW.dist(simulation(), ego=True)
    else:
        return A_move_support_LS.dist(simulation(), ego=True)  # Fallback

def λ_termination(scene, sample):
    # Terminate after idling in support (not after pass or receive)
    return False

def λ_precondition(scene, sample):
    # None needed for this program
    return True



# Ego (center midfielder) at origin
pi = 3.1415
ego = new Coach at (0, 0, 0), facing toward (0, 0, 0), with team "blue", with behavior CoachBehavior()

# Wingers
left_winger_angle = 90 + Uniform(0, 10)  # degrees from y-axis, 90 is positive x-axis (left), variance +/-10
right_winger_angle = -90 + Uniform(0, 10)  # degrees from y-axis, -90 is negative x-axis (right), variance +/-10
winger_dist = Uniform(6,8)

left_winger_x = winger_dist * sin(left_winger_angle * pi / 180)
left_winger_y = winger_dist * cos(left_winger_angle * pi / 180)
LeftWinger = new Player at (left_winger_x, left_winger_y, 0), facing toward ego, with name "LeftWinger", with team "blue"

right_winger_x = winger_dist * sin(right_winger_angle * pi / 180)
right_winger_y = winger_dist * cos(right_winger_angle * pi / 180)
RightWinger = new Player at (right_winger_x, right_winger_y, 0), facing toward ego, with name "RightWinger", with team "blue"

# Strikers
left_striker_angle = -Uniform(8, 20)
right_striker_angle = Uniform(8, 20)
striker_dist = Uniform(8,10)

left_striker_x = striker_dist * sin(left_striker_angle * pi / 180)
left_striker_y = striker_dist * cos(left_striker_angle * pi / 180)
LeftStriker = new Player at (left_striker_x, left_striker_y, 0), facing toward ego, with name "LeftStriker", with team "blue"

right_striker_x = striker_dist * sin(right_striker_angle * pi / 180)
right_striker_y = striker_dist * cos(right_striker_angle * pi / 180)
RightStriker = new Player at (right_striker_x, right_striker_y, 0), facing toward ego, with name "RightStriker", with team "blue"

# Ball at ego's feet
ball = new Ball at (0, 1, 0)

# Defenders: each assigned to one attacker, at a distance and angle in front of them, facing ego
# Helper function for defender placement
# (Scenic doesn't support functions in .scenic, so we inline the logic)

defender1_angle = Uniform(-10, 10)
defender1_dist = Uniform(2,4)
defender1_x = ego.position.x + defender1_dist * sin(defender1_angle * pi / 180)
defender1_y = ego.position.y + defender1_dist * cos(defender1_angle * pi / 180)
defender1 = new Player at (defender1_x, defender1_y, 0), facing toward ego, with team "red", with name "Defender1"

defender2_angle = Uniform(-30, 30)
defender2_dist = Uniform(1,2)
defender2_x = LeftWinger.position.x + defender2_dist * sin(defender2_angle * pi / 180)
defender2_y = LeftWinger.position.y + defender2_dist * cos(defender2_angle * pi / 180)
defender2 = new Player at (defender2_x, defender2_y, 0), facing toward ego, with team "red", with name "Defender2"

defender3_angle = Uniform(-30, 30)
defender3_dist = Uniform(1,2)
defender3_x = RightWinger.position.x + defender3_dist * sin(defender3_angle * pi / 180)
defender3_y = RightWinger.position.y + defender3_dist * cos(defender3_angle * pi / 180)
defender3 = new Player at (defender3_x, defender3_y, 0), facing toward ego, with team "red", with name "Defender3"

defender4_angle = Uniform(-30, 30)
defender4_dist = Uniform(1,2)
defender4_x = LeftStriker.position.x + defender4_dist * sin(defender4_angle * pi / 180)
defender4_y = LeftStriker.position.y + defender4_dist * cos(defender4_angle * pi / 180)
defender4 = new Player at (defender4_x, defender4_y, 0), facing toward ego, with team "red", with name "Defender4"

defender5_angle = Uniform(-30, 30)
defender5_dist = Uniform(1,2)
defender5_x = RightStriker.position.x + defender5_dist * sin(defender5_angle * pi / 180)
defender5_y = RightStriker.position.y + defender5_dist * cos(defender5_angle * pi / 180)
defender5 = new Player at (defender5_x, defender5_y, 0), facing toward ego, with team "red", with name "Defender5"
