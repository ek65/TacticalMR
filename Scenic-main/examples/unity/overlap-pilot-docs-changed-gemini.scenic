from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("In this 2v1, I need to make an overlapping run to give my teammate a passing option.")
    do MoveTo(λ_target=λ_target0()) until λ_termination0(simulation(), None)
    do Speak("Now that I am open, I will wait for my teammate to pass the ball.")
    do Idle() until λ_precondition0(simulation(), None)
    do Speak("My teammate is passing. I will get possession of the ball.")
    do GetBallPossession(target=ball)
    do Speak("Now that I have the ball, I will check if the defender is closing me down.")
    do Idle() until λ_precondition_branch(simulation(), None)
    if λ_is_pressured(simulation(), None):
        do Speak("The defender is on me, so I will pass the ball back to my teammate.")
        do Pass(target=teammate)
    else:
        do Speak("I have an open shot on goal, so I will take it.")
        do Shoot(target=goal)
    do Idle()

A1target_0 = Overlap(player='Coach', ball='ball', goal='goal', opponent='defender1', theta={'avg': 55.67, 'std': 9.28}, dist={'avg': 6.18, 'std': 0.58})
A1termination_0 = DistanceTo(to='Coach', from='teammate', operator='greater_than', min={'avg': 5.0, 'std': 1.0})
A1precondition_0 = MakePass(player='teammate')
A1precondition_1 = Pressure(player1='defender1', player2='coach')
A2precondition_1 = CloseTo(obj='coach', ref='defender1', max={'avg': 3.0, 'std': 0.5})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_is_pressured(scene, sample):
    return A1precondition_1.bool(simulation()) and A2precondition_1.bool(simulation())

def λ_precondition_branch(scene, sample):
    return λ_is_pressured(scene, sample) or not λ_is_pressured(scene, sample)







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
    do GetBallPossession(ball)
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