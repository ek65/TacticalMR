from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Wait and get ready to receive the ball.")
    do Idle() until λ_precondition_receive(simulation(), None)
    do Speak("Get possession of the ball in midfield.")
    do GetBallPossession(ball)
    do Speak("Pause to assess passing options and scan for openings.")
    do Idle() until λ_termination_pause1(simulation(), None)
    do Speak("Pass to the best open forward teammate with a clear path.")
    do Pass(λ_target_pass1())
    do Speak("Pause and wait for teammate to receive the ball.")
    do Idle() until λ_precondition_teammate_received(simulation(), None)
    do Speak("Move forward to support the receiving teammate.")
    do MoveTo(λ_target_support())
    do Speak("Pause to provide a passing option in support.")
    do Idle() until λ_termination_pause2(simulation(), None)

# Constraint and precondition definitions

# Ball possession event when Coach is about to get/pass the ball
A_receive_possession = HasBallPossession({'player': 'Coach'})

# Pauses correspond to evaluation moments, intermediate, not success of pass itself
def λ_precondition_receive(scene, sample):
    # End waiting when Coach has ball possession
    return A_receive_possession.bool(simulation())

# Termination for pauses (not goal or pass success, but intermediate event changes)
def λ_termination_pause1(scene, sample):
    # E.g., RightStriker, LeftStriker, LeftWinger, or RightWinger is not yet in possession
    # (simulate interruption for scan/evaluation step after receiving the ball)
    # Here, simply ends after one Idle step for illustration purposes; typically watching for game state changes
    return True

# Best passing target determined by clear paths, usually to forward-most open player
A_clear_path_RS = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 2.77, 'std': 0.31}})
A_clear_path_LW = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger', 'path_width': {'avg': 2.55, 'std': 0.34}})
A_clear_path_LS = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 2.85, 'std': 0.30}})
A_clear_path_RW = HasPath({'obj1': 'Coach', 'obj2': 'RightWinger', 'path_width': {'avg': 2.10, 'std': 0.29}})

def λ_target_pass1():
    # Priority: right/left striker only if not blocked, then left winger, then right winger
    # Uses constraints from all demos: adapt priority per blocking explanations in transcripts
    if A_clear_path_RS.bool(simulation()):
        return RightStriker
    elif A_clear_path_LS.bool(simulation()):
        return LeftStriker
    elif A_clear_path_LW.bool(simulation()):
        return LeftWinger
    else:
        return RightWinger

# Wait for the target teammate to get ball after pass
A_teammate_received_RS = HasBallPossession({'player': 'RightStriker'})
A_teammate_received_LS = HasBallPossession({'player': 'LeftStriker'})
A_teammate_received_LW = HasBallPossession({'player': 'LeftWinger'})
A_teammate_received_RW = HasBallPossession({'player': 'RightWinger'})

def λ_precondition_teammate_received(scene, sample):
    # Did the intended recipient of the pass get the ball?
    return (A_teammate_received_RS.bool(simulation()) or
            A_teammate_received_LS.bool(simulation()) or
            A_teammate_received_LW.bool(simulation()) or
            A_teammate_received_RW.bool(simulation()))

# Move to support—towards the receiving teammate, into available space near them
A_support_RS = DistanceTo({'to': 'Coach', 'from': 'RightStriker', 'operator': 'less_than', 'max': {'avg': 5.8, 'std': 0.3}})
A_support_LS = DistanceTo({'to': 'Coach', 'from': 'LeftStriker', 'operator': 'less_than', 'max': {'avg': 5.8, 'std': 0.3}})
A_support_LW = DistanceTo({'to': 'Coach', 'from': 'LeftWinger', 'operator': 'less_than', 'max': {'avg': 5.9, 'std': 0.3}})
A_support_RW = DistanceTo({'to': 'Coach', 'from': 'RightWinger', 'operator': 'less_than', 'max': {'avg': 5.9, 'std': 0.3}})

def λ_target_support():
    # Move near the newly possessed teammate to support
    if A_teammate_received_RS.bool(simulation()):
        return A_support_RS.dist(simulation(), ego=True)
    elif A_teammate_received_LS.bool(simulation()):
        return A_support_LS.dist(simulation(), ego=True)
    elif A_teammate_received_LW.bool(simulation()):
        return A_support_LW.dist(simulation(), ego=True)
    else:
        return A_support_RW.dist(simulation(), ego=True)

# Final pause is just for demonstration clarity
def λ_termination_pause2(scene, sample):
    return True



# Ego (center midfielder) at origin
pi = 3.1415
ego = new Coach at (0, 0, 0), facing toward (0, 0, 0), with team "blue", with behavior CoachBehavior()

# Wingers
left_winger_angle = 90 + Uniform(0, 10)  # degrees from y-axis, 90 is positive x-axis (left), variance +/-10
right_winger_angle = -90 + Uniform(0, 10)  # degrees from y-axis, -90 is negative x-axis (right), variance +/-10
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
left_striker = new Player at (left_striker_x, left_striker_y, 0), facing toward ego, with name "LeftStriker", with team "blue"

right_striker_x = striker_dist * sin(right_striker_angle * pi / 180)
right_striker_y = striker_dist * cos(right_striker_angle * pi / 180)
right_striker = new Player at (right_striker_x, right_striker_y, 0), facing toward ego, with name "RightStriker", with team "blue"

# Ball at ego's feet
ball = new Ball at (0, 1, 0)

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
defender2_x = left_winger.position.x + defender2_dist * sin(defender2_angle * pi / 180)
defender2_y = left_winger.position.y + defender2_dist * cos(defender2_angle * pi / 180)
defender2 = new Player at (defender2_x, defender2_y, 0), facing toward ego, with team "red", with name "Defender2"

defender3_angle = Uniform(-30, 30)
defender3_dist = Uniform(1,2)
defender3_x = right_winger.position.x + defender3_dist * sin(defender3_angle * pi / 180)
defender3_y = right_winger.position.y + defender3_dist * cos(defender3_angle * pi / 180)
defender3 = new Player at (defender3_x, defender3_y, 0), facing toward ego, with team "red", with name "Defender3"

defender4_angle = Uniform(-30, 30)
defender4_dist = Uniform(1,2)
defender4_x = left_striker.position.x + defender4_dist * sin(defender4_angle * pi / 180)
defender4_y = left_striker.position.y + defender4_dist * cos(defender4_angle * pi / 180)
defender4 = new Player at (defender4_x, defender4_y, 0), facing toward ego, with team "red", with name "Defender4"

defender5_angle = Uniform(-30, 30)
defender5_dist = Uniform(1,2)
defender5_x = right_striker.position.x + defender5_dist * sin(defender5_angle * pi / 180)
defender5_y = right_striker.position.y + defender5_dist * cos(defender5_angle * pi / 180)
defender5 = new Player at (defender5_x, defender5_y, 0), facing toward ego, with team "red", with name "Defender5"

terminate after 10 seconds