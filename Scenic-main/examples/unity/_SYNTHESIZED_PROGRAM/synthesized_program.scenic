from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_move1 = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 2, 'std': 0.2}})
A2target_move1 = DistanceTo({'from': 'Coach', 'to': 'Defender4', 'min': {'avg': 3, 'std': 0.3}, 'max': None, 'operator': 'greater_than'})
A3target_move1 = DistanceTo({'from': 'Coach', 'to': 'LeftStriker', 'min': {'avg': 7, 'std': 0.3}, 'max': {'avg': 13, 'std': 0.2}, 'operator': 'within'})

A1precondition_pass = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 2, 'std': 0.2}})
A2precondition_pass = HasBallPossession({'player': 'Coach'})

A1precondition_support = HasBallPossession({'player': 'LeftStriker'})

# Supporting position after pass (as in demo_1): X=-2.39, Y=2.76, about 4 meters from both current Coach and LeftStriker
A1target_support = DistanceTo({'from': 'Coach', 'to': 'LeftStriker', 'min': {'avg': 3.5, 'std': 0.2}, 'max': {'avg': 5, 'std': 0.2}, 'operator': 'within'})
# Also keep clear of nearby defender
A2target_support = DistanceTo({'from': 'Coach', 'to': 'Defender4', 'min': {'avg': 3, 'std': 0.2}, 'max': None, 'operator': 'greater_than'})

def λ_target_move1():
    cond = A1target_move1 & A2target_move1 & A3target_move1
    return cond.dist(simulation(), ego=True)

def λ_target_support():
    cond = A1target_support & A2target_support
    return cond.dist(simulation(), ego=True)

def λ_precondition_pass():
    cond = A1precondition_pass & A2precondition_pass
    return cond.bool(simulation())

def λ_precondition_support():
    return A1precondition_support.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Wait 3 seconds to observe ball possession and teammates' positioning.")
    do Speak("Move to spot with clear 2m passing lane to LeftStriker, more than 3m from Defender4.")
    do MoveTo(λ_target_move1(), False)
    do Speak("Wait until you have ball and passing lane to LeftStriker is clear (2m path width).")
    do Idle() until λ_precondition_pass()
    do Speak("Angle and lane to LeftStriker is clear; pass ball to LeftStriker.")
    do Pass("LeftStriker")
    do Speak("Wait until LeftStriker has the ball before supporting.")
    do Idle() until λ_precondition_support()
    do Speak("Move to support: 3.5-5m from LeftStriker, more than 3m from nearest defender.")
    do MoveTo(λ_target_support(), False)
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