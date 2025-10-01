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
    # Termination is not based on success of the pass or triangle, just on a pause.
    return False

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Wait until I have the ball before taking the first action")
    do Idle() until λ_precondition_0()
    do Speak("Pass the ball to RightStriker: pass must be under 9 meters with at least 2m clear path")
    do Pass(RightStriker)
    do Speak("Wait until RightStriker receives the ball")
    do Idle() until λ_precondition_1()
    do Speak("Move right and upfield, at about 46 degrees from the ball, about 5 meters")
    do MoveTo(λ_target2(), False)
    do Speak("Wait until you see the ball being passed again")
    do Idle() until λ_precondition_2()
    do Speak("Once I receive the ball again, pause and look for options")
    do Idle() until λ_precondition_3()
    do Speak("You've now repositioned to form a triangle for distribution, giving multiple passing options")
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