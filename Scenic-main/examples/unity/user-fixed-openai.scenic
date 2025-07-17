
from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I'm waiting to get the ball before I decide my next move.")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Okay, I have possession. I need to scan the field for the best passing option to move forward.")
    do GetBallPossession(ball, λ_termination=λ_termination_1())
    do Speak("I need to find an open teammate to pass to, prioritizing the strikers.")
    do Idle() until λ_precondition_2(simulation(), None)
    if λ_precondition_3(simulation(), None):
        do Speak("The left striker is open and in a great position. I'm passing the ball to him.")
        do Pass(LeftStriker, λ_termination=λ_termination_4())
    elif λ_precondition_5(simulation(), None):
        do Speak("The right striker is the best option. Passing the ball to him now.")
        do Pass(RightStriker, λ_termination=λ_termination_4())
    else:
        do Speak("Both strikers are covered. I will pass to the right winger to build up the play from the side.")
        do Pass(RightWinger, λ_termination=λ_termination_4())
    do Idle()

A1_precondition_0 = HasBallPossession(player='Coach')
A1_termination_1 = Pressure(player1='Defender1', player2='Coach')
A1_precondition_2 = HasPath(obj1='Coach', obj2='LeftStriker', path_width={'avg': 2.0, 'std': 0.0})
A2_precondition_2 = HasPath(obj1='Coach', obj2='RightStriker', path_width={'avg': 2.0, 'std': 0.0})
A3_precondition_2 = HasPath(obj1='Coach', obj2='RightWinger', path_width={'avg': 2.0, 'std': 0.0})
A1_precondition_3 = HasPath(obj1='Coach', obj2='LeftStriker', path_width={'avg': 2.0, 'std': 0.0})
A2_precondition_3 = HeightRelation(obj='LeftStriker', ref='RightStriker', relation='above', height_threshold={'avg': 0.5, 'std': 0.1})
A1_termination_4 = HasBallPossession(player='LeftStriker')
A2_termination_4 = HasBallPossession(player='RightStriker')
A3_termination_4 = HasBallPossession(player='RightWinger')
A1_precondition_5 = HasPath(obj1='Coach', obj2='RightStriker', path_width={'avg': 2.0, 'std': 0.0})

def λ_precondition_0(scene, sample):
    return not A1_precondition_0.bool(simulation())

def λ_termination_1(scene, sample):
    return A1_termination_1.bool(simulation())

def λ_precondition_2(scene, sample):
    return (A1_precondition_2.bool(simulation()) or A2_precondition_2.bool(simulation()) or A3_precondition_2.bool(simulation()))

def λ_precondition_3(scene, sample):
    return (A1_precondition_3.bool(simulation()) and A2_precondition_3.bool(simulation()))

def λ_termination_4(scene, sample):
    return (A1_termination_4.bool(simulation()) or A2_termination_4.bool(simulation()) or A3_termination_4.bool(simulation()))

def λ_precondition_5(scene, sample):
    return A1_precondition_5.bool(simulation())




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
defender2_x = LeftWinger.position.x + defender2_dist * sin(defender2_angle * pi / 180)
defender2_y = LeftWinger.position.y + defender2_dist * cos(defender2_angle * pi / 180)
defender2 = new Player at (defender2_x, defender2_y, 0), facing toward ego, with team "red", with name "Defender2"

defender3_angle = Uniform(-30, 30)
defender3_dist = Uniform(1,2)
defender3_x = RightWinger.position.x + defender3_dist * sin(defender3_angle * pi / 180)
defender3_y = RightWinger.position.y + defender3_dist * cos(defender3_angle * pi / 180)
defender3 = new Player at (defender3_x, defender3_y, 0), facing toward ego, with team "red", with name "Defender3"

defender4_angle = Uniform(-30, 30)
defender4_dist = Uniform(1,2)
defender4_x = LeftStriker.position.x + defender4_dist * sin(defender4_angle * pi / 180)
defender4_y = LeftStriker.position.y + defender4_dist * cos(defender4_angle * pi / 180)
defender4 = new Player at (defender4_x, defender4_y, 0), facing toward ego, with team "red", with name "Defender4"

defender5_angle = Uniform(-30, 30)
defender5_dist = Uniform(1,2)
defender5_x = RightStriker.position.x + defender5_dist * sin(defender5_angle * pi / 180)
defender5_y = RightStriker.position.y + defender5_dist * cos(defender5_angle * pi / 180)
defender5 = new Player at (defender5_x, defender5_y, 0), facing toward ego, with team "red", with name "Defender5"
