from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1precondition_possession = HasBallPossession({'player': 'Coach'})
A1target_leftstriker = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 3, 'std': 1}})
A1target_rightwinger = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 3, 'std': 1}})
A1target_rightstriker = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 3, 'std': 1}})
A1target_horizontal = DistanceTo({'from': 'Coach', 'to': 'RightWinger', 'min': {'avg': 0, 'std': 0.5}, 'max': {'avg': 2, 'std': 1}, 'operator': 'within'})
A2target_horizontal = DistanceTo({'from': 'Coach', 'to': 'LeftStriker', 'min': {'avg': 0, 'std': 0.5}, 'max': {'avg': 10, 'std': 1}, 'operator': 'within'})
Aprecondition_path_leftstriker = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 3, 'std': 1}})
Aprecondition_path_rightstriker = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 3, 'std': 1}})
Aprecondition_path_rightwinger = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 3, 'std': 1}})

def λ_precondition_possession():
    return A1precondition_possession.bool(simulation())

def λ_precondition_path_leftstriker():
    return Aprecondition_path_leftstriker.bool(simulation())

def λ_precondition_path_rightstriker():
    return Aprecondition_path_rightstriker.bool(simulation())

def λ_precondition_path_rightwinger():
    return Aprecondition_path_rightwinger.bool(simulation())

def λ_target_horizontal():
    cond = A1target_horizontal
    return cond.dist(simulation(), ego=True)

def λ_target_leftstriker():
    return A1target_leftstriker.bool(simulation())

def λ_target_rightstriker():
    return A1target_rightstriker.bool(simulation())

def λ_target_rightwinger():
    return A1target_rightwinger.bool(simulation())


behavior CoachBehavior():
    do Idle() for 3 seconds
    # do Speak("Wait until Coach receives ball possession before taking any action")
    # do Idle() until λ_precondition_possession()
    # do Speak("Coach now has possession, choose your passing option based on who is free")
    # if λ_precondition_path_rightstriker():
        # do Speak("There is a clear passing lane of about 3 meters to RightStriker. Pass to RightStriker for fastest progression.")
        # do Pass(RightStriker)
    # elif λ_precondition_path_leftstriker():
    #     do Speak("There is a clear path of 3 meters wide to LeftStriker. Pass to LeftStriker if no better forward option.")
    #     do Pass(LeftStriker)
    # elif λ_precondition_path_rightwinger():
    #     do Speak("No strikers open forward, but clear 3m wide pass to RightWinger. Use horizontal pass to maintain possession.")
    #     do Pass(RightWinger)
    # else:
    #     do Speak("No safe forward pass. Move horizontally within 10 meters to support build-up play.")
    do MoveTo(Vector(-8,1,0), False)
    do Idle()

####Environment Behavior START####

# Ego (center midfielder) at origin
pi = 3.1415
ego = new Coach at (0, 0, 0), facing toward (0, 0, 0), with team "blue", with behavior CoachBehavior()

# Wingers
left_winger_angle = 90 + Range(0, 10)  # degrees from y-axis, 90 is positive x-axis (left), variance +/-10
right_winger_angle = -90 + Range(0, 10)  # degrees from y-axis, -90 is negative x-axis (right), variance +/-10
winger_dist = Range(6,8)

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