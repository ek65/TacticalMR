from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_0 = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 4.0, 'std': 0.5}, 'max': {'avg': 8.0, 'std': 0.5}, 'operator': 'within'})
A2target_0 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 2.0, 'std': 0.1}})
# New: Ensure Coach is "above" (higher y) teammate by at least 0.5m (threshold allows numerical fuzz)
A3target_0 = HeightRelation({'obj': 'Coach', 'relation': 'above', 'ref': 'teammate', 'height_threshold': {'avg': 0.5, 'std': 0.2}})

A1precondition_0 = HasBallPossession({'player': 'Coach'})
A2precondition_0 = MakePass({'player': 'teammate'})
A1target_1 = DistanceTo({'from': 'goal', 'to': 'Coach', 'min': None, 'max': {'avg': 9.5, 'std': 1.0}, 'operator': 'less_than'})
A2target_1 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 1.5, 'std': 0.3}})
A1target_2 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 1.5, 'std': 0.3}})
A2precondition_2 = HasBallPossession({'player': 'teammate'})
A1precondition_3 = MakePass({'player': 'Coach'})
A_is_pressured = Pressure({'player1': 'opponent', 'player2': 'Coach'})

def λ_target0():
    # Fix: Only consider positions above teammate (Coach's y > teammate's y + 0.5m)
    cond = A1target_0 & A2target_0 & A3target_0
    return cond.dist(simulation(), ego=True)

def λ_precondition_0():
    cond = A1precondition_0 & A2precondition_0
    return cond.bool(simulation())

def λ_target1():
    cond = A1target_1 & A2target_1
    return cond.dist(simulation(), ego=True)

def λ_target2():
    return A1target_2.dist(simulation(), ego=True)

def λ_precondition_2():
    return A2precondition_2.bool(simulation())

def λ_precondition_3():
    return A1precondition_3.bool(simulation())

def λ_termination():
    # Terminate if Coach loses possession unexpectedly (not success condition)
    return not A1precondition_0.bool(simulation())

def λ_not_pressured():
    # True if coach is not being pressured by opponent
    return not A_is_pressured.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    # Fix: updated narration to specify "above" the teammate.
    do Speak("I want to get open. Move to open space 4-8 meters left or right of teammate, but always above them (further upfield), where they have a clear passing path of at least 2 meters wide.")
    do MoveTo(λ_target0(), True)
    do Speak("Wait for possession of the ball after teammate passes.")
    do Idle() until λ_precondition_0()
    do Speak("I now have the ball. I will check if I have a clear path to the goal and attempt to shoot if I do.")

    if max([v for row in λ_target1() for v in row]) > 0.7:  # flatten array; check open shot
        do Speak("Path to goal is clear. Taking the shot.")
        do Shoot(goal)
        do Idle()
    else:
        do Speak("No clear shot to goal. Look for pass.")
        do Pass(teammate)
        do Speak("Wait for teammate to regain possession before next instruction.")
        do Idle() until λ_precondition_2()
        do Speak("Once teammate has the ball, I will wait for a pass from them again with a clear path.")
        do Idle() until λ_precondition_3()
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
        do MoveToBehavior(target_position)
        
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
        do MoveToBehavior(target_position)
        
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