from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1_target_approachDef = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 6.0, 'std': 0.3}, 'max': None, 'operator': 'greater_than'})
A2_target_approachDef = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 2.0, 'std': 0.2}})
A_target_approachDef = A1_target_approachDef and A2_target_approachDef

A_precond_pressure = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A_precond_pathToTeammate = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 2.0, 'std': 0.2}})
A_precond_eitherPressureOrPath = A_precond_pressure or A_precond_pathToTeammate

A_precond_getPossession = HasBallPossession({'player': 'Coach'})

A_target_sideSpace = DistanceTo({'from': 'Coach', 'to': 'ball', 'min': {'avg': 3.0, 'std': 0.2}, 'max': {'avg': 12.0, 'std': 1.0}, 'operator': 'within'})
A2_target_sideSpace = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 2.0, 'std': 0.2}})
A_target_sideSpaceFull = A_target_sideSpace and A2_target_sideSpace

A_precond_teammateArrives = MakePass({'player': 'teammate'})

A_precond_possAndNotShutDown = HasBallPossession({'player': 'Coach'}) and ~(Pressure({'player1': 'opponent', 'player2': 'Coach'}))

A_goal_shotRange = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 6.0, 'std': 0.3}, 'operator': 'less_than'})
A_goal_path = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.4, 'std': 0.35}})
A_shot_condition = A_goal_shotRange and A_goal_path

A_precond_teamMate_shutDown = Pressure({'player1': 'opponent', 'player2': 'Coach'})

A_precond_pass_back = HasBallPossession({'player': 'Coach'}) and Pressure({'player1': 'opponent', 'player2': 'Coach'})

A_pass_clear = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 2.0, 'std': 0.2}})

A_precond_teammateReady = HasBallPossession({'player': 'teammate'})


def λ_target_approachDef():
    return A_target_approachDef.dist(simulation(), ego=True)

def λ_target_sideSpace():
    return A_target_sideSpaceFull.dist(simulation(), ego=True)

def λ_shotRange():
    return A_shot_condition.dist(simulation(), ego=True)

def λ_precondition_pressure(scene, sample):
    return A_precond_pressure.bool(simulation())

def λ_precondition_pathToTeammate(scene, sample):
    return A_precond_pathToTeammate.bool(simulation())

def λ_precondition_eitherPressureOrPath(scene, sample):
    return A_precond_eitherPressureOrPath.bool(simulation())

def λ_precondition_getPossession(scene, sample):
    return A_precond_getPossession.bool(simulation())

def λ_precondition_teammateArrives(scene, sample):
    return A_precond_teammateArrives.bool(simulation())

def λ_precondition_possAndNotShutDown(scene, sample):
    return A_precond_possAndNotShutDown.bool(simulation())

def λ_precondition_shot(scene, sample):
    return A_shot_condition.bool(simulation())

def λ_precondition_passBack(scene, sample):
    return A_precond_pass_back.bool(simulation())

def λ_precondition_teammateReady(scene, sample):
    return A_precond_teammateReady.bool(simulation())


behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Approach the defender with the ball to force pressure")
    do MoveTo(λ_target_approachDef())
    do Speak("Wait until the opponent pressures or clear pass to teammate")
    do Idle() until λ_precondition_eitherPressureOrPath(simulation(), None)
    do Speak("Move to ball and get possession when ball is passed")
    do MoveToBallAndGetPossession()
    do Speak("Wait until you receive the ball or you are shut down")
    do Idle() until λ_precondition_getPossession(simulation(), None)
    if λ_precondition_shot(simulation(), None) and λ_precondition_possAndNotShutDown(simulation(), None):
        do Speak("Take the open opportunity and shoot at goal")
        do Shoot(goal)
        do Idle()
    else:
        do Speak("Pass the ball back to teammate for a better chance")
        do Pass(teammate)
        do Speak("Wait until teammate gets possession again")
        do Idle() until λ_precondition_teammateReady(simulation(), None)
        do Speak("Coach idle after successful action")
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