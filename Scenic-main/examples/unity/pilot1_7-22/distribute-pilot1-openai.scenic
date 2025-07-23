from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####
A1HasBallPossession = HasBallPossession({'player': 'Coach'})
A1HasPath_LS = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_RW = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_RS = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_High = HasPath({'obj1': 'Coach', 'obj2': 'HighestPlayer', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_Horiz = HasPath({'obj1': 'Coach', 'obj2': 'HorizontalPlayer', 'path_width': {'avg': 2.3, 'std': 0.2}})
A1HasPath_SafeStriker = HasPath({'obj1': 'Coach', 'obj2': 'SafeStriker', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_BestStriker = HasPath({'obj1': 'Coach', 'obj2': 'BestStriker', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_ClearStriker = HasPath({'obj1': 'Coach', 'obj2': 'ClearStriker', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_Forward = HasPath({'obj1': 'Coach', 'obj2': 'ForwardPlayer', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_Striker = HasPath({'obj1': 'Coach', 'obj2': 'Striker', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_LeftAvail = HasPath({'obj1': 'Coach', 'obj2': 'LeftAvailable', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_RightAvail = HasPath({'obj1': 'Coach', 'obj2': 'RightAvailable', 'path_width': {'avg': 2.4, 'std': 0.2}})
A1HasPath_HorizAvail = HasPath({'obj1': 'Coach', 'obj2': 'HorizontalAvailable', 'path_width': {'avg': 2.3, 'std': 0.2}})
A1HasPath_StrikerSingle = HasPath({'obj1': 'Coach', 'obj2': 'SingleStriker', 'path_width': {'avg': 2.4, 'std': 0.2}})

A1MakePass_LS = MakePass({'player': 'Coach'})
A1MakePass_RW = MakePass({'player': 'Coach'})
A1MakePass_RS = MakePass({'player': 'Coach'})
A1MakePass_High = MakePass({'player': 'Coach'})

def λ_termination_MoveToBestPass(scene, sample):
    # Terminate if situation changes (not the goal of pass itself): e.g., defender closes path or Coach loses possession
    return not A1HasBallPossession.bool(simulation()) or \
           (not A1HasPath_ClearStriker.bool(simulation()) and 
            not A1HasPath_Forward.bool(simulation()))
           
def λ_target_pass_clearStriker():
    return A1HasPath_ClearStriker.dist(simulation(), ego=True)
def λ_target_pass_RS():
    return A1HasPath_RS.dist(simulation(), ego=True)
def λ_target_pass_RW():
    return A1HasPath_RW.dist(simulation(), ego=True)
def λ_target_pass_LS():
    return A1HasPath_LS.dist(simulation(), ego=True)
def λ_target_pass_High():
    return A1HasPath_High.dist(simulation(), ego=True)
def λ_target_pass_Horiz():
    return A1HasPath_Horiz.dist(simulation(), ego=True)

def λ_target_pass_Striker():
    return A1HasPath_Striker.dist(simulation(), ego=True)

def λ_precondition_hasPossession(scene, sample):
    return A1HasBallPossession.bool(simulation())

def λ_precondition_forwardClear(scene, sample):
    return A1HasPath_ClearStriker.bool(simulation()) or \
           A1HasPath_Forward.bool(simulation())

def λ_precondition_highest(scene, sample):
    return A1HasPath_High.bool(simulation())

def λ_precondition_horiz_and_other(scene, sample):
    return A1HasPath_Horiz.bool(simulation()) or \
           A1HasPath_HorizAvail.bool(simulation())

def λ_termination_idle(scene, sample):
    # As an intermediate step, terminate when ball is possessed or possession changes
    return not A1HasBallPossession.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Be aware of your options and surroundings immediately.")
    do Idle() until λ_precondition_hasPossession(simulation(), None)
    do Speak("Look forward to the highest player, seek best clear passing path.")
    if λ_precondition_forwardClear(simulation(), None):
        do Pass(ClearStriker)
    elif λ_precondition_highest(simulation(), None):
        do Speak("Pass to highest or most advanced field player.")
        do Pass(HighestPlayer)
    elif λ_precondition_horiz_and_other(simulation(), None):
        do Speak("Forward pass not open; look to horizontal teammate.")
        do Pass(HorizontalPlayer)
    else:
        do Speak("If none clear, seek safest available striker or hold the ball.")
        do Idle() until λ_termination_idle(simulation(), None)
    do Idle()
####Environment Behavior START####


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