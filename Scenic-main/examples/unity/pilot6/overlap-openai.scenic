from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_behind = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 3.5, 'std': 0.2}, 'max': {'avg': 5.5, 'std': 0.2}, 'operator': 'within'})
A1precondition_receive1 = MakePass({'player': 'teammate'})
A1haspossession_Coach = HasBallPossession({'player': 'Coach'})
A1target_passback = DistanceTo({'from': 'teammate', 'to': 'Coach', 'min': {'avg': 2.5, 'std': 0.2}, 'max': {'avg': 4.5, 'std': 0.3}, 'operator': 'within'})
A1precondition_receive2 = MakePass({'player': 'Coach'})
A1target_infront_space = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 3.0, 'std': 0.3}, 'max': {'avg': 7.0, 'std': 0.3}, 'operator': 'within'})
A1precondition_receive3 = MakePass({'player': 'teammate'})
A1haspossession2_Coach = HasBallPossession({'player': 'Coach'})
A1target_turn_shoot = DistanceTo({'from': 'goal', 'to': 'Coach', 'min': None, 'max': {'avg': 14.0, 'std': 0.2}, 'operator': 'less_than'})

def λ_target_behind():
    return A1target_behind.dist(simulation(), ego=True)

def λ_precondition_receive1():
    return A1precondition_receive1.bool(simulation())

def λ_haspossession_Coach():
    return A1haspossession_Coach.bool(simulation())

def λ_target_passback():
    return A1target_passback.dist(simulation(), ego=True)

def λ_precondition_receive2():
    return A1precondition_receive2.bool(simulation())

def λ_target_infront_space():
    return A1target_infront_space.dist(simulation(), ego=True)

def λ_precondition_receive3():
    return A1precondition_receive3.bool(simulation())

def λ_haspossession2_Coach():
    return A1haspossession2_Coach.bool(simulation())

def λ_target_turn_shoot():
    return A1target_turn_shoot.dist(simulation(), ego=True)

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Move to open space 4 meters behind teammate to receive ball")
    do MoveTo(λ_target_behind(), False)
    do Speak("Wait to receive a pass from teammate before moving to ball")
    do Idle() until λ_precondition_receive1()
    do Speak("Move to ball and get possession when pass is made")
    do MoveToBallAndGetPossession()
    do Speak("Wait until you have ball possession before passing")
    do Idle() until λ_haspossession_Coach()
    do Speak("Pass ball back to teammate at distance 4 meters")
    do Pass(teammate)
    do Speak("Wait to receive a return pass from teammate")
    do Idle() until λ_precondition_receive2()
    do Speak("Move to ball and get possession from teammate's return pass")
    do MoveToBallAndGetPossession()
    do Speak("Move to open space 5 meters in front of teammate after receiving ball")
    do MoveTo(λ_target_infront_space(), False)
    do Speak("Wait to receive the ball in advanced position")
    do Idle() until λ_precondition_receive3()
    do Speak("Move to ball and get possession for final attack")
    do MoveToBallAndGetPossession()
    do Speak("Turn to goal and shoot when less than 14 meters from goal")
    do Idle() until λ_haspossession2_Coach()
    do Speak("Shoot at the goal now")
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
    # Double checking gotBall to ensure the pass is triggered correctly
    # since MoveToBallAndGetPossession() might get interrupted
    gotBall = False
    try:
        do Idle() for 1 seconds
        do MoveToBallAndGetPossession()
        gotBall = True
        do Idle()
    interrupt when ego.triggerPass and self.gameObject.ballPossession and gotBall:
        ego.triggerPass = False
        do Idle() for 1 seconds
        do Pass(ego.xMark)
        do Idle() for 1 seconds
        if self.gameObject.ballPossession:
            do Idle() until (distance from opponent to ego) <= 3
            do DribbleTo(goal) until (distance from opponent to ego) > 3
    
    do Idle()
    

behavior DefenderBehavior():
    do Idle() for 1 seconds
    do Idle() until ego.position.y > 1
    while True:
        if distance from self to ego > 3.5:
            do MoveToBehavior(ego.position, distance=3.5)
        else:
            do Idle() for 0.1 seconds   
    

teammate = new Player at (0, 0, 0),
      with behavior TeammateBehavior(), with name "teammate", with team "blue"

ball = new Ball ahead of teammate by 1

ego = new Coach at (ego_x_distance, ego_y_distance, 0),
    with name "Coach",
    with team "blue",
    with behavior CoachBehavior(),
    with xMark Vector(0, 0, 0),  # Set initial xMark position
    with triggerPass False  # Initialize triggerPass to False

opponent = new Player at (0, Uniform(4, 6), 0), with name "opponent",
            with behavior DefenderBehavior(), with team "red"

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)