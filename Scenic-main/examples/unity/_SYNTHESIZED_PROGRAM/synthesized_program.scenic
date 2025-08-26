from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

# Target constraints: reposition 2 meters left/right of the nearest defender (defender1)
A1target_leftDist = DistanceTo({'from': 'ego', 'to': 'defender1', 'min': {'avg': 1.5, 'std': 0.2}, 'max': {'avg': 2.5, 'std': 0.2}, 'operator': 'within'})
A2target_leftSide = HorizontalRelation({'obj': 'ego', 'ref': 'defender1', 'relation': 'left', 'horizontal_threshold': {'avg': 0.0, 'std': 0.3}})

A1target_rightDist = DistanceTo({'from': 'ego', 'to': 'defender1', 'min': {'avg': 1.5, 'std': 0.2}, 'max': {'avg': 2.5, 'std': 0.2}, 'operator': 'within'})
A2target_rightSide = HorizontalRelation({'obj': 'ego', 'ref': 'defender1', 'relation': 'right', 'horizontal_threshold': {'avg': 0.0, 'std': 0.3}})

# Target constraints: support positions 2–3 meters below the receiving striker
A1target_supportLeftDist = DistanceTo({'from': 'ego', 'to': 'LeftStriker', 'min': {'avg': 2.0, 'std': 0.3}, 'max': {'avg': 3.0, 'std': 0.3}, 'operator': 'within'})
A2target_supportLeftBelow = HeightRelation({'obj': 'ego', 'ref': 'LeftStriker', 'relation': 'below', 'height_threshold': {'avg': 1.0, 'std': 0.3}})

A1target_supportRightDist = DistanceTo({'from': 'ego', 'to': 'RightStriker', 'min': {'avg': 2.0, 'std': 0.3}, 'max': {'avg': 3.0, 'std': 0.3}, 'operator': 'within'})
A2target_supportRightBelow = HeightRelation({'obj': 'ego', 'ref': 'RightStriker', 'relation': 'below', 'height_threshold': {'avg': 1.0, 'std': 0.3}})

# Preconditions: clear passing lanes of 2 meters to strikers, and confirmation that pass was made
Apre_path_left2m = HasPath({'obj1': 'ego', 'obj2': 'LeftStriker', 'path_width': {'avg': 2.0, 'std': 0.3}})
Apre_path_right2m = HasPath({'obj1': 'ego', 'obj2': 'RightStriker', 'path_width': {'avg': 2.0, 'std': 0.3}})
Apre_pass_made = MakePass({'player': 'ego'})

def λ_target_left():
    cond = A1target_leftDist & A2target_leftSide
    return cond.debug_dist(simulation(), ego=True)

def λ_target_right():
    cond = A1target_rightDist & A2target_rightSide
    return cond.debug_dist(simulation(), ego=True)

def λ_target_support_left():
    cond = A1target_supportLeftDist & A2target_supportLeftBelow
    return cond.debug_dist(simulation(), ego=True)

def λ_target_support_right():
    cond = A1target_supportRightDist & A2target_supportRightBelow
    return cond.debug_dist(simulation(), ego=True)

def λ_precondition_path_left():
    return Apre_path_left2m.bool(simulation())

def λ_precondition_path_right():
    return Apre_path_right2m.bool(simulation())

def λ_precondition_pass_made():
    return Apre_pass_made.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds

    do Speak("No clear 2 meter lanes to either striker.")
    do Speak("Shift about 2 meters left of defender1, keep ball.")
    do MoveTo(λ_target_left(), False)

    do Speak("wait to decide which passing lane opens")
    do Idle() until True

    if λ_precondition_path_left():
        do Speak("Left lane is at least 2 meters wide.")
        do Speak("wait until clear 2 meter lane to LeftStriker")
        do Idle() until λ_precondition_path_left()

        do Speak("Pass to LeftStriker now.")
        do Pass(LeftStriker)

        do Speak("wait until your pass is registered")
        do Idle() until λ_precondition_pass_made()

        do Speak("Move about 3 meters below LeftStriker for support.")
        do MoveTo(λ_target_support_left(), False)

        do Speak("Hold position ready to receive support pass.")
        do StopAndReceiveBall()
    else:
        do Speak("Left lane not 2 meters; switch to right option.")
        do Speak("Shift about 2 meters right of defender1, keep ball.")
        do MoveTo(λ_target_right(), False)

        do Speak("wait until clear 2 meter lane to RightStriker")
        do Idle() until λ_precondition_path_right()

        do Speak("Pass to RightStriker now.")
        do Pass(RightStriker)

        do Speak("wait until your pass is registered")
        do Idle() until λ_precondition_pass_made()

        do Speak("Move about 3 meters below RightStriker for support.")
        do MoveTo(λ_target_support_right(), False)

        do Speak("Hold position ready to receive support pass.")
        do StopAndReceiveBall()

    do Idle()

####Environment Behavior START####

# Ego (center midfielder) at origin
pi = 3.1415
ego = new Coach at (0, 0, 0), with team "blue", with behavior CoachBehavior()

# Wingers
left_winger_angle = -90 + Uniform(0, 10)  # degrees from y-axis, 90 is positive x-axis (left), variance +/-10
right_winger_angle = 90 + Uniform(0, 10)  # degrees from y-axis, -90 is negative x-axis (right), variance +/-10
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
goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)