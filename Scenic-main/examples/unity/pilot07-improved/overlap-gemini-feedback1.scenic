from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_0 = HasPath({
    'obj1': 'teammate',
    'obj2': 'Coach',
    'path_width': {'avg': 2.0, 'std': 0.5}
})

A2target_0 = DistanceTo({
    'from': 'Coach',
    'to': 'teammate',
    'min': {'avg': 8.5, 'std': 1.0},
    'max': None,
    'operator': 'greater_than'
})

# NEW: Only allow moving UPFIELD (y > teammate) for open positions, not downward.
A3target_0 = HeightRelation({
    'obj': 'Coach',
    'relation': 'above',
    'ref': 'teammate',
    'height_threshold': {'avg': 1.0, 'std': 0.2}
})

precondition_wait = HasBallPossession({'player': 'Coach'})
precondition_shoot = HasPath({
    'obj1': 'Coach',
    'obj2': 'goal',
    'path_width': {'avg': 2.0, 'std': 0.5}
})
termination_pass = HasBallPossession({'player': 'Coach'})


def λ_target0():
    # Changed: add upfield constraint (A3target_0), so coach only moves above teammate, not downward.
    cond = A1target_0 and A2target_0 and A3target_0
    return cond.dist(simulation(), ego=True)


def λ_precondition_wait():
    return precondition_wait.bool(simulation())


def λ_precondition_shoot():
    return precondition_shoot.bool(simulation())


def λ_termination_pass():
    return not termination_pass.bool(simulation())


behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("My teammate is blocked, so I will move about 8 meters away to create a clear passing lane.")
    # Now MoveTo will only select open lateral/upfield positions (not downward)
    do MoveTo(λ_target0(), True)
    do Speak("Now, I will wait until I receive the ball from my teammate.")
    do Idle() until λ_precondition_wait()
    do Speak("Now that I have the ball, I will check if I have a clear shot at the goal.")
    if λ_precondition_shoot():
        do Speak("I have a clear shot at the goal, so I will take it.")
        do Shoot(goal)
    else:
        do Speak("The opponent is blocking my path to the goal, so I will pass the ball back to my teammate.")
        do Pass(teammate)
    do Idle()

####Environment Behavior START####

opponent_y_distance = Range(3, 5)
opponent_x_distance = Range(-2, 2)
ego_x_distance = Range(-2, 2)
ego_y_distance = Range(-1, -2)

# Ensure teammate and opponent are on the same side
#require (opponent_x_distance < 0 and ego_x_distance < 0) or (opponent_x_distance >= 0 and ego_x_distance >= 0)

behavior Follow(obj):
    while ego.position.y > 1:
        do MoveToBehavior(obj, distance = 2, status = f"Follow {obj.name}")

behavior TeammateBehavior():
    # Double checking gotBall to ensure the pass is triggered correctly
    # since MoveToBallAndGetPossession() might get interrupted
    do SetPlayerSpeed(6.0)
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
        
        # Calculate target position: height between coach and goal, opposite X side
        ego_x = ego.position.x
        ego_y = ego.position.y
        goal_y = goal.position.y
        
        # Go to opposite side (negative if ego is positive, positive if ego is negative)
        target_x = -ego_x if ego_x > 0 else abs(ego_x)
        target_y = (ego_y + goal_y) / 2  # Height between coach and goal


        
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
        variation = Range(-1, 1)  # Random variation in both directions


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

ego = new Coach at (0, ego_y_distance, 0),
    with name "Coach",
    with team "blue",
    with behavior CoachBehavior(),
    with xMark Vector(0, 0, 0),  # Set initial xMark position
    with triggerPass False  # Initialize triggerPass to False

opponent = new Player at (0, Range(4, 6), 0), with name "opponent",
            with behavior DefenderBehavior(), with team "red"

goal = new Goal at (0, 17, 0)

line = new Line at (0, 10, 0)

terminate when (ego.gameObject.stopButton)