from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# Constraint Instantiations
path_to_LeftStriker = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 2.5, 'std': 0.5}})
path_to_RightStriker = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 2.5, 'std': 0.5}})
path_to_LeftWinger = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger', 'path_width': {'avg': 2.5, 'std': 0.5}})
path_to_RightWinger = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 2.5, 'std': 0.5}})

precondition_LeftStriker_has_ball = HasBallPossession({'player': 'LeftStriker'})
precondition_RightWinger_has_ball = HasBallPossession({'player': 'RightWinger'})
precondition_LeftWinger_has_ball = HasBallPossession({'player': 'LeftWinger'})

target_for_support = AtAngle({'player': 'Coach', 'ball': 'ball', 'left': {'theta': {'avg': 50, 'std': 5}, 'dist': {'avg': 8, 'std': 1}}, 'right': {'theta': {'avg': 50, 'std': 5}, 'dist': {'avg': 8, 'std': 1}}})

# Lambda Functions
def λ_path_to_LS_is_clear():
    return path_to_LeftStriker.bool(simulation())

def λ_path_to_RS_is_clear():
    return path_to_RightStriker.bool(simulation())

def λ_path_to_LW_is_clear():
    return path_to_LeftWinger.bool(simulation())

def λ_path_to_RW_is_clear():
    return path_to_RightWinger.bool(simulation())

def λ_precondition_LS_has_ball():
    return precondition_LeftStriker_has_ball.bool(simulation())

def λ_precondition_RW_has_ball():
    return precondition_RightWinger_has_ball.bool(simulation())

def λ_precondition_LW_has_ball():
    return precondition_LeftWinger_has_ball.bool(simulation())

def λ_target_support_position():
    return target_for_support.dist(simulation(), ego=True)

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I have the ball. I need to scan the field and find an open teammate for a pass.")
    
    if not λ_path_to_LS_is_clear() and not λ_path_to_RS_is_clear():
        if λ_path_to_RW_is_clear():
            do Speak("The strikers are covered. I'll pass it to the Right Winger on the flank.")
            do Pass(RightWinger)
            do Speak("Now I'll wait for the winger to receive the ball before moving.")
            do Idle() until λ_precondition_RW_has_ball()
            do Speak("Great, now I'll move into a supporting position for the Right Winger.")
            do MoveTo(λ_target_support_position())
        elif λ_path_to_LW_is_clear():
            do Speak("Both strikers are marked by defenders. I see the Left Winger is open.")
            do Pass(LeftWinger)
            do Speak("I need to wait for the Left Winger to secure the ball.")
            do Idle() until λ_precondition_LW_has_ball()
            do Speak("Okay, now I will reposition to act as a support player.")
            do MoveTo(λ_target_support_position())
        else:
            do Speak("All my forward passing options are blocked. I'll hold onto the ball for now.")
            do Idle()
    elif λ_path_to_LS_is_clear():
        do Speak("The defender isn't covering the Left Striker. That's my best option.")
        do Pass(LeftStriker)
        do Speak("I'll wait until the striker has possession.")
        do Idle() until λ_precondition_LS_has_ball()
        do Speak("Now that he has the ball, I will move to give him a passing option.")
        do MoveTo(λ_target_support_position())
    else:
        do Speak("There are no clear passing lanes right now. I'll wait for an opportunity.")
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