from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("We're on a 2v1 attack. I'll make an overlapping run to give my teammate a passing option.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("Teammate sees me, making the pass. I'll receive it and assess.")
    do StopAndReceiveBall()
    do Speak("I have possession. Decide now: pass to my teammate or shoot?")
    do Idle() until λ_precondition1(simulation(), None) or C4.bool(simulation())
    if λ_precondition1(simulation(), None):
        do Speak("The defender has shut me down. My teammate is in a better spot, so I am passing it back.")
        do Pass(target=teammate)
    else:
        do Speak("The defender did not cover me! I have an open lane to the goal, I am taking the shot!")
        do Shoot(goal)
    do Idle()

C1 = Overlap(player='coach', ball='ball', goal='goal', opponent='defender1', theta={'avg': 36.56, 'std': 2.18}, dist={'avg': 6.41, 'std': 1.13})
C2 = MakePass(player='teammate')
C3 = Pressure(player1='defender1', player2='coach')
C4 = HasPath(obj1='coach', obj2='goal', path_width={'avg': 3.0, 'std': 0.5})

def λ_target0():
    return C1.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return C2.bool(simulation())

def λ_precondition1(scene, sample):
    return C3.bool(simulation()) and not C4.bool(simulation())




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
    do MoveToBallAndGetPossession(ball)
    do Idle() until ego.position.y > 2
    do Pass(ego, slow=False) until (distance from opponent to ego) <= 3
    do DribbleTo(goal) until (distance from opponent to ego) > 3
    do Idle() for 2 seconds
    

behavior DefenderBehavior():
    do Idle() for 1 seconds
    do Idle() until ego.position.y > 1
    do Follow(ego) until ego.gameObject.ballPossession
    

teammate = new Player at (0, 0, 0),
      with behavior TeammateBehavior(), with name "teammate", with team "blue"

ball = new Ball ahead of teammate by 1

ego = new Coach at (ego_x_distance, ego_y_distance, 0), with name "Coach", with team "blue", with behavior CoachBehavior()

opponent = new Player at (0, Uniform(4, 6), 0), with name "opponent",
            with behavior DefenderBehavior(), with team "red"

goal = new Goal at (0, 17, 0)