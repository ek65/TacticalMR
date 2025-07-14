from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Move forward to draw out the defender.")
    do MoveTo(λ_target0())
    do Speak("Idle until the defender closes you down or you are close to the defender.")
    do Idle() until λ_precondition0(simulation(), None)
    do Speak("Get possession of the ball and be decisive.")
    do GetBallPossession(ball)
    do Speak("Idle until you have the ball and are pressured.")
    do Idle() until λ_precondition1(simulation(), None)
    do Speak("Receive the ball and look for a chance to shoot.")
    do ReceiveBall()
    do Speak("Idle until defender is pressuring you or path is blocked.")
    do Idle() until λ_termination0(simulation(), None)
    do Speak("Pass the ball back to teammate for a better chance.")
    do Pass(teammate)
    do Speak("Idle and observe the teammate attempt to score.")
    do Idle()

A1_target0 = DistanceTo({'from': 'Coach', 'to': 'defender', 'min': None, 'max': {'avg': 4.0, 'std': 0.5}, 'operator': 'less_than'})
A2_precondition0 = DistanceTo({'from': 'Coach', 'to': 'defender', 'min': None, 'max': {'avg': 2.5, 'std': 0.3}, 'operator': 'less_than'})
A3_precondition1 = HasBallPossession({'player': 'Coach'})
A4_termination0 = Pressure({'player1': 'defender', 'player2': 'Coach'})

def λ_target0():
    return A1_target0.dist(simulation(), ego=True)

def λ_precondition0(scene, sample):
    return A2_precondition0.bool(simulation())

def λ_precondition1(scene, sample):
    return A3_precondition1.bool(simulation())

def λ_termination0(scene, sample):
    return A4_termination0.bool(simulation())




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
            with behavior DefenderBehavior()

goal = new Goal at (0, 17, 0)