from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1_overlap = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 28.0, 'std': 4.0},
    'dist': {'avg': 4.9, 'std': 0.7}
})

A2_overlap = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 29.0, 'std': 4.0},
    'dist': {'avg': 4.2, 'std': 0.6}
})

A3_overlap = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 27.0, 'std': 4.0},
    'dist': {'avg': 4.5, 'std': 0.5}
})

A_has_possession = HasBallPossession({'player': 'Coach'})
A_teammate_has_possession = HasBallPossession({'player': 'teammate'})
A_makepass_teammate = MakePass({'player': 'teammate'})
A_opponent_pressure_coach = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A_path_to_goal = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.6, 'std': 0.3}})
A_opponent_pressure_range = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': None, 'max': {'avg': 2.5, 'std': 0.3}, 'operator': 'less_than'})

def λ_target_overlap_right():
    return A1_overlap.dist(simulation(), ego=True)

def λ_target_overlap_left():
    return A2_overlap.dist(simulation(), ego=True)

def λ_target_overlap_additional():
    return A3_overlap.dist(simulation(), ego=True)

def λ_precondition_wait_for_pass(scene, sample):
    return A_makepass_teammate.bool(simulation())

def λ_precondition_coach_has_ball(scene, sample):
    return A_has_possession.bool(simulation())

def λ_precondition_opponent_pressuring(scene, sample):
    return A_opponent_pressure_coach.bool(simulation())

def λ_precondition_path_to_goal(scene, sample):
    # This is an intermediate signal: path is unblocked, not a shot taken yet
    return A_path_to_goal.bool(simulation())

def λ_precondition_opponent_close(scene, sample):
    return A_opponent_pressure_range.bool(simulation())

def λ_precondition_teammate_has_possession(scene, sample):
    return A_teammate_has_possession.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("teammate has the ball, overlap to create space on right")
    do MoveTo(λ_target_overlap_right(), False)
    do Speak("wait for teammate to pass you the ball")
    do Idle() until λ_precondition_wait_for_pass(simulation(), None)
    do Speak("move to ball and get possession for attack")
    do MoveToBallAndGetPossession()
    do Speak("wait until you get possession of the ball")
    do Idle() until λ_precondition_coach_has_ball(simulation(), None)
    if not λ_precondition_opponent_close(simulation(), None):
        do Speak("the defender is not close, look for shot at goal")
        do Idle() until λ_precondition_path_to_goal(simulation(), None)
        do Speak("shoot the ball towards the goal")
        do Shoot(goal)
    else:
        do Speak("the defender is close, pass back to your teammate")
        do Pass(teammate)
    do Idle()

####Environment Behavior START####


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
    try:
        do Idle() for 1 seconds
        do MoveToBallAndGetPossession()
        do Idle()
    interrupt when ego.triggerPass and self.gameObject.ballPossession:
        ego.triggerPass = False
        do Idle() until ego.position.y > 2
        do Pass(ego.xMark)
        do Idle() until (distance from opponent to ego) <= 3
        do DribbleTo(goal) until (distance from opponent to ego) > 3
    
    do Idle()
    

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