from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1_has_possession = HasBallPossession({'player': 'Coach'})
A2_leftstriker_open = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 2.2, 'std': 0.1}})
A3_def1_guarding_right = DistanceTo({'from': 'Defender1', 'to': 'Coach', 'min': None, 'max': {'avg': 2.0, 'std': 0.2}, 'operator': 'less_than'})
A4_leftstriker_spacing = DistanceTo({'from': 'Defender4', 'to': 'LeftStriker', 'min': {'avg': 3.7, 'std': 0.2}, 'max': None, 'operator': 'greater_than'})
A5_path_clear = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 2.2, 'std': 0.1}})
A6_pass_to_leftstriker = MakePass({'player': 'Coach'})
A7_leftstriker_receives = HasBallPossession({'player': 'LeftStriker'})
A8_at_angle_for_return_pass = AtAngle({'player': 'Coach', 'ball': 'ball', 'left': {'theta': {'avg': 50, 'std': 5}, 'dist': {'avg': 4.0, 'std': 0.3}}})
A9_def1_cannot_defend = HasPath({'obj1': 'LeftStriker', 'obj2': 'Coach', 'path_width': {'avg': 2.3, 'std': 0.2}})
A10_def4_cannot_defend = HasPath({'obj1': 'LeftStriker', 'obj2': 'Coach', 'path_width': {'avg': 2.3, 'std': 0.2}})
A11_stop_n_receive = HasBallPossession({'player': 'Coach'})

def λ_precondition_possession(scene, sample):
    return A1_has_possession.bool(simulation())

def λ_precondition_leftstriker_open(scene, sample):
    return A2_leftstriker_open.bool(simulation()) and A4_leftstriker_spacing.bool(simulation())

def λ_termination_possession(scene, sample):
    # Terminate when possession changes (i.e., when Coach passes)
    return not A1_has_possession.bool(simulation())

def λ_termination_return_pass(scene, sample):
    # Terminate showing run when Coach receives the ball (possession switches back to Coach)
    return A11_stop_n_receive.bool(simulation())

def λ_precondition_show_for_angle(scene, sample):
    return A7_leftstriker_receives.bool(simulation())

def λ_termination_show_for_angle(scene, sample):
    # Terminate when Coach is at desired angle for return pass (AtAngle satisfied)
    return A8_at_angle_for_return_pass.bool(simulation())

def λ_precondition_return_angle(scene, sample):
    return (A8_at_angle_for_return_pass.bool(simulation())
            and A9_def1_cannot_defend.bool(simulation())
            and A10_def4_cannot_defend.bool(simulation()))

def λ_termination_stop_receive(scene, sample):
    # Terminate when ball returns and Coach gets possession
    return A11_stop_n_receive.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Check for ball possession before making next move")
    do Idle() until λ_precondition_possession(simulation(), None)
    do Speak("Look for open passing lane to LeftStriker with space from Defender4")
    do Idle() until λ_precondition_leftstriker_open(simulation(), None)
    do Speak("Pass to open LeftStriker, avoid right-side defender")
    do Pass(LeftStriker)
    do Speak("Wait until LeftStriker receives the ball and you can make yourself available")
    do Idle() until λ_precondition_show_for_angle(simulation(), None)
    do Speak("Move into an angle to support a return pass that defeats defenders")
    do MoveTo(λ_show_for_angle())
    do Speak("Wait until you are at the correct angle for the return")
    do Idle() until λ_precondition_return_angle(simulation(), None)
    do Speak("Stop and become ready to receive a return pass from LeftStriker")
    do StopAndReceiveBall()
    do Idle()

def λ_show_for_angle():
    return A8_at_angle_for_return_pass.dist(simulation(), ego=True)

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