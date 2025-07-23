from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1_precondition_pass_LS = HasPath({'obj1': 'Coach', 'obj2': 'LeftStriker', 'path_width': {'avg': 3.0, 'std': 0.5}})
A1_precondition_pass_LW = HasPath({'obj1': 'Coach', 'obj2': 'LeftWinger', 'path_width': {'avg': 3.0, 'std': 0.5}})
A1_precondition_pass_RS = HasPath({'obj1': 'Coach', 'obj2': 'RightStriker', 'path_width': {'avg': 3.0, 'std': 0.5}})
A1_target_LS = AtAngle({'player': 'Coach', 'ball': 'ball', 'left': {'theta': {'avg': 45, 'std': 5}, 'dist': {'avg': 3.5, 'std': 0.5}}})
A1_target_LW = AtAngle({'player': 'Coach', 'ball': 'ball', 'right': {'theta': {'avg': 30, 'std': 5}, 'dist': {'avg': 6, 'std': 1}}})
A1_target_RS = AtAngle({'player': 'Coach', 'ball': 'ball', 'right': {'theta': {'avg': 60, 'std': 5}, 'dist': {'avg': 4, 'std': 1}}})

def λ_target_LS():
	return A1_target_LS.dist(simulation(), ego=True)

def λ_target_LW():
	return A1_target_LW.dist(simulation(), ego=True)

def λ_target_RS():
	return A1_target_RS.dist(simulation(), ego=True)

def λ_precondition_pass_LS(scene, sample):
	return A1_precondition_pass_LS.bool(simulation())

def λ_precondition_pass_LW(scene, sample):
	return A1_precondition_pass_LW.bool(simulation())

def λ_precondition_pass_possible(scene, sample):
	return A1_precondition_pass_LS.bool(simulation()) or A1_precondition_pass_LW.bool(simulation()) or A1_precondition_pass_RS.bool(simulation())

behavior CoachBehavior():
	do Idle() for 3 seconds
	do Speak("I'll wait until there's a clear passing lane to a teammate before starting the play.")
	do Idle() until λ_precondition_pass_possible(simulation(), None)
	if λ_precondition_pass_LS(simulation(), None):
		do Speak("The Left Striker is open. I will pass the ball to them now.")
		do Pass(LeftStriker)
		do Speak("Now, I'll move to create a good return passing angle for the Left Striker.")
		do MoveTo(λ_target_LS())
		do Speak("I'm in a good spot. I will stop and wait for a possible return pass.")
		do StopAndReceiveBall()
	elif λ_precondition_pass_LW(simulation(), None):
		do Speak("The Left Winger has space. I am passing to them.")
		do Pass(LeftWinger)
		do Speak("I will move into the gap between defenders to provide support.")
		do MoveTo(λ_target_LW())
		do Speak("Okay, I'm now an option for the Left Winger. I will wait.")
		do StopAndReceiveBall()
	else:
		do Speak("The Right Striker is open on the other side. I'm passing the ball.")
		do Pass(RightStriker)
		do Speak("I'll move up and right to give the Right Striker a passing option back.")
		do MoveTo(λ_target_RS())
		do Speak("I am now available for a pass. I'll stop here and wait.")
		do StopAndReceiveBall()
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