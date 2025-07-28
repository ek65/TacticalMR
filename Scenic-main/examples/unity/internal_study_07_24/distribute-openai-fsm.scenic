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
    do Speak("I have the ball. Evaluating best passing option now.")
    # Wait until the coach has possession of the ball before making a decision
    do Idle() until λ_precondition_0(simulation(), None)

    # FIX: Replaced the static pass action with a dynamic decision-making process.
    # The coach now evaluates each potential passing option based on whether there's a clear path.
    # The if/elif structure prioritizes passing to strikers over wingers if both are open.
    if λ_path_to_RightStriker(simulation(), None):
        do Speak("prepare to pass to the most forward open striker")
        do Pass(RightStriker)
        do Speak("move up to support the striker after pass")
        # FIX: The condition now correctly waits for the RightStriker to get the ball.
        do Idle() until λ_poss_RightStriker(simulation(), None)
        do Speak("move forward to support and provide a passing option")
        # FIX: The MoveTo target is now specific to supporting the RightStriker.
        do MoveTo(λ_target_support_RS())
    elif λ_path_to_LeftStriker(simulation(), None):
        # FIX: Added new logic branch to consider passing to the LeftStriker.
        do Speak("prepare to pass to the other open striker")
        do Pass(LeftStriker)
        do Speak("move up to support the striker after pass")
        do Idle() until λ_poss_LeftStriker(simulation(), None)
        do Speak("move forward to support and provide a passing option")
        do MoveTo(λ_target_support_LS())
    elif λ_path_to_RightWinger(simulation(), None):
        # FIX: Added new logic branch to consider passing to the RightWinger.
        do Speak("prepare to pass to the open right winger")
        do Pass(RightWinger)
        do Speak("move up to support the winger after pass")
        do Idle() until λ_poss_RightWinger(simulation(), None)
        do Speak("move forward to support and provide a passing option")
        do MoveTo(λ_target_support_RW())
    elif λ_path_to_LeftWinger(simulation(), None):
        # FIX: Added new logic branch to consider passing to the LeftWinger.
        do Speak("prepare to pass to the open left winger")
        do Pass(LeftWinger)
        do Speak("move up to support the winger after pass")
        do Idle() until λ_poss_LeftWinger(simulation(), None)
        do Speak("move forward to support and provide a passing option")
        do MoveTo(λ_target_support_LW())

    do Idle()

# Constraints and Condition APIs

# Coach receives or gets possession of the ball:
A1precondition_0 = HasBallPossession({'player': 'Coach'})

# FIX: Added HasPath constraints to check for open passing lanes to all teammates, as per feedback.
# This models the "is the teammate open?" condition.
A1precondition_path_RS = HasPath({
    'obj1': 'Coach',
    'obj2': 'RightStriker',
    'path_width': {'avg': 1.5, 'std': 0.5}
})
A1precondition_path_LS = HasPath({
    'obj1': 'Coach',
    'obj2': 'LeftStriker',
    'path_width': {'avg': 1.5, 'std': 0.5}
})
A1precondition_path_RW = HasPath({
    'obj1': 'Coach',
    'obj2': 'RightWinger',
    'path_width': {'avg': 1.5, 'std': 0.5}
})
A1precondition_path_LW = HasPath({
    'obj1': 'Coach',
    'obj2': 'LeftWinger',
    'path_width': {'avg': 1.5, 'std': 0.5}
})

# FIX: Replaced old A1precondition_1 with a full set of possession checks for each potential receiver.
# This ensures the coach waits until the correct teammate has received the pass.
A1postcondition_poss_RS = HasBallPossession({'player': 'RightStriker'})
A1postcondition_poss_LS = HasBallPossession({'player': 'LeftStriker'})
A1postcondition_poss_RW = HasBallPossession({'player': 'RightWinger'})
A1postcondition_poss_LW = HasBallPossession({'player': 'LeftWinger'})

# FIX: Replaced old A1target_2 with a full set of target locations for supporting each teammate.
# This makes the support movement dynamic based on who received the pass.
A1target_support_RS = DistanceTo({
    'from': 'Coach',
    'to': 'RightStriker',
    'min': {'avg': 4.0, 'std': 0.5},
    'max': {'avg': 9.0, 'std': 0.7},
    'operator': 'within'
})
A1target_support_LS = DistanceTo({
    'from': 'Coach',
    'to': 'LeftStriker',
    'min': {'avg': 4.0, 'std': 0.5},
    'max': {'avg': 9.0, 'std': 0.7},
    'operator': 'within'
})
A1target_support_RW = DistanceTo({
    'from': 'Coach',
    'to': 'RightWinger',
    'min': {'avg': 4.0, 'std': 0.5},
    'max': {'avg': 9.0, 'std': 0.7},
    'operator': 'within'
})
A1target_support_LW = DistanceTo({
    'from': 'Coach',
    'to': 'LeftWinger',
    'min': {'avg': 4.0, 'std': 0.5},
    'max': {'avg': 9.0, 'std': 0.7},
    'operator': 'within'
})


def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())


# FIX: Added lambdas to check for open paths to each teammate.
def λ_path_to_RightStriker(scene, sample):
    return A1precondition_path_RS.bool(simulation())


def λ_path_to_LeftStriker(scene, sample):
    return A1precondition_path_LS.bool(simulation())


def λ_path_to_RightWinger(scene, sample):
    return A1precondition_path_RW.bool(simulation())


def λ_path_to_LeftWinger(scene, sample):
    return A1precondition_path_LW.bool(simulation())


# FIX: Replaced λ_precondition_1 with a set of specific possession-checking lambdas.
def λ_poss_RightStriker(scene, sample):
    return A1postcondition_poss_RS.bool(simulation())


def λ_poss_LeftStriker(scene, sample):
    return A1postcondition_poss_LS.bool(simulation())


def λ_poss_RightWinger(scene, sample):
    return A1postcondition_poss_RW.bool(simulation())


def λ_poss_LeftWinger(scene, sample):
    return A1postcondition_poss_LW.bool(simulation())


# FIX: Replaced λ_target2 with a set of specific lambdas for finding a support position.
def λ_target_support_RS():
    return A1target_support_RS.dist(simulation(), ego=True)


def λ_target_support_LS():
    return A1target_support_LS.dist(simulation(), ego=True)


def λ_target_support_RW():
    return A1target_support_RW.dist(simulation(), ego=True)


def λ_target_support_LW():
    return A1target_support_LW.dist(simulation(), ego=True)
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