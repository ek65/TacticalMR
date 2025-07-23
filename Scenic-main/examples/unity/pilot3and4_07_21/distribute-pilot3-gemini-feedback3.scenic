from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####
precondition_has_ball = HasBallPossession({'player': 'Coach'})

precondition_path_to_striker = HasPath({
    'obj1': 'Coach',
    'obj2': 'LeftStriker',
    'path_width': {'avg': 1.5, 'std': 0.25}
})

precondition_path_to_right_striker = HasPath({
    'obj1': 'Coach',
    'obj2': 'RightStriker',
    'path_width': {'avg': 1.5, 'std': 0.25}
})

precondition_path_to_left_winger = HasPath({
    'obj1': 'Coach',
    'obj2': 'LeftWinger',
    'path_width': {'avg': 1.5, 'std': 0.25}
})

target_move_upfield = HeightRelation({
    'obj': 'Coach',
    'relation': 'above',
    'ref': None,
    'height_threshold': {'avg': 8.0, 'std': 1.0}
})

# This target is specific to the RightWinger pass scenario (moving behind the coach's own marker)
target_move_to_space = DistanceTo({
    'from': 'Coach',
    'to': 'Defender1',
    'min': {'avg': 5.0, 'std': 1.0},
    'operator': 'greater_than'
})

# These targets ensure the coach moves away from the defender marking the pass receiver.
target_support_ls = DistanceTo({
    'from': 'Coach',
    'to': 'defender4',
    'min': {'avg': 5.0, 'std': 1.0},
    'operator': 'greater_than'
})

target_support_rs = DistanceTo({
    'from': 'Coach',
    'to': 'defender5',
    'min': {'avg': 5.0, 'std': 1.0},
    'operator': 'greater_than'
})

target_support_lw = DistanceTo({
    'from': 'Coach',
    'to': 'defender2',
    'min': {'avg': 5.0, 'std': 1.0},
    'operator': 'greater_than'
})

# CHANGE START: Added new constraints based on coach feedback.
# These ensure the coach moves closer to the teammate who received the pass,
# making the supporting run more effective as a follow-up passing option.
target_support_proximity_ls = DistanceTo({
    'from': 'Coach',
    'to': 'LeftStriker',
    'max': {'avg': 8.0, 'std': 1.5},
    'operator': 'less_than'
})

target_support_proximity_rs = DistanceTo({
    'from': 'Coach',
    'to': 'RightStriker',
    'max': {'avg': 8.0, 'std': 1.5},
    'operator': 'less_than'
})

target_support_proximity_lw = DistanceTo({
    'from': 'Coach',
    'to': 'LeftWinger',
    'max': {'avg': 8.0, 'std': 1.5},
    'operator': 'less_than'
})

target_support_proximity_rw = DistanceTo({
    'from': 'Coach',
    'to': 'RightWinger',
    'max': {'avg': 8.0, 'std': 1.5},
    'operator': 'less_than'
})
# CHANGE END

termination_striker_has_ball = HasBallPossession({'player': 'LeftStriker'})
termination_left_winger_has_ball = HasBallPossession({'player': 'LeftWinger'})
termination_right_winger_has_ball = HasBallPossession({'player': 'RightWinger'})


def λ_precondition_has_ball():
    return precondition_has_ball.bool(simulation())


def λ_precondition_path_to_striker():
    return precondition_path_to_striker.bool(simulation())


def λ_precondition_path_to_right_striker():
    return precondition_path_to_right_striker.bool(simulation())


def λ_precondition_path_to_left_winger():
    return precondition_path_to_left_winger.bool(simulation())


def λ_target_move_into_space():
    # CHANGE: Added proximity constraint to the RightWinger to refine the supporting run,
    # ensuring the coach stays close enough for a return pass, as per feedback.
    cond = (target_move_upfield
            and target_move_to_space
            and target_support_proximity_rw)
    return cond.dist(simulation(), ego=True)


def λ_target_support_ls():
    # CHANGE: Added proximity constraint to the LeftStriker to refine the supporting run,
    # ensuring the coach stays close enough for a return pass, as per feedback.
    cond = (target_move_upfield
            and target_support_ls
            and target_support_proximity_ls)
    return cond.dist(simulation(), ego=True)


def λ_target_support_rs():
    # CHANGE: Added proximity constraint to the RightStriker to refine the supporting run,
    # ensuring the coach stays close enough for a return pass, as per feedback.
    cond = (target_move_upfield
            and target_support_rs
            and target_support_proximity_rs)
    return cond.dist(simulation(), ego=True)


def λ_target_support_lw():
    # CHANGE: Added proximity constraint to the LeftWinger to refine the supporting run,
    # ensuring the coach stays close enough for a return pass, as per feedback.
    cond = (target_move_upfield
            and target_support_lw
            and target_support_proximity_lw)
    return cond.dist(simulation(), ego=True)


def λ_termination_striker_ball_received():
    return termination_striker_has_ball.bool(simulation())


def λ_termination_lw_ball_received():
    return termination_left_winger_has_ball.bool(simulation())


def λ_termination_rw_ball_received():
    return termination_right_winger_has_ball.bool(simulation())


behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I'm starting in midfield. I need to get the ball to initiate the play.")
    do MoveToBallAndGetPossession()
    do Speak("Now that I have possession, I will evaluate the defensive pressure to make my decision.")
    do Idle() until λ_precondition_has_ball()

    if λ_precondition_path_to_striker():
        do Speak("The defense is giving me space. I'll make an ambitious pass to the left striker.")
        do Pass(LeftStriker)
        do Speak("Now I'll run into the open space to provide a follow-up option.")
        # NOTE: The lambda for this MoveTo is now updated to keep the coach closer to the striker.
        do MoveTo(λ_target_support_ls())
    elif λ_precondition_path_to_right_striker():
        do Speak("The defense is giving me space on the right. I'll pass to the right striker.")
        do Pass(RightStriker)
        do Speak("Now I'll run into the open space to provide a follow-up option.")
        # NOTE: The lambda for this MoveTo is now updated to keep the coach closer to the striker.
        do MoveTo(λ_target_support_rs())
    elif λ_precondition_path_to_left_winger():
        do Speak("The path to the strikers is blocked. I'll play it safe to the left winger.")
        do Pass(LeftWinger)
        do Speak("After the pass, I'm making a run forward to create space and be ready for a return.")
        # NOTE: The lambda for this MoveTo is now updated to keep the coach closer to the winger.
        do MoveTo(λ_target_support_lw())
    else:
        do Speak("The left side is crowded. I'll pass to the right winger to exploit space there.")
        do Pass(RightWinger)
        do Speak("I'm moving into the space behind the defender to receive a quick one-two pass.")
        # NOTE: The lambda for this MoveTo is now updated to keep the coach closer to the winger.
        do MoveTo(λ_target_move_into_space())
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