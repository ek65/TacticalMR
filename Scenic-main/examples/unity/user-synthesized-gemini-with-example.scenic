from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A_move_away = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 5.0, 'std': 1.0}, 'operator': 'greater_than'})
A_receive_path = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 2.0, 'std': 0.5}})
A_is_pressured = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A_has_shot_path = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.5}})

def λ_target0():
	cond = A_move_away and A_receive_path
	return cond.dist(simulation(), ego=True)

def λ_precondition_shoot(scene, sample):
	return (not A_is_pressured.bool(simulation())) and A_has_shot_path.bool(simulation())

behavior CoachBehavior():
	do Idle() for 3 seconds
	do Speak("I'll move away from the defender to create space and a clear passing lane for my teammate.")
	do MoveTo(λ_target0())
	do Speak("Now that I'm in a good position, I'll stop and wait for the pass.")
	do StopAndReceiveBall()
	do Speak("I have the ball. Now I'll check if I should shoot or pass back.")
	do Idle() until HasBallPossession({'player': 'Coach'}).bool(simulation())
	if λ_precondition_shoot(simulation(), None):
		do Speak("The defender is far enough away and I have a clear shot. I'm going for the goal!")
		do Shoot(goal)
	else:
		do Speak("The defender is too close. The safest option is to pass it back to my teammate.")
		do Pass(teammate)
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
    do MoveToBallAndGetPossession()
    print("got ball")
    do Idle() for 10.0 seconds
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