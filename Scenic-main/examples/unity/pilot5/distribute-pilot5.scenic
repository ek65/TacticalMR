from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

precond_has_path_to_rs = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 3.5, 'std': 0.5}})
precond_has_path_to_lw = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger', 'path_width': {'avg': 3.5, 'std': 0.5}})
precond_rs_has_ball = HasBallPossession({'player': 'RightStriker'})
precond_lw_has_ball = HasBallPossession({'player': 'LeftWinger'})
target_overlap_run = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'Defender1', 'theta': {'avg': 40.0, 'std': 2.0}, 'dist': {'avg': 5.0, 'std': 1.0}})
term_rs_makes_pass = MakePass({'player': 'RightStriker'})

def λ_precondition_0(scene, sample):
    return precond_has_path_to_rs.bool(simulation())

def λ_precondition_1(scene, sample):
    return precond_has_path_to_lw.bool(simulation())
    
def λ_precondition_2(scene, sample):
    return precond_rs_has_ball.bool(simulation())
    
def λ_precondition_3(scene, sample):
    return precond_lw_has_ball.bool(simulation())

def λ_target0():
    return target_overlap_run.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return term_rs_makes_pass.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("First, I need to get the ball to initiate the play.")
    do MoveToBallAndGetPossession()
    do Speak("Now, is there a clear path to pass to the Right Striker?")
    if λ_precondition_0(simulation(), None):
        do Speak("Yes, the Right Striker is open. I will pass the ball now.")
        do Pass(RightStriker)
        do Speak("I will wait until the Right Striker receives my pass.")
        do Idle() until λ_precondition_2(simulation(), None)
        do Speak("I will make a supporting run to overlap, at an angle of 40 degrees and distance of 5 meters.")
        do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    elif λ_precondition_1(simulation(), None):
        do Speak("The Right Striker is not open. I will pass to the Left Winger instead.")
        do Pass(LeftWinger)
        do Speak("I will wait for the Left Winger to receive the ball.")
        do Idle() until λ_precondition_3(simulation(), None)
        do Speak("Now I will hold my position to provide a safe pass-back option.")
        do Idle() for 5 seconds
    else:
        do Speak("No clear passing options are available, so I will hold my position.")
        do Idle() for 5 seconds
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