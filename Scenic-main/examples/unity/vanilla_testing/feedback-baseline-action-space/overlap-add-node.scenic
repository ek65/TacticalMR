from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("I will make an overlapping run to create space and attract the defender.")
    # MODIFIED: The run now terminates if the teammate passes OR if they start dribbling to the goal.
    # This addresses the feedback that the teammate might not pass and may dribble to the goal instead.
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None) or λ_termination1(simulation(), None)

    # ADDED: A conditional branch to handle the two possible outcomes from the previous state.
    if λ_termination0(simulation(), None):
        # This is the original path: the teammate passed the ball.
        do Speak("The teammate is passing. I will receive the ball.")
        do ReceiveBall()
        if λ_precondition1(simulation(), None):
            do Speak("I have enough space, so I will dribble towards the goal.")
            do MoveTo(goal)
        else:
            do Speak("The defender is too close, so I will pass the ball back to my teammate.")
            do Pass(teammate)
    else:
        # This is the new path based on feedback: the teammate is dribbling.
        # The coach moves to a supporting position to create a passing angle.
        do Speak("Teammate is dribbling to the goal. I will create an angle for a pass.")
        do MoveTo(λ_target1())

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

# ADDED: New target definition for the supporting run, as suggested by the coach's feedback.
# It uses AtAngle to find a good position to receive a future pass from the dribbling teammate.
A1target_1 = AtAngle({
    'player': 'Coach',
    'ball': 'ball',
    'left': {
        'theta': {'avg': 45, 'std': 10},
        'dist': {'avg': 5, 'std': 1}
    },
    'right': {
        'theta': {'avg': 45, 'std': 10},
        'dist': {'avg': 5, 'std': 1}
    }
})

A1termination_0 = MakePass({'player': 'teammate'})

# ADDED: New constraint to detect when the teammate moves towards the goal.
# This provides an alternative termination condition for the coach's initial run.
A1termination_1 = MovingTowards({'obj': 'teammate', 'ref': 'goal'})

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

# ADDED: Lambda function for the new move target.
def λ_target1():
    return A1target_1.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

# ADDED: Lambda function for the new termination condition.
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