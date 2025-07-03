from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Wait for teammate to gain possession of the ball")
    do Idle() until λ_precondition_teammate_has_ball(simulation(), None)
    do Speak("Move ahead into overlapped space to attract defender and create an opening")
    do MoveTo(λ_target_overlap()) until λ_termination_overlap(simulation(), None)
    do Speak("Wait to receive the ball from teammate")
    do Idle() until λ_precondition_pass_to_coach(simulation(), None)
    do Speak("Move to ball and gain possession")
    do GetBallPossession(ball)
    if λ_precondition_defender_close(simulation(), None):
        do Speak("Defender is close. Quickly pass it back to teammate")
        do Pass(teammate)
    else:
        do Speak("Defender is far. Dribble forward towards the goal")
        do MoveTo(λ_target_goal()) until λ_termination_goal(simulation(), None)

# Constraints and lambda functions

A1precondition_teammate_has_ball = HasBallPossession({'player': 'teammate'})

A1target_overlap = HorizontalRelation({'obj': 'Coach', 'relation': 'right', 'ref': 'teammate', 'horizontal_threshold': {'avg': 3.5, 'std': 0.9}})
A2target_overlap = HeightRelation({'obj': 'Coach', 'relation': 'above', 'ref': 'teammate', 'height_threshold': {'avg': 2.4, 'std': 0.6}})
A3termination_overlap = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 3.9, 'std': 0.7}, 'max': None, 'operator': 'greater_than'})

A1precondition_pass_to_coach = MakePass({'player': 'teammate'})

A1precondition_defender_close = CloseTo({'obj': 'Coach', 'ref': 'defender1', 'max': {'avg': 3.0, 'std': 0.6}})

A1target_goal = HeightRelation({'obj': 'Coach', 'relation': 'above', 'ref': 'goal', 'height_threshold': {'avg': 0.5, 'std': 0.4}})
A2termination_goal = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.0, 'std': 0.7}, 'operator': 'less_than'})

def λ_precondition_teammate_has_ball(scene, sample):
    return A1precondition_teammate_has_ball.bool(simulation())

def λ_target_overlap():
    # Move both horizontally to the right and vertically above teammate
    # Move until sufficiently overlapped in both dimensions
    return A1target_overlap.dist(simulation(), ego=True) and A2target_overlap.dist(simulation(), ego=True)

def λ_termination_overlap(scene, sample):
    # Overlap distance achieved: Coach is ahead/right/above and farther than learned threshold from teammate
    return A3termination_overlap.bool(simulation())

def λ_precondition_pass_to_coach(scene, sample):
    return A1precondition_pass_to_coach.bool(simulation())

def λ_precondition_defender_close(scene, sample):
    return A1precondition_defender_close.bool(simulation())

def λ_target_goal():
    return A1target_goal.dist(simulation(), ego=True)

def λ_termination_goal(scene, sample):
    return A2termination_goal.bool(simulation())




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