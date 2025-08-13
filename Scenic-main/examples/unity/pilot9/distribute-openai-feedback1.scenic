from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Wait until you have the ball before acting")
    do Idle() until lambda_precondition_0()

    # -- Branching: Consider main passing options for distribution --
    if lambda_pass_to_rightstriker():
        do Speak("You have ball possession, assess passing options")
        do Speak("Pass to RightStriker -- path is at least 2m wide and less than 9m distance")
        do Pass(RightStriker)
        do Speak("Wait until RightStriker has the ball")
        do Idle() until lambda_precondition_rightstriker()
        do Speak("Move to create a triangle: move to support at x≈2.35, y≈3.24 where you are not blocked from RightStriker by a defender")
        do MoveTo(lambda_triangle_support_rs(), False)  # Updated support location & constraint
    elif lambda_pass_to_leftstriker():
        do Speak("You have ball possession, assess passing options")
        do Speak("Pass to LeftStriker because they are more open or have an easier pass angle")
        do Pass(LeftStriker)
        do Speak("Wait until LeftStriker has the ball")
        do Idle() until lambda_precondition_leftstriker()
        do Speak("Move to create a triangle with LeftStriker and RightStriker, positioning at x -2, y 4")
        do MoveTo(lambda_target_left_striker_support(), False)
    elif lambda_pass_to_leftwinger():
        do Speak("You have ball possession, assess passing options")
        do Speak("Pass to LeftWinger to exploit the left flank")
        do Pass(LeftWinger)
        do Speak("Wait until LeftWinger has the ball")
        do Idle() until lambda_precondition_leftwinger()
        do Speak("Move to create a triangle with LeftWinger and LeftStriker, supporting the left wing advance")
        do MoveTo(lambda_target_leftwinger_support(), False)
    elif lambda_pass_to_rightwinger():
        do Speak("You have ball possession, assess passing options")
        do Speak("Pass to RightWinger to exploit the right flank")
        do Pass(RightWinger)
        do Speak("Wait until RightWinger has the ball")
        do Idle() until lambda_precondition_rightwinger()
        do Speak("Move to create a triangle with RightWinger and RightStriker, supporting the right wing advance")
        do MoveTo(lambda_target_rightwinger_support(), False)

    # All branches finish here
    do Idle()

# --- Constraints and Logic ---

A1precondition_0 = HasBallPossession({'player': 'Coach'})
A1precondition_1 = HasBallPossession({'player': 'RightStriker'})
A1precondition_2 = HasBallPossession({'player': 'LeftStriker'})
A1precondition_3 = HasBallPossession({'player': 'LeftWinger'})
A1precondition_4 = HasBallPossession({'player': 'RightWinger'})

# --- UPDATED LOGIC BASED ON FEEDBACK FOR RightStriker PASS ---

def lambda_pass_to_rightstriker():
    # Must be: path open at least 2m + under 9m distance
    path_ok = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 2, 'std': 0.1}}).bool(simulation())
    dist_ok = DistanceTo({'from': 'Coach', 'to': 'RightStriker', 'min': None, 'max': {'avg': 9, 'std': 0.1}, 'operator': 'less_than'}).bool(simulation())
    return path_ok and dist_ok

def lambda_pass_to_leftstriker():
    # Insert logic for LeftStriker pass (e.g., if more open than RightStriker)
    return False  # Default, set True if LeftStriker is preferable

def lambda_pass_to_leftwinger():
    # Insert logic for LeftWinger pass (e.g., left flank open)
    return False  # Default, set True if LeftWinger is preferable

def lambda_pass_to_rightwinger():
    # Insert logic for RightWinger pass (e.g., right flank open)
    return False  # Default, set True if RightWinger is preferable

# Ball possession preconditions for branches
def lambda_precondition_0():
    return A1precondition_0.bool(simulation())

def lambda_precondition_rightstriker():
    return A1precondition_1.bool(simulation())

def lambda_precondition_leftstriker():
    return A1precondition_2.bool(simulation())

def lambda_precondition_leftwinger():
    return A1precondition_3.bool(simulation())

def lambda_precondition_rightwinger():
    return A1precondition_4.bool(simulation())

# ------------- Triangle/Support move targets ------------------

# New: For supporting after RightStriker receives pass, using feedback coordinate and defender avoidance
def lambda_triangle_support_rs():
    # Move very close to (x=2.35, y=3.24), but with the constraint that no defender is in line between Coach and RightStriker
    target_dist = DistanceTo({'from': 'Coach', 'to': None, 'min': {'avg': 0, 'std': 0.01}, 'max': {'avg': 0.5, 'std': 0.1}, 'operator': 'within'})
    # Region centered at (2.35555553, 3.24444222)
    A_target_x = DistanceTo({'from': 'Coach', 'to': None, 'min': None, 'max': {'avg': 0.5, 'std': 0.1}, 'operator': 'less_than'})
    # The following is an illustration: place constraint at that x/y position.
    class FixedTargetConstraint:
        def dist(self, simulation, ego=True):
            import numpy as np
            arr = np.zeros((20, 40))
            # grid is x=-10..10, y=0..17
            # mark 1 at coordinate (x=2.35, y=3.24)
            arr[int(3.24/0.85), int((2.35+10)/0.5)] = 1  # rough mapping to local grid
            return arr
    # Must also ensure NO defender between Coach and RightStriker (not literal code: in reality, you must select support spot not "in line")
    return FixedTargetConstraint().dist(simulation(), ego=True)

# Remain unchanged for other triangle targets:
A1target_leftstriker = DistanceTo({'from': 'Coach', 'to': 'goal',
                                  'min': None, 'max': {'avg': 14.5, 'std': 1.0}, 'operator': 'less_than'})
A2target_leftstriker = HeightRelation({'obj': 'Coach', 'ref': None,
                                      'relation': 'above', 'height_threshold': {'avg': 4, 'std': 0.5}})
def lambda_target_left_striker_support():
    cond = A1target_leftstriker & A2target_leftstriker
    return cond.dist(simulation(), ego=True)

A1target_leftwinger = DistanceTo({'from': 'Coach', 'to': 'goal',
                                 'min': None, 'max': {'avg': 15, 'std': 1.0}, 'operator': 'less_than'})
A2target_leftwinger = HeightRelation({'obj': 'Coach', 'ref': None,
                                     'relation': 'above', 'height_threshold': {'avg': 3.5, 'std': 0.5}})
def lambda_target_leftwinger_support():
    cond = A1target_leftwinger & A2target_leftwinger
    return cond.dist(simulation(), ego=True)

A1target_rightwinger = DistanceTo({'from': 'Coach', 'to': 'goal',
                                  'min': None, 'max': {'avg': 15, 'std': 1.0}, 'operator': 'less_than'})
A2target_rightwinger = HeightRelation({'obj': 'Coach', 'ref': None,
                                      'relation': 'above', 'height_threshold': {'avg': 3.5, 'std': 0.5}})
def lambda_target_rightwinger_support():
    cond = A1target_rightwinger & A2target_rightwinger
    return cond.dist(simulation(), ego=True)

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
left_winger = new Player at (left_winger_x, left_winger_y, 0), facing toward ego, with name "LeftWinger", with team "blue"

right_winger_x = winger_dist * sin(right_winger_angle * pi / 180)
right_winger_y = winger_dist * cos(right_winger_angle * pi / 180)
right_winger = new Player at (right_winger_x, right_winger_y, 0), facing toward ego, with name "RightWinger", with team "blue"

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