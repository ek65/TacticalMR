from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_0 = DistanceTo({
    'from': 'Coach',
    'to': 'teammate',
    'min': {'avg': 4.0, 'std': 0.5},
    'max': {'avg': 8.0, 'std': 0.7},
    'operator': 'within'
})
A2target_0 = HorizontalRelation({
    'obj': 'Coach',
    'ref': 'teammate',
    'relation': 'left',
    'horizontal_threshold': {'avg': 3.5, 'std': 0.6}
})
A3target_0 = HasPath({
    'obj1': 'teammate',
    'obj2': 'Coach',
    'path_width': {'avg': 2.0, 'std': 0.3}
})

A1precondition_0 = HasBallPossession({'player': 'Coach'})
A2precondition_0 = MakePass({'player': 'teammate'})
Aprecondition_1 = HasBallPossession({'player': 'teammate'})
Aprecondition_2 = HasBallPossession({'player': 'Coach'})
Aprecondition_3 = HasBallPossession({'player': 'teammate'})

A1target_3 = HasPath({
    'obj1': 'Coach',
    'obj2': 'teammate',
    'path_width': {'avg': 2.0, 'std': 0.3}
})
A2target_3 = DistanceTo({
    'from': 'Coach',
    'to': 'teammate',
    'min': {'avg': 4.0, 'std': 0.5},
    'max': {'avg': 7.5, 'std': 0.5},
    'operator': 'within'
})

A1target_4 = DistanceTo({
    'from': 'teammate',
    'to': 'goal',
    'min': None,
    'max': {'avg': 8.0, 'std': 0.5},
    'operator': 'less_than'
})


def λ_target0():
    cond = A1target_0 & A2target_0 & A3target_0
    return cond.dist(simulation(), ego=True)


def λ_precondition_1():
    # Coach waits to get ball
    return A1precondition_0.bool(simulation())


def λ_precondition_2():
    # Teammate waits to have ball after receiving Coach's pass
    return Aprecondition_1.bool(simulation())


def λ_precondition_3():
    # Coach waits to have ball possession again
    return Aprecondition_2.bool(simulation())


def λ_precondition_4():
    # Teammate waits to have ball possession again
    return Aprecondition_3.bool(simulation())


def λ_target_passback():
    cond = A1target_3 & A2target_3
    return cond.dist(simulation(), ego=True)


def λ_target_goal():
    return A1target_4.dist(simulation(), ego=True)


# Replaced "no pressure" with "clear path to goal"
def λ_path_to_goal():
    # Returns True if there is an unobstructed 2m path from Coach to the goal
    return HasPath({
        'obj1': 'Coach',
        'obj2': 'goal',
        'path_width': {'avg': 2.0, 'std': 0.3}
    }).bool(simulation())


behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak(
        "Move 4 to 8 meters away left of teammate where teammate's passing lane is clear (2m width)."
    )
    do MoveTo(λ_target0(), True)

    # Changed: Check for clear path to goal instead of opponent pressure
    if λ_path_to_goal():
        do Speak("Path to goal is clear, shooting for goal.")
        do Shoot(goal)
        do Idle()  # End here if shot; follows structure that always ends with Idle.
    else:
        do Speak("Wait until I have ball possession after teammate passes to me.")
        do Idle() until λ_precondition_1()
        do Speak("Now, defender blocks path to goal. Look for teammate in open position, lane 2m wide.")
        do Pass(teammate)
        do Speak("Wait for teammate to get ball and move towards goal less than 8 meters away.")
        do Idle() until λ_precondition_2()
        do Speak("Teammate should move closer to goal (<8m), then pass back to me.")
        do Idle() until λ_precondition_3()
        do Speak("Receive pass and wait until I regain possession of the ball.")
        do StopAndReceiveBall()
        do Speak("Wait for teammate to regain ball to allow another pass.")
        do Idle() until λ_precondition_4()
        do Speak("Teammate advances and makes a final pass towards goal area around 17 meters.")
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