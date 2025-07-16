from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Let's draw the defender by approaching with the ball.")
    do MoveTo(λ_target0())
    do Speak("Waiting until the defender pressures or a pass angle opens.")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Get possession of the ball to threaten the defense.")
    do GetBallPossession(ball)
    do Speak("Waiting for the defender to close me, creating space for my teammate.")
    do Idle() until λ_precondition_1(simulation(), None)
    do Speak("Move into space to the side for a pass opportunity.")
    do MoveTo(λ_target1())
    do Speak("Waiting for the perfect moment to receive the ball.")
    do Idle() until λ_precondition_2(simulation(), None)
    do Speak("Receive the ball and look for an option.")
    do ReceiveBall()
    do Speak("Wait until I'm blocked. Will play back to teammate.")
    do Idle() until λ_precondition_3(simulation(), None)
    do Speak("Make a safe pass back to teammate.")
    do Pass(teammate)
    do Speak("Wait for teammate to be in position for a shot.")
    do Idle() until λ_precondition_4(simulation(), None)
    do Speak("Teammate should shoot for goal after my pass.")
    do Shoot(goal)
    do Idle()

# Constraint Instances

Demo0_MoveTo_draw_defender = DistanceTo({'from': 'Coach', 'to': 'defender', 'min': None, 'max': {'avg': 5.8, 'std': 0.3}, 'operator': 'less_than'})
Demo0_Pressure_on_Coach = Pressure({'player1': 'defender', 'player2': 'Coach'})
Demo0_MoveTo_space = AtAngle({'player': 'Coach', 'ball': 'ball', 'right': {'theta': {'avg': 43.0, 'std': 6.0}, 'dist': {'avg': 8.2, 'std': 0.88}}})
Demo0_ReceiveBall = MakePass({'player': 'teammate'})
Demo0_Close_blocked = CloseTo({'obj': 'defender', 'ref': 'Coach', 'max': {'avg': 1.7, 'std': 0.2}})
Demo0_PassBack = HasBallPossession({'player': 'Coach'})
Demo0_Teammate_ready = HasBallPossession({'player': 'teammate'})
Demo0_Coach_shot_angle = HasPath({'obj1': 'teammate', 'obj2': 'goal', 'path_width': {'avg': 2.6, 'std': 0.25}})

# Lambda/Helper Functions

def λ_target0():
    return Demo0_MoveTo_draw_defender.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    return Demo0_Pressure_on_Coach.bool(simulation())

def λ_precondition_1(scene, sample):
    return Demo0_Pressure_on_Coach.bool(simulation())

def λ_target1():
    return Demo0_MoveTo_space.dist(simulation(), ego=True)

def λ_precondition_2(scene, sample):
    return Demo0_ReceiveBall.bool(simulation())

def λ_precondition_3(scene, sample):
    return Demo0_Close_blocked.bool(simulation())

def λ_precondition_4(scene, sample):
    return (Demo0_Teammate_ready.bool(simulation()) and Demo0_Coach_shot_angle.bool(simulation()))







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