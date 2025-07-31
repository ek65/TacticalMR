from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1precondition_0 = HasBallPossession({'player': 'Coach'})
A1precondition_1 = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 3, 'std': 0.2}})
A1precondition_2 = HasBallPossession({'player': 'RightStriker'})
A1precondition_3 = HasPath({'obj1': 'RightStriker', 'obj2': 'Coach', 'path_width': {'avg': 3, 'std': 0.2}})
A1precondition_4 = HasBallPossession({'player': 'LeftWinger'})
A1precondition_5 = HasPath({'obj1': 'LeftWinger', 'obj2': 'LeftStriker', 'path_width': {'avg': 3, 'std': 0.2}})

def λ_precondition_0(scene, sample):
    # Coach has ball possession
    return A1precondition_0.bool(simulation())

def λ_precondition_1(scene, sample):
    # Coach has clear path to RightStriker, path width > 3m
    return A1precondition_1.bool(simulation())

def λ_precondition_2(scene, sample):
    # RightStriker has ball possession
    return A1precondition_2.bool(simulation())

def λ_precondition_3(scene, sample):
    # RightStriker has clear path to Coach, path width > 3m
    return A1precondition_3.bool(simulation())

def λ_precondition_4(scene, sample):
    # LeftWinger has ball possession
    return A1precondition_4.bool(simulation())

def λ_precondition_5(scene, sample):
    # LeftWinger has clear path to LeftStriker, path width > 3m
    return A1precondition_5.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Waiting for ball possession.")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Check if passing lane to right striker is open with at least 3 meters width.")
    if λ_precondition_1(simulation(), None):
        do Speak("Right striker is open. Pass to right striker immediately.")
        do Pass(RightStriker)
        do Speak("Wait until right striker receives the ball.")
        do Idle() until λ_precondition_2(simulation(), None)
        do Speak("Make a supporting run to open space after the pass.")
        # Move into supporting space - not toward a teammate, but toward space.
        # We'll treat this as MoveTo a position more than 8m away from nearest defender and 6m from ball.
        A_support_space = DistanceTo({'from': 'Coach', 'to': 'ball', 'min': {'avg': 6, 'std':0.1}, 'max': None, 'operator': 'greater_than'})
        def λ_target_support_space():
            return A_support_space.dist(simulation(), ego=True)
        do MoveTo(λ_target_support_space(), False)
        do Speak("Wait to see if right striker passes back, clear lane needed.")
        do Idle() until λ_precondition_3(simulation(), None)
        do Speak("Receive ball from right striker.")
        do StopAndReceiveBall()
        do Speak("Attempt shot on goal after receiving ball.")
        do Shoot(goal)
    else:
        do Speak("Deep option not open; check if left winger can receive.")
        if λ_precondition_4(simulation(), None):
            do Speak("Pass to left winger.")
            do Pass(LeftWinger)
            do Speak("Wait until left winger receives the ball.")
            do Idle() until λ_precondition_4(simulation(), None)
            do Speak("Hold midfield position, stay available for pass back, no forward run.")
            do Idle()
        else:
            do Speak("Hold position as no safe pass available.")
            do Idle()
    do Idle()

####Environment Behavior START####

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