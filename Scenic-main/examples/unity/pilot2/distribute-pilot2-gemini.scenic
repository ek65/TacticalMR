from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

HasPath_LS = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 2.5, 'std': 0.5}})
HasPath_RS = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 2.5, 'std': 0.5}})
HasPath_LW = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger', 'path_width': {'avg': 2.5, 'std': 0.5}})
HasBallPossession_LS = HasBallPossession({'player': 'LeftStriker'})
HasBallPossession_RS = HasBallPossession({'player': 'RightStriker'})
HasBallPossession_LW = HasBallPossession({'player': 'LeftWinger'})
A1target_LS = HasPath({'obj1': 'LeftStriker', 'obj2': 'Coach', 'path_width': {'avg': 2.0, 'std': 0.5}})
A1target_RS = HasPath({'obj1': 'RightStriker', 'obj2': 'Coach', 'path_width': {'avg': 2.0, 'std': 0.5}})
A1target_LW = HasPath({'obj1': 'LeftWinger', 'obj2': 'Coach', 'path_width': {'avg': 2.0, 'std': 0.5}})
A1termination_LS = MakePass({'player': 'LeftStriker'})
A1termination_RS = MakePass({'player': 'RightStriker'})
A1termination_LW = MakePass({'player': 'LeftWinger'})

def λ_target_LS():
    return A1target_LS.dist(simulation(), ego=True)

def λ_target_RS():
    return A1target_RS.dist(simulation(), ego=True)

def λ_target_LW():
    return A1target_LW.dist(simulation(), ego=True)

def λ_precondition_pass_LS(scene, sample):
    return HasPath_LS.bool(simulation())

def λ_precondition_pass_RS(scene, sample):
    return HasPath_RS.bool(simulation())

def λ_precondition_pass_LW(scene, sample):
    return HasPath_LW.bool(simulation())

def λ_precondition_move_LS(scene, sample):
    return HasBallPossession_LS.bool(simulation())

def λ_precondition_move_RS(scene, sample):
    return HasBallPossession_RS.bool(simulation())

def λ_precondition_move_LW(scene, sample):
    return HasBallPossession_LW.bool(simulation())

def λ_termination_LS(scene, sample):
    return A1termination_LS.bool(simulation())

def λ_termination_RS(scene, sample):
    return A1termination_RS.bool(simulation())

def λ_termination_LW(scene, sample):
    return A1termination_LW.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I need to find an open teammate to pass to, let me evaluate my options.")
    if λ_precondition_pass_LS(simulation(), None):
        do Speak("The Left Striker is open, I will pass to him.")
        do Pass(LeftStriker)
        do Idle() until λ_precondition_move_LS(simulation(), None)
        do Speak("Now I will move to create a return passing lane for the Left Striker.")
        do MoveTo(λ_target_LS()) until λ_termination_LS(simulation(), None)
        do Speak("I'm in position. I'm ready for the return pass.")
        do StopAndReceiveBall()
    elif λ_precondition_pass_RS(simulation(), None):
        do Speak("The Right Striker has space, I'm passing to him.")
        do Pass(RightStriker)
        do Idle() until λ_precondition_move_RS(simulation(), None)
        do Speak("I'll get open for the Right Striker to pass back.")
        do MoveTo(λ_target_RS()) until λ_termination_RS(simulation(), None)
        do Speak("I'm ready to receive the ball back.")
        do StopAndReceiveBall()
    elif λ_precondition_pass_LW(simulation(), None):
        do Speak("The Left Winger is the best option, passing now.")
        do Pass(LeftWinger)
        do Idle() until λ_precondition_move_LW(simulation(), None)
        do Speak("I'll move into the gap to support the Left Winger.")
        do MoveTo(λ_target_LW()) until λ_termination_LW(simulation(), None)
        do Speak("I am open for a pass now.")
        do StopAndReceiveBall()
    else:
        do Speak("Nobody is open right now, so I will wait for an opportunity.")
        do Idle()
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