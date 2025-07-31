from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1_target_get_open_path = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 2.0, 'std': 0.5}})
A2_target_get_open_dist = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.0, 'std': 0.5}, 'operator': 'greater_than'})
A1_precondition_receive_pass = MakePass({'player': 'teammate'})
A1_precondition_is_pressured = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1_target_get_open_again_height = HeightRelation({'obj': 'Coach', 'relation': 'above', 'ref': 'teammate', 'height_threshold': {'avg': 5.5, 'std': 0.5}})
A2_target_get_open_again_path = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 2.0, 'std': 0.5}})
A1_precondition_shoot = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 3.0, 'std': 0.5}})

def λ_target_get_open():
    return (A1_target_get_open_path and A2_target_get_open_dist).dist(simulation(), ego=True)

def λ_precondition_receive_pass():
    return A1_precondition_receive_pass.bool(simulation())

def λ_precondition_is_pressured():
    return A1_precondition_is_pressured.bool(simulation())

def λ_target_get_open_again():
    return (A1_target_get_open_again_height and A2_target_get_open_again_path).dist(simulation(), ego=True)

def λ_precondition_shoot():
    return A1_precondition_shoot.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I will move to an open space with a clear 2 meter wide passing lane, away from the opponent.")
    do MoveTo(λ_target_get_open(), False)
    do Speak("I'm in position, waiting for my teammate to make the pass.")
    do Idle() until λ_precondition_receive_pass()
    do Speak("The pass is on its way. I'll move to the ball and get possession.")
    do MoveToBallAndGetPossession()
    do Speak("Now that I have the ball, I'll check if the opponent is pressuring me.")
    do Idle() until True
    if λ_precondition_is_pressured():
        do Speak("The opponent is pressuring me. I'll pass it right back to my teammate.")
        do Pass(teammate)
        do Speak("Now I'll run about 6 meters ahead of my teammate to find a new open passing lane.")
        do MoveTo(λ_target_get_open_again(), False)
        do Speak("I'm in a new open space, waiting for the return pass.")
        do Idle() until λ_precondition_receive_pass()
        do Speak("Here comes the return pass. I'll get possession.")
        do MoveToBallAndGetPossession()
        do Speak("I'll wait for a clear shot at the goal with a path width of at least 3 meters.")
        do Idle() until λ_precondition_shoot()
        do Speak("I have a clear shot. Time to score!")
        do Shoot(goal)
    else:
        do Speak("The opponent is not pressuring me. I'll check for a clear shot on goal.")
        do Idle() until λ_precondition_shoot()
        do Speak("The path to goal is clear. I'll take the shot myself!")
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