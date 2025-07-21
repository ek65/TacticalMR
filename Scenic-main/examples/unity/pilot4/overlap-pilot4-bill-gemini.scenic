from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I'll overlap to get open for a pass and create space for my teammate.")
    do MoveTo(λ_target0())
    do Speak("Now that I'm in a good position, I'll wait for the pass.")
    do Idle() until λ_precondition1()
    do StopAndReceiveBall()
    do Speak("I've received the ball. Now I'll wait for a clear path to the goal.")
    do Idle() until λ_precondition2()
    do Speak("I have a clear path, so I am taking the shot now!")
    do Shoot(goal)
    do Idle()

Overlap_0 = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 60.0, 'std': 5.0},
    'dist': {'avg': 4.0, 'std': 0.5}
})
Precond_Receive_1 = MakePass({'player': 'teammate'})
Precond_Shoot_2_Possession = HasBallPossession({'player': 'Coach'})
Precond_Shoot_2_Path = HasPath({
    'obj1': 'Coach',
    'obj2': 'goal',
    'path_width': {'avg': 3.0, 'std': 0.5}
})
Term_MoveTo_0 = MakePass({'player': 'teammate'})
Term_Receive_1 = MovingTowards({'obj': 'opponent', 'ref': 'Coach'})
Term_Shoot_2 = HeightRelation({
    'obj': 'ball',
    'relation': 'above',
    'ref': 'Coach',
    'height_threshold': {'avg': 1.0, 'std': 0.1}
})

def λ_target0():
    return Overlap_0.dist(simulation(), ego=True)

def λ_termination0():
    return Term_MoveTo_0.bool(simulation())

def λ_precondition1():
    return Precond_Receive_1.bool(simulation())

def λ_termination1():
    return Term_Receive_1.bool(simulation())

def λ_precondition2():
    return Precond_Shoot_2_Possession.bool(simulation()) and Precond_Shoot_2_Path.bool(simulation())

def λ_termination2():
    return Term_Shoot_2.bool(simulation())




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
    do MoveToBallAndGetPossession(ball)
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