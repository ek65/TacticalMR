from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("I will make an overlapping run to create space and attract the defender.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("The teammate is passing. I will receive the ball.")
    do ReceiveBall()
    if λ_precondition1(simulation(), None):
        do Speak("I have enough space, so I will dribble towards the goal.")
        # CHANGE: Added a termination condition to stop the coach's movement when they get close to the goal.
        # This addresses the feedback that the coach should pass when near the goal.
        do MoveTo(goal) until λ_termination1(simulation(), None)
        # ADD: Added a narration for the new pass action.
        do Speak("I am near the goal, so I will pass to my teammate.")
        # ADD: Added the Pass action to implement the coach's feedback.
        do Pass(teammate)
    else:
        do Speak("The defender is too close, so I will pass the ball back to my teammate.")
        do Pass(teammate)

A1target_0 = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'defender1',
    'theta': {
        'avg': 37.49500473945939,
        'std': 4.316867385966453
    },
    'dist': {
        'avg': 4.144414138072041,
        'std': 1.1398863177626926
    }
})

A1termination_0 = MakePass({'player': 'teammate'})

# ADD: Defined a new constraint to determine if the coach is "near the goal".
A1termination_1 = DistanceTo({
    'to': 'goal',
    'from': 'Coach',
    'operator': 'less_than',
    'max': 5
})

A1precondition_1 = DistanceTo({
    'to': 'defender1',
    'from': 'Coach',
    'operator': 'greater_than',
    'min': {
        'avg': 4.238624128913989,
        'std': 0.613329061405903
    }
})

A2precondition_1 = HasBallPossession({'player': 'Coach'})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

# ADD: Added a new lambda function for the termination condition of the MoveTo(goal) action.
def λ_termination1(scene, sample):
    return A1termination_1.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation()) and A2precondition_1.bool(simulation())

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
    print("ego at good position")
    do Idle() for 1 seconds
    do Pass(ego, slow=False) until (distance from opponent to ego) <= 3
    print("pass happened")
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

goal = new Goal at (0, 17, 0)