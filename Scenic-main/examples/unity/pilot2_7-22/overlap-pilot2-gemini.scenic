from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

C_Overlap = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'opponent', 'theta': {'avg': 42.5, 'std': 7.5}, 'dist': {'avg': 4.25, 'std': 0.75}})
C_TeammatePasses = MakePass({'player': 'teammate'})
C_DribbleToGoal = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 8.5, 'std': 1.5}, 'operator': 'less_than'})
C_PastOpponent = HeightRelation({'obj': 'Coach', 'relation': 'above', 'ref': 'opponent', 'height_threshold': {'avg': 1.0, 'std': 0.5}})
C_HasPossession = HasBallPossession({'player': 'Coach'})
C_HasClearPath = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.5, 'std': 0.5}})

def λ_target0():
    return C_Overlap.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return C_TeammatePasses.bool(simulation())

def λ_target1():
    return C_DribbleToGoal.dist(simulation(), ego=True)

def λ_termination1(scene, sample):
    return C_PastOpponent.bool(simulation())

def λ_precondition_shoot(scene, sample):
    return C_HasPossession.bool(simulation()) and C_HasClearPath.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds

    do Speak("The defender is on my teammate. I'll make an overlap run to get open for a pass.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)

    do Speak("The pass is on its way. I'll stop to receive the ball and control it.")
    do StopAndReceiveBall()

    do Speak("Now with the ball, I will dribble forward past the opponent to get into a shooting position.")
    do MoveTo(λ_target1()) until λ_termination1(simulation(), None)

    do Speak("I'm past the defender and have a clear path. Time to take the shot!")
    do Idle() until λ_precondition_shoot(simulation(), None)
    do Shoot(goal)

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