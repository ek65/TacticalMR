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
A1_precondition_shoot = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 1, 'std': 0.5}})

A_target_overlap = DistanceTo({
    'from': 'Coach', 
    'to': 'teammate', 
    'min': {'avg': 2.0, 'std': 0.5}, 
    'max': {'avg': 4.0, 'std': 0.5}, 
    'operator': 'within'
})

A_overlap_constraint = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 35.0, 'std': 5.0},
    'dist': {'avg': 5.0, 'std': 2.0}
})

def λ_target_get_open():
    return (A1_target_get_open_path and A2_target_get_open_dist).dist(simulation(), ego=True)

def λ_target_overlap():
    return A_overlap_constraint.dist(simulation(), ego=True)

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
    do Speak("I will overlap my teammate to create space and receive the ball")
    do MoveTo(λ_target_overlap(), True)
    do Idle() for 2 seconds
    
    # Decide whether to shoot or pass back based on clear path to goal
    if λ_precondition_shoot():
        do Speak("I have a clear path to goal, I'll shoot")
        do Shoot(goal)
    else:
        do Speak("No clear path to goal, I'll pass back to my teammate")
        do Pass(teammate)
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
        
        # After passing to coach, go to opposite side at same height as ego
        do Idle() for 1 seconds
        
        # Calculate target position: same Y as ego, opposite X side
        ego_x = ego.position.x
        ego_y = ego.position.y
        
        # Go to opposite side (negative if ego is positive, positive if ego is negative)
        target_x = -ego_x if ego_x > 0 else abs(ego_x)
        target_y = ego_y  # Same height as ego
        
        target_position = Vector(target_x, target_y, 0)
        do MoveToBehavior(target_position, distance=0.5)
        
        # Wait to receive ball back from coach
        do Idle() until self.gameObject.ballPossession
        
        # If received ball back, score a goal
        if self.gameObject.ballPossession:
            do Shoot(goal)
    
    do Idle()
    
### Modified opponent behavior: Keep position until ego receives ball, then move to middle of line with variation
behavior DefenderBehavior():
    do Idle() for 1 seconds
    do Idle() until ego.position.y > 1
    
    # Keep position until ego receives the ball
    while not ego.gameObject.ballPossession:
        do Idle() for 0.1 seconds
    
    # Once ego receives ball, move to middle of line between ego and goal
    if ego.gameObject.ballPossession:
        # Calculate middle point between ego and goal
        goal_x = goal.position.x
        goal_y = goal.position.y
        ego_x = ego.position.x
        ego_y = ego.position.y
        
        middle_x = (ego_x + goal_x) / 2
        middle_y = (ego_y + goal_y) / 2
        
        # Add some variation to create opportunities or blocking
        variation = Uniform(-2, 2)  # Random variation in both directions
        target_x = middle_x + variation
        target_y = middle_y + variation
        
        # Move to the target position
        target_position = Vector(target_x, target_y, 0)
        do MoveToBehavior(target_position, distance=.1)
        
        # Face the ego (coach) once in position
        do LookAt(ego)
        
    

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