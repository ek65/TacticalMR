from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

C1_Overlap = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'opponent', 'theta': {'avg': 45.0, 'std': 5.0}, 'dist': {'avg': 9.25, 'std': 0.75}})
C2_TeammatePass = MakePass({'player': 'teammate'})
C3_CoachPossession = HasBallPossession({'player': 'Coach'})
C4_PressureOnCoach = Pressure({'player1': 'opponent', 'player2': 'Coach'})
C5_PathToGoal = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.5}})
C6_DistanceToGoal = DistanceTo({'from': 'Coach', 'to': 'goal', 'operator': 'less_than', 'max': {'avg': 6.0, 'std': 1.0}, 'min': None})

def λ_target0():
	return C1_Overlap.dist(simulation(), ego=True)

def λ_precondition_receive():
	return C2_TeammatePass.bool(simulation())

def λ_precondition_decision():
	return C3_CoachPossession.bool(simulation())

def λ_if_condition():
	return C4_PressureOnCoach.bool(simulation())

def λ_else_target_move():
	return C6_DistanceToGoal.dist(simulation(), ego=True)

def λ_else_precondition_shoot():
	return C5_PathToGoal.bool(simulation()) and C3_CoachPossession.bool(simulation())

behavior CoachBehavior():
	do Idle() for 3 seconds
	do Speak("I'll move to the side to draw the defender and create an opening for my teammate.")
	do MoveTo(λ_target0())
	do Speak("Okay, the teammate passed the ball. I'll stop and receive it.")
	do Idle() until λ_precondition_receive()
	do StopAndReceiveBall()
	do Speak("Now I have the ball. Let's see what the defender does.")
	do Idle() until λ_precondition_decision()
	if λ_if_condition():
		do Speak("The defender is on me, I'll pass it back to my open teammate who now has a clear shot.")
		do Pass('teammate')
	else:
		do Speak("The defender is out of position. I'll move towards the goal for a shot.")
		do MoveTo(λ_else_target_move())
		do Idle() until λ_else_precondition_shoot()
		do Speak("I have a clear line to the goal, I'm taking the shot!")
		do Shoot(goal)
	do Idle()
####Environment Behavior START####


# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do MoveToBallAndGetPossession()
    print("got ball")
    do Idle() for 10.0 seconds
    do Pass(ego)
    do Idle()
####Environment Behavior START####
behavior OpponentFollowCoach():
    do Idle() for 1.0 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    while True:
        if distance from self to ego > 2.0:
            do MoveToBehavior(ego.position, distance=2.0)
        else:
            do Idle() for 0.1 seconds

# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "blue", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Coach ahead of teammate by coach_start_dist, with name "Coach", with team "blue", with behavior CoachBehavior()

# Place opponent ahead of coach (further from goal than coach)
opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)