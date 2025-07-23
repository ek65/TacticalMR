from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# Constraint and Termination Function Definitions

A1precondition_possession = HasBallPossession({'player': 'Coach'})
A1precondition_rightwinger_path = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_leftwinger_path = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger',  'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_leftstriker_path = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker','path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_rightstriker_path= HasPath({'obj1': 'Coach', 'obj2': 'RightStriker','path_width': {'avg': 2.0, 'std': 0.0}})

A1target_pass_rightwinger  = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1target_pass_leftstriker = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker','path_width': {'avg': 2.0, 'std': 0.0}})
A1target_pass_leftwinger  = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger', 'path_width': {'avg': 2.0, 'std': 0.0}})

A1target_support_rightwinger  = DistanceTo({'from': 'Coach', 'to': 'RightWinger',  'min': {'avg': 4.0,'std': 0.2}, 'max': {'avg': 7.0,'std': 0.3}, 'operator': 'within'})
A1target_support_leftstriker = DistanceTo({'from': 'Coach', 'to': 'LeftStriker',  'min': {'avg': 4.0,'std': 0.2}, 'max': {'avg': 7.0,'std': 0.3}, 'operator': 'within'})
A1target_support_leftwinger  = DistanceTo({'from': 'Coach', 'to': 'LeftWinger',   'min': {'avg': 4.0,'std': 0.2}, 'max': {'avg': 7.0,'std': 0.3}, 'operator': 'within'})
A1target_support_rightstriker= DistanceTo({'from': 'Coach', 'to': 'RightStriker','min': {'avg': 4.0,'std': 0.2}, 'max': {'avg': 7.0,'std': 0.3}, 'operator': 'within'})

def λ_precondition_possession(scene, sample):
    return A1precondition_possession.bool(simulation())

def λ_precondition_clear_path_rightwinger(scene, sample):
    return A1precondition_rightwinger_path.bool(simulation())

def λ_precondition_clear_path_leftstriker(scene, sample):
    return A1precondition_leftstriker_path.bool(simulation())

def λ_precondition_clear_path_leftwinger(scene, sample):
    return A1precondition_leftwinger_path.bool(simulation())

# ADDED: check for clear path to Right Striker
def λ_precondition_clear_path_rightstriker(scene, sample):
    return A1precondition_rightstriker_path.bool(simulation())

def λ_termination_wait_for_pass(scene, sample):
    return not A1precondition_possession.bool(simulation())

def λ_target_pass_rightwinger():
    return 'RightWinger'

def λ_target_pass_leftstriker():
    return 'LeftStriker'

def λ_target_pass_leftwinger():
    return 'LeftWinger'

# ADDED: lambda for passing to Right Striker
def λ_target_pass_rightstriker():
    return 'RightStriker'

def λ_target_support_rightwinger():
    return A1target_support_rightwinger.dist(simulation(), ego=True)

def λ_target_support_leftstriker():
    return A1target_support_leftstriker.dist(simulation(), ego=True)

def λ_target_support_leftwinger():
    return A1target_support_leftwinger.dist(simulation(), ego=True)

# ADDED: lambda for moving to support Right Striker
def λ_target_support_rightstriker():
    return A1target_support_rightstriker.dist(simulation(), ego=True)

##########################################

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Ready to make a decision: scanning for best passing options now.")
    do Idle() until λ_precondition_possession(simulation(), None)
    do Speak("I've received the ball. Checking who should receive my pass.")

    # Prioritize strikers before wingers, and include RightStriker
    if λ_precondition_clear_path_leftstriker(simulation(), None):
        do Speak("Path to left striker is open. Passing there.")
        do Pass('LeftStriker')
        do Speak("Waiting for left striker to receive the ball.")
        do Idle() until HasBallPossession({'player': 'LeftStriker'}).bool(simulation())
        do Speak("Left striker has the ball. I'll move to support.")
        do MoveTo(λ_target_support_leftstriker())
    elif λ_precondition_clear_path_rightstriker(simulation(), None):
        do Speak("Path to right striker is open. Passing there.")
        do Pass('RightStriker')
        do Speak("Waiting for right striker to receive the ball.")
        do Idle() until HasBallPossession({'player': 'RightStriker'}).bool(simulation())
        do Speak("Right striker has the ball. I'll move to support.")
        do MoveTo(λ_target_support_rightstriker())
    elif λ_precondition_clear_path_leftwinger(simulation(), None):
        do Speak("Strikers blocked, passing to left winger.")
        do Pass('LeftWinger')
        do Speak("Waiting for left winger to receive the ball.")
        do Idle() until HasBallPossession({'player': 'LeftWinger'}).bool(simulation())
        do Speak("Left winger has the ball. I'll move to support.")
        do MoveTo(λ_target_support_leftwinger())
    elif λ_precondition_clear_path_rightwinger(simulation(), None):
        do Speak("Strikers blocked, passing to right winger.")
        do Pass('RightWinger')
        do Speak("Waiting for right winger to receive the ball.")
        do Idle() until HasBallPossession({'player': 'RightWinger'}).bool(simulation())
        do Speak("Right winger now has the ball. I'll move to support.")
        do MoveTo(λ_target_support_rightwinger())
    else:
        do Speak("No obvious pass available. Holding ball briefly.")
        do Idle() for 1 seconds

    do Speak("Ready for the next phase, waiting for instructions or opening.")
    do Idle()

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
ball = new Ball at (0, .2, 0)

# Defenders: each assigned to one attacker, at a distance and angle in front of them, facing ego
# Helper function for defender placement
# (Scenic doesn't support functions in .scenic, so we inline the logic)

defender1_angle = Uniform(-10, 10)
defender1_dist = Uniform(2,4)
defender1_x = ego.position.x + defender1_dist * sin(defender1_angle * pi / 180)
defender1_y = ego.position.y + defender1_dist * cos(defender1_angle * pi / 180)
defender1 = new Player at (defender1_x, defender1_y, 0), facing toward ego, with team "red", with name "defender1"

defender2_angle = Uniform(-30, 30)
defender2_dist = Uniform(1,2)
defender2_x = LeftWinger.position.x + defender2_dist * sin(defender2_angle * pi / 180)
defender2_y = LeftWinger.position.y + defender2_dist * cos(defender2_angle * pi / 180)
defender2 = new Player at (defender2_x, defender2_y, 0), facing toward ego, with team "red", with name "defender2"

defender3_angle = Uniform(-30, 30)
defender3_dist = Uniform(1,2)
defender3_x = RightWinger.position.x + defender3_dist * sin(defender3_angle * pi / 180)
defender3_y = RightWinger.position.y + defender3_dist * cos(defender3_angle * pi / 180)
defender3 = new Player at (defender3_x, defender3_y, 0), facing toward ego, with team "red", with name "defender3"

defender4_angle = Uniform(-30, 30)
defender4_dist = Uniform(1,2)
defender4_x = LeftStriker.position.x + defender4_dist * sin(defender4_angle * pi / 180)
defender4_y = LeftStriker.position.y + defender4_dist * cos(defender4_angle * pi / 180)
defender4 = new Player at (defender4_x, defender4_y, 0), facing toward ego, with team "red", with name "defender4"

defender5_angle = Uniform(-30, 30)
defender5_dist = Uniform(1,2)
defender5_x = RightStriker.position.x + defender5_dist * sin(defender5_angle * pi / 180)
defender5_y = RightStriker.position.y + defender5_dist * cos(defender5_angle * pi / 180)
defender5 = new Player at (defender5_x, defender5_y, 0), facing toward ego, with team "red", with name "defender5"
terminate when (ego.gameObject.stopButton)