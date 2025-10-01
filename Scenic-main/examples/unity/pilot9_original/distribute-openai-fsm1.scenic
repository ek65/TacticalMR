from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_0 = DistanceTo({'from': 'Coach', 'to': 'RightStriker', 'min': None, 'max': {'avg': 9, 'std': 1}, 'operator': 'less_than'})
A2target_0 = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 2, 'std': 0.5}})
Aprecondition_0 = HasBallPossession({'player': 'Coach'})

def λ_target0():
    cond = A1target_0 & A2target_0
    return cond.dist(simulation(), ego=True)

def λ_precondition_0():
    return Aprecondition_0.bool(simulation())

Aprecondition_1 = HasBallPossession({'player': 'RightStriker'})
def λ_precondition_1():
    return Aprecondition_1.bool(simulation())

A1target_2 = AtAngle({'player': 'Coach', 'ball': 'ball', 'right': {'theta': {'avg': 46, 'std': 8}, 'dist': {'avg': 5, 'std': 1}}})
def λ_target2():
    return A1target_2.dist(simulation(), ego=True)

Aprecondition_2 = MakePass({'player': 'Coach'})
def λ_precondition_2():
    return Aprecondition_2.bool(simulation())

Aprecondition_3 = HasBallPossession({'player': 'Coach'})
def λ_precondition_3():
    return Aprecondition_3.bool(simulation())

def λ_termination_0():
    return False

# --- New constraint/logic for "IsOpen" targets ---

A_rightstriker_open = (A1target_0 & A2target_0)
def λ_rightstriker_open():
    # Returns True if RightStriker is open (under 9m, clear >2m path)
    return (A_rightstriker_open.bool(simulation()))

A1target_LS = DistanceTo({'from': 'Coach', 'to': 'LeftStriker', 'min': None, 'max': {'avg': 9, 'std': 1}, 'operator': 'less_than'})
A2target_LS = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 2, 'std': 0.5}})
A_leftstriker_open = (A1target_LS & A2target_LS)
def λ_leftstriker_open():
    return (A_leftstriker_open.bool(simulation()))

A1target_RW = DistanceTo({'from': 'Coach', 'to': 'RightWinger', 'min': None, 'max': {'avg': 9, 'std': 1}, 'operator': 'less_than'})
A2target_RW = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 2, 'std': 0.5}})
A_rightwinger_open = (A1target_RW & A2target_RW)
def λ_rightwinger_open():
    return (A_rightwinger_open.bool(simulation()))

A1target_LW = DistanceTo({'from': 'Coach', 'to': 'LeftWinger', 'min': None, 'max': {'avg': 9, 'std': 1}, 'operator': 'less_than'})
A2target_LW = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger', 'path_width': {'avg': 2, 'std': 0.5}})
A_leftwinger_open = (A1target_LW & A2target_LW)
def λ_leftwinger_open():
    return (A_leftwinger_open.bool(simulation()))

# --- END new constraints ---

behavior CoachBehavior():
    # 1. Wait at the start
    do Idle() for 3 seconds

    # 2. Wait until possession before taking first action
    do Speak("Wait until I have the ball before taking the first action")
    do Idle() until λ_precondition_0()

    # 3. Conditional passing logic based on who is open (RightStriker > LeftStriker > RightWinger > LeftWinger)
    do Speak("Check for an open forward to pass to: right striker, left striker, otherwise winger")
    if λ_rightstriker_open():
        do Speak("RightStriker is open, pass the ball to RightStriker: pass must be under 9 meters with at least 2m clear path")
        do Pass('RightStriker')
    elif λ_leftstriker_open():
        do Speak("LeftStriker is open, pass the ball to LeftStriker: pass must be under 9 meters with at least 2m clear path")
        do Pass('LeftStriker')
    elif λ_rightwinger_open():
        do Speak("RightWinger is open, pass the ball to RightWinger: pass must be under 9 meters with at least 2m clear path")
        do Pass('RightWinger')
    else:
        do Speak("LeftWinger is open or fallback, pass the ball to LeftWinger: pass must be under 9 meters with at least 2m clear path")
        do Pass('LeftWinger')

    # 4. Move immediately after the pass, do NOT wait for receiver
    do Speak("Immediately reposition to form a triangle, moving to a support spot")
    do MoveTo(λ_target2(), False)
    
    # 5. FSM always ends with Idle
    do Speak("Repositioned to create a triangle; finish with a pause.")
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