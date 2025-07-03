from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Wait and let the teammate receive ball before starting.")
    do Idle() until λ_precondition_teammate_has_ball(simulation(), None)
    do Speak("Now move forward to overlap and create space.")
    do MoveTo(λ_target_overlap())
    do Speak("Wait for the pass from your teammate.")
    do Idle() until λ_precondition_pass_from_teammate(simulation(), None)
    do Speak("Gain ball possession from pass.")
    do GetBallPossession(ball)
    do Speak("Pause to check how close the defender is.")
    do Idle() until λ_termination_pause1(simulation(), None)
    if λ_precondition_defender_far(simulation(), None):
        do Speak("Defender is far. Attack -- dribble towards the goal!")
        do MoveTo(λ_target_attack_goal())
    else:
        do Speak("Defender is close. Pass the ball back to safety.")
        do Pass(teammate)

A_overlap_dist = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 5.1, 'std': 0.65}, 'max': None, 'operator': 'greater_than'})
A_pass_from_teammate = MakePass({'player': 'teammate'})
A_coach_gets_ball = HasBallPossession({'player': 'Coach'})
A_defender_far = DistanceTo({'from': 'defender1', 'to': 'Coach', 'min': {'avg': 4.1, 'std': 0.36}, 'max': None, 'operator': 'greater_than'})
A_defender_close = DistanceTo({'from': 'defender1', 'to': 'Coach', 'min': None, 'max': {'avg': 3.6, 'std': 0.2}, 'operator': 'less_than'})
A_attack_goal = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': {'avg': 7.0, 'std': 0.5}, 'max': None, 'operator': 'greater_than'})

def λ_precondition_teammate_has_ball(scene, _):
    return HasBallPossession({'player': 'teammate'}).bool(simulation())

def λ_target_overlap():
    return A_overlap_dist.dist(simulation(), ego=True)

def λ_precondition_pass_from_teammate(scene, _):
    return A_pass_from_teammate.bool(simulation())

def λ_termination_pause1(scene, _):
    # Terminate after the coach gets ball possession or the defender distance changes
    return A_coach_gets_ball.bool(simulation()) or not (A_overlap_dist.bool(simulation()))

def λ_precondition_defender_far(scene, _):
    return A_defender_far.bool(simulation())

def λ_target_attack_goal():
    return A_attack_goal.dist(simulation(), ego=True)




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