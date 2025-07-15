from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Let's get ball possession and scan for passing options.")
    do GetBallPossession(ball)
    do Speak("Wait until we have the ball under control.")
    do Idle() until λ_precondition_possession(simulation(), None)
    do Speak("Check for best open attacker to pass forward efficiently.")
    do Idle() until λ_precondition_pass(simulation(), None)
    if λ_precondition_striker(simulation(), None):
        do Speak("We have a clear passing lane to RightStriker. Pass to RightStriker to progress quickly.")
        do Pass(RightStriker)
        do Speak("Wait to see RightStriker receive the ball.")
        do Idle() until λ_termination_striker(simulation(), None)
    elif λ_precondition_winger(simulation(), None):
        do Speak("No open high striker, right winger is available. Pass to RightWinger into open space.")
        do Pass(RightWinger)
        do Speak("Wait to see RightWinger receive the ball.")
        do Idle() until λ_termination_winger(simulation(), None)
    else:
        do Speak("No clear forward pass, remain idle to reassess.")
        do Idle() for 2 seconds
    do Idle()


# Constraints
A_possession = HasBallPossession({'player': 'Coach'})
A_path_RS = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 2.0, 'std': 0.1}})
A_path_RW = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 2.0, 'std': 0.1}})
A_RS_open = InZone({'player': 'RightStriker', 'zone': 'C4'})
A_RW_open = InZone({'player': 'RightWinger', 'zone': 'A4'})
A_pass_RS = MakePass({'player': 'Coach'})
A_pass_RW = MakePass({'player': 'Coach'})
A_receive_RS = HasBallPossession({'player': 'RightStriker'})
A_receive_RW = HasBallPossession({'player': 'RightWinger'})

# Lambda targets and conditions

def λ_precondition_possession(scene, sample):
    return A_possession.bool(simulation())

def λ_precondition_pass(scene, sample):
    # Wait until Coach can detect either passing lane or zone opportunity opening up.
    return (A_path_RS.bool(simulation()) and A_RS_open.bool(simulation())) or (A_path_RW.bool(simulation()) and A_RW_open.bool(simulation()))

def λ_precondition_striker(scene, sample):
    # Is RightStriker both in advanced position and pass lane open?
    return A_path_RS.bool(simulation()) and A_RS_open.bool(simulation())

def λ_precondition_winger(scene, sample):
    # Is RightWinger a safe option, pass lane open?
    return A_path_RW.bool(simulation()) and A_RW_open.bool(simulation())

def λ_termination_striker(scene, sample):
    # Terminate waiting as soon as striker gets possession or ball leaves coach.
    return A_receive_RS.bool(simulation()) or not A_possession.bool(simulation())

def λ_termination_winger(scene, sample):
    # Terminate waiting as soon as winger gets possession or ball leaves coach.
    return A_receive_RW.bool(simulation()) or not A_possession.bool(simulation())




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