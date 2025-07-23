from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I'm in midfield with the ball, assessing my options based on the defender's pressure.")
    do MoveToBallAndGetPossession()
    do Speak("I have the ball, now I'll look for a passing lane.")
    #do Idle() until λ_precondition_pass_options()
    if λ_precondition_path_to_LS():
        do Speak("The defender gave me space, so I'll make an ambitious pass to the striker.")
        do Pass(LeftStriker)
        do Idle() until λ_precondition_LS_has_ball()
        do Speak("Now I'll make a run to support the striker and create a scoring opportunity.")
        do MoveTo(λ_target_fwd())
    elif λ_precondition_path_to_LW():
        do Speak("The defender is too close for a forward pass, so I'll play it safe to the left winger.")
        do Pass(LeftWinger)
        do Idle() until λ_precondition_LW_has_ball()
        do Speak("I'll move into the open space to be an option for a return pass.")
        do MoveTo(λ_target_fwd())
    else:
        do Speak("I'll exploit the space behind the defender by passing to my right winger.")
        do Pass(RightWinger)
        do Idle() until λ_precondition_RW_has_ball()
        do Speak("Now I'll run behind the defender to receive a quick one-two pass in a dangerous area.")
        do MoveTo(λ_target_behind_d1())
    do Idle()

C_path_to_LS = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 3.0, 'std': 0.5}})
C_path_to_LW = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger', 'path_width': {'avg': 3.0, 'std': 0.5}})
C_path_to_RW = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 3.0, 'std': 0.5}})
C_LS_has_ball = HasBallPossession({'player': 'LeftStriker'})
C_LW_has_ball = HasBallPossession({'player': 'LeftWinger'})
C_RW_has_ball = HasBallPossession({'player': 'RightWinger'})
C_move_target_fwd = HeightRelation({'obj': 'Coach', 'relation': 'above', 'ref': 'Coach', 'height_threshold': {'avg': 7.0, 'std': 1.0}})
C_move_target_behind_d1 = HeightRelation({'obj': 'Coach', 'relation': 'above', 'ref': 'Defender1', 'height_threshold': {'avg': 4.0, 'std': 1.0}})

def λ_target_fwd():
    return C_move_target_fwd.dist(simulation(), ego=True)

def λ_target_behind_d1():
    return C_move_target_behind_d1.dist(simulation(), ego=True)

def λ_precondition_path_to_LS():
    return C_path_to_LS.bool(simulation())

def λ_precondition_path_to_LW():
    return C_path_to_LW.bool(simulation())

def λ_precondition_path_to_RW():
    return C_path_to_RW.bool(simulation())

def λ_precondition_pass_options():
    return C_path_to_LS.bool(simulation()) or C_path_to_LW.bool(simulation()) or C_path_to_RW.bool(simulation())

def λ_precondition_LS_has_ball():
    return C_LS_has_ball.bool(simulation())

def λ_precondition_LW_has_ball():
    return C_LW_has_ball.bool(simulation())

def λ_precondition_RW_has_ball():
    return C_RW_has_ball.bool(simulation())

def λ_termination():
    return False



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