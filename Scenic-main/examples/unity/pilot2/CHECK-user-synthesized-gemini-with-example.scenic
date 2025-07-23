from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

from scenic.simulators.unity.rl_agent import *
from scenic.simulators.unity.soccer.soccer_utils import *

A1target_0 = AtAngle({'player': 'Coach', 'ball': 'ball', 'left': {'theta': {'avg': 45.0, 'std': 10.0}, 'dist': {'avg': 7.0, 'std': 1.0}}, 'right': {'theta': {'avg': 45.0, 'std': 10.0}, 'dist': {'avg': 7.0, 'std': 1.0}}})
A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = HasBallPossession({'player': 'Coach'})
A1precondition_shoot = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.5}})
A2precondition_shoot = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 2.5, 'std': 0.5}, 'operator': 'greater_than'})
A1termination_0 = MakePass({'player': 'teammate'})
A1termination_1 = HasBallPossession({'player': 'Coach'})
A1termination_2 = HasBallPossession({'player': 'Coach'})
A1termination_3 = HasBallPossession({'player': 'Coach'})

def λ_target0():
	return A1target_0.dist(simulation(), ego=True)

def λ_precondition0(scene, sample):
	return A1precondition_0.bool(simulation())

def λ_precondition1(scene, sample):
	return A1precondition_1.bool(simulation())

def λ_should_shoot(scene, sample):
	return A1precondition_shoot.bool(simulation()) and A2precondition_shoot.bool(simulation())

def λ_termination0(scene, sample):
	return A1termination_0.bool(simulation())

def λ_termination1(scene, sample):
	return A1termination_1.bool(simulation())

def λ_termination2(scene, sample):
	return not A1termination_2.bool(simulation())

def λ_termination3(scene, sample):
	return not A1termination_3.bool(simulation())

behavior CoachBehavior():
	do Idle() for 3 seconds
	do Speak("The defender is close, so I will move away to create a better angle for a pass.")
	do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
	do Speak("My teammate is about to pass. I will stop and wait for the ball.")
	do StopAndReceiveBall() until λ_termination1(simulation(), None)
	do Idle() until λ_precondition1(simulation(), None)
	if λ_should_shoot(simulation(), None):
		do Speak("There's a clear path to the goal, so I will take the shot.")
		do Shoot(goal) until λ_termination2(simulation(), None)
	else:
		do Speak("The defender is blocking my path, so I will pass the ball back to my teammate.")
		do Pass(teammate) until λ_termination3(simulation(), None)
	do Idle()



# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do MoveToBallAndGetPossession(ball)
    print("got ball")
    do Idle() for 5.0 seconds
    do Pass(ego)
    do Idle()

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