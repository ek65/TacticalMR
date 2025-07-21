from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1_target_overlap = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'opponent', 'theta': {'avg': 40.0, 'std': 5.0}, 'dist': {'avg': 5.0, 'std': 1.0}})
A1_pre_receive = MakePass({'player': 'teammate'})
A1_pre_shoot = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 3.0, 'std': 0.5}})
A2_pre_shoot = HasBallPossession({'player': 'Coach'})

def λ_target_overlap():
	return A1_target_overlap.dist(simulation(), ego=True)

def λ_precondition_receive(scene, sample):
	return A1_pre_receive.bool(simulation())

def λ_precondition_shoot(scene, sample):
	return A1_pre_shoot.bool(simulation()) and A2_pre_shoot.bool(simulation())

behavior CoachBehavior():
	do Idle() for 3 seconds
	do Speak("Your teammate is blocked. I'm moving to overlap and create a passing lane.")
	do MoveTo(λ_target_overlap())
	do Speak("Okay, I'm in position. Waiting for your pass.")
	do Idle() until λ_precondition_receive(simulation(), None)
	do StopAndReceiveBall()
	do Speak("Got the ball. I will shoot when I have a clear path to the goal.")
	do Idle() until λ_precondition_shoot(simulation(), None)
	do Speak("The path is clear! I'm taking the shot!")
	do Shoot(goal)
	do Idle()




opponent_y_distance = Uniform(3, 5)
opponent_x_distance = Uniform(-2, 2)
ego_x_distance = Uniform(-2, 2)
ego_y_distance = Uniform(-1, -2)

# Ensure teammate and opponent are on the same side
#require (opponent_x_distance < 0 and ego_x_distance < 0) or (opponent_x_distance >= 0 and ego_x_distance >= 0)

behavior Follow(obj):
    while ego.position.y > 1:
        do MoveToBehavior(obj, distance = 2, status = f"Follow {obj.name}")

behavior TeammateBehavior():
    do Idle() for 1 seconds
    do MoveToBallAndGetPossession()
    do Idle() for 10 seconds
    do Idle() until ego.position.y > 2
    do Pass(ego, slow=False) until (distance from opponent to ego) <= 3
    do DribbleTo(goal) until (distance from opponent to ego) > 3
    do Idle() for 2 seconds
    

behavior DefenderBehavior():
    do Idle() for 1 seconds
    do Idle() until ego.position.y > 1
    while True:
        if distance from self to ego > 2.0:
            do MoveToBehavior(ego.position, distance=2.0)
        else:
            do Idle() for 0.1 seconds   
    

teammate = new Player at (0, 0, 0),
      with behavior TeammateBehavior(), with name "teammate", with team "blue"

ball = new Ball ahead of teammate by 1

ego = new Coach at (ego_x_distance, ego_y_distance, 0), with name "Coach", with team "blue", with behavior CoachBehavior()

opponent = new Player at (0, Uniform(4, 6), 0), with name "opponent",
            with behavior DefenderBehavior(), with team "red"

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)