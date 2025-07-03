from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Wait for teammate to get possession of the ball")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Move to overlap position to draw defender and create space")
    do MoveTo(λ_target_0())
    do Speak("Wait to receive pass from teammate")
    do ReceiveBall()
    do Speak("Decide between dribbling forward or passing back to teammate")
    if λ_precondition_1(simulation(), None):
        do Speak("Dribble forward, defender is far away")
        do MoveTo(λ_target_1())
    else:
        do Speak("Pass back to teammate, defender is close")
        do Pass(teammate)

A1precondition_0 = HasBallPossession({'player': 'teammate'})
A1target_0 = Overlap({'coach': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'defender1', 'theta': {'avg': 34.2, 'std': 5.4}, 'dist': {'avg': 4.8, 'std': 0.6}})
A1precondition_1_far = DistanceTo({'to': 'defender1', 'from': 'Coach', 'operator': 'greater_than', 'min': 4.2})
A1target_1 = MoveTo({'destination': 'goal', 'min_dist': 3.5, 'max_dist': 10.0})  # Dribble forward, not a Scenic API, providing placeholder
A1precondition_1_close = DistanceTo({'to': 'defender1', 'from': 'Coach', 'operator': 'less_than', 'max': 4.2})

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_target_0():
    return A1target_0.dist(simulation(), ego=True)

def λ_precondition_1(scene, sample):
    # Return True if defender is far, False if defender is close
    return A1precondition_1_far.bool(simulation())

def λ_target_1():
    # Dribble forward towards goal when space allows
    return A1target_1.dist(simulation(), ego=True)

def λ_termination_MoveTo_overlap(scene, sample):
    # Terminate overlap move when at overlap position (but not tied to HasPath or overlap outcome)
    return CloseTo({'obj': 'Coach', 'ref': λ_target_0(), 'max': 0.5}).bool(simulation())

def λ_termination_MoveTo_goal(scene, sample):
    # Terminate dribble forward when inside max distance region
    return CloseTo({'obj': 'Coach', 'ref': λ_target_1(), 'max': 1.0}).bool(simulation())

def λ_termination_ReceiveBall(scene, sample):
    # Terminate ReceiveBall after a short duration or after possession changes (not outcome-tied)
    return not HasBallPossession({'player': 'Coach'}).bool(simulation())

def λ_termination_Idle(scene, sample):
    # Terminate Idle when teammate has possession (not tied to pass event)
    return A1precondition_0.bool(simulation())

def λ_termination_Pass(scene, sample):
    # Terminate Pass after action step or if Coach loses possession (not tied to successful receive)
    return not HasBallPossession({'player': 'Coach'}).bool(simulation())




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
      with behavior TeammateBehavior(), with name "teammate"

ball = new Ball ahead of teammate by 1

ego = new Coach at (ego_x_distance, ego_y_distance, 0), with name "Coach", with team "blue", with behavior CoachBehavior()

opponent = new Player at (0, Uniform(4, 6), 0), with name "defender1",
            with behavior DefenderBehavior()

goal = new Goal at (0, 10, 0)