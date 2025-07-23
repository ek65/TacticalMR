from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Prepare for the pass, get in position.")
    # Wait for Coach to have ball possession (after teammate passes and Coach receives)
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Move to receive the ball and get possession.")
    do MoveToBallAndGetPossession()
    do Speak("Analyze the defenders, look for open passing lane.")
    do Idle() until λ_precondition_1(simulation(), None)
    do Speak("Pass to left striker, he is open on the left.")
    do Pass('LeftStriker')
    do Speak("Show to make yourself available for a pass back.")
    do Idle() until λ_precondition_2(simulation(), None)
    do Speak("Move into position to receive return pass from LeftStriker.")
    do MoveTo(λ_target2())
    do Speak("Stop and become a good passing option for LeftStriker.")
    do StopAndReceiveBall()
    do Idle()

# Constraints for behaviors

# Precondition 0: Coach can move only after teammate passed and Coach receives the ball (Coach gets possession)
A1precondition_0 = HasBallPossession({'player': 'Coach'})

# Precondition 1: Coach should have clear passing path to LeftStriker (no defender between Coach and LeftStriker)
A1precondition_1 = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 2.5, 'std': 0.2}})

# Precondition 2: Wait until LeftStriker receives the ball, then move to open angle/space
A1precondition_2 = HasBallPossession({'player': 'LeftStriker'})

# After the pass, create an angle to receive a pass from LeftStriker, away from Defender1 and Defender4.
A1target_2 = AtAngle({
    'player': 'Coach',
    'ball': 'ball',
    'left': {
        'theta': {'avg': 50.0, 'std': 5.0},
        'dist': {'avg': 5.0, 'std': 0.3}
    },
    'right': {
        'theta': {'avg': 50.0, 'std': 5.0},
        'dist': {'avg': 5.0, 'std': 0.3}
    }
})

# Lambda functions

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition_2(scene, sample):
    return A1precondition_2.bool(simulation())

def λ_target2():
    return A1target_2.dist(simulation(), ego=True)




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