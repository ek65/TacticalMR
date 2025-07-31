from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_0 = HasBallPossession({'player': 'Coach'})
A2target_1 = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger',
                      'path_width': {'avg': 2.0, 'std': 0.2}})
A3target_1 = DistanceTo({'from': 'Coach', 'to': 'LeftWinger',
                        'min': {'avg': 8.0, 'std': 0.5},
                        'max': {'avg': 18.0, 'std': 1.0},
                        'operator': 'within'})
A1precondition_2 = HasBallPossession({'player': 'LeftWinger'})
A1target_3 = DistanceTo({'from': 'Coach', 'to': 'ball',
                        'min': {'avg': 6.0, 'std': 0.3},
                        'max': {'avg': 12.0, 'std': 0.5},
                        'operator': 'within'})

# Added constraints to check for open paths and possession for other players,
# enabling more dynamic decision-making as per the coach's feedback.
A_path_LS = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker',
                     'path_width': {'avg': 2.0, 'std': 0.2}})
A_path_RS = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker',
                     'path_width': {'avg': 2.0, 'std': 0.2}})
A_path_RW = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger',
                     'path_width': {'avg': 2.0, 'std': 0.2}})
A_poss_LS = HasBallPossession({'player': 'LeftStriker'})
A_poss_RS = HasBallPossession({'player': 'RightStriker'})
A_poss_RW = HasBallPossession({'player': 'RightWinger'})


def λ_target0():
    return A1target_0.bool(simulation())


def λ_target1():
    cond = A2target_1 and A3target_1
    return cond.dist(simulation(), ego=True)


def λ_precondition_2(scene, sample):
    return A1precondition_2.bool(simulation())


def λ_target3():
    return A1target_3.dist(simulation(), ego=True)


# Added lambda functions to evaluate the new path and possession constraints.
# These are used in the updated CoachBehavior to decide which player to pass to.
def λ_path_to_LS():
    return A_path_LS.bool(simulation())


def λ_path_to_RS():
    return A_path_RS.bool(simulation())


def λ_path_to_LW():
    return A2target_1.bool(simulation())


def λ_path_to_RW():
    return A_path_RW.bool(simulation())


def λ_precondition_LS(scene, sample):
    return A_poss_LS.bool(simulation())


def λ_precondition_RS(scene, sample):
    return A_poss_RS.bool(simulation())


def λ_precondition_RW(scene, sample):
    return A_poss_RW.bool(simulation())


behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Get ready—possess the ball and scan for passing options.")
    do MoveToBallAndGetPossession()

    # Wait until the coach has possession before deciding on the next action.
    do Idle() until λ_target0()
    do Speak("I have the ball, now checking for passing options.")

    # Modified the behavior to include conditional passing logic based on feedback.
    # The coach now prioritizes passing to an open striker, and falls back to
    # passing to a winger if the strikers are blocked by defenders.
    if λ_path_to_LS():
        do Speak("Clear path to Left Striker. Passing now.")
        do Pass(LeftStriker)
        do Speak("Waiting for Left Striker to receive the ball.")
        do Idle() until λ_precondition_LS(simulation(), None)
    elif λ_path_to_RS():
        do Speak("Clear path to Right Striker. Passing now.")
        do Pass(RightStriker)
        do Speak("Waiting for Right Striker to receive the ball.")
        do Idle() until λ_precondition_RS(simulation(), None)
    elif λ_path_to_LW():
        do Speak("Strikers are covered. Making a safe pass to the Left Winger.")
        do Pass(LeftWinger)
        do Speak("Wait for LeftWinger to receive the ball before moving.")
        do Idle() until λ_precondition_2(simulation(), None)
    elif λ_path_to_RW():
        do Speak("Strikers are covered. Making a safe pass to the Right Winger.")
        do Pass(RightWinger)
        do Speak("Waiting for Right Winger to receive the ball.")
        do Idle() until λ_precondition_RW(simulation(), None)
    else:
        # Fallback action: if all other options are blocked, default to a safe pass to the Left Winger.
        do Speak("All preferred options are blocked. Defaulting to a safe pass to Left Winger.")
        do Pass(LeftWinger)
        do Speak("Wait for LeftWinger to receive the ball before moving.")
        do Idle() until λ_precondition_2(simulation(), None)

    do Speak("Move into half-space to open up and provide an option.")
    do MoveTo(λ_target3(), False)
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