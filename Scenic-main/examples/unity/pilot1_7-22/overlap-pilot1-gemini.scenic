from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

C1_overlap = Overlap({'player': 'coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'defender1', 'theta': {'avg': 90.0, 'std': 30.0}, 'dist': {'avg': 7.0, 'std': 1.5}})
C2_teammate_passed = MakePass({'player': 'teammate'})
C3_coach_has_ball = HasBallPossession({'player': 'coach'})
C4_path_to_goal = HasPath({'obj1': 'coach', 'obj2': 'goal', 'path_width': {'avg': 2.5, 'std': 0.5}})

def λ_target0():
    return C1_overlap.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return C2_teammate_passed.bool(simulation())

def λ_precondition_has_ball(scene, sample):
    return C3_coach_has_ball.bool(simulation())

def λ_condition_can_shoot(scene, sample):
    return C4_path_to_goal.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds

    do Speak("I need to overlap my teammate to create a passing lane and get open for a pass.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)

    do Speak("My teammate has passed the ball, so I will stop and receive it.")
    do StopAndReceiveBall()

    do Speak("Now that I have the ball, I'll check if I can shoot or if I should pass back.")
    do Idle() until λ_precondition_has_ball(simulation(), None)

    if λ_condition_can_shoot(simulation(), None):
        do Speak("The path to the goal is clear. I'm taking the shot!")
        do Shoot(goal)
    else:
        do Speak("The defender is blocking me. I'll pass back to my open teammate.")
        do Pass(teammate)

    do Idle()
####Environment Behavior START####



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