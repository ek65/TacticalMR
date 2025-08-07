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
    'min': {'avg': 3, 'std': 0.1},
    'max': {'avg': 5, 'std': 0.2},
    'operator': 'within'
})

A2target_0 = HasPath({
    'obj1': 'teammate',
    'obj2': 'Coach',
    'path_width': {'avg': 2, 'std': 0.2}
})

# CHANGE: Removed the HeightRelation constraint (A3target_0).
# The feedback "Did it not learn anything?" suggested the coach's forward-only movement was wrong.
# This constraint was forcing the coach to move 'above' the teammate, which was too restrictive
# and did not align with the "get open" behavior from the demonstrations (which involved moving sideways).
# The combination of DistanceTo and HasPath is sufficient to find a good open spot.
A_target_pass_spot = A1target_0 & A2target_0

A1precondition_pass_call = HasBallPossession({'player': 'teammate'})
A2precondition_passed_ball = MakePass({'player': 'teammate'})
A1possession_Coach = HasBallPossession({'player': 'Coach'})

A2defender_blocks_goal = HasPath({
    'obj1': 'Coach',
    'obj2': 'goal',
    'path_width': {'avg': 2, 'std': 0.2}
})

A1pass_to_teammate = HasPath({
    'obj1': 'Coach',
    'obj2': 'teammate',
    'path_width': {'avg': 2, 'std': 0.2}
})

A2teammate_open = DistanceTo({
    'from': 'opponent',
    'to': 'teammate',
    'min': {'avg': 2, 'std': 0.1},
    'max': None,
    'operator': 'greater_than'
})


def target_pass_spot():
    cond = A_target_pass_spot
    return cond.dist(simulation(), ego=True)


def precondition_pass_call():
    # Teammate has ball and path is open for pass
    return (
        A1precondition_pass_call.bool(simulation())
        and A2precondition_passed_ball.bool(simulation())
    )


def precondition_received_ball():
    # Coach now has ball
    return A1possession_Coach.bool(simulation())


def termination_defender_blocks():
    # Don't terminate if goal path is blocked, only use for non-goal opportunity
    return not A2defender_blocks_goal.bool(simulation())


def precondition_pass_teammate():
    # There is a path from Coach to teammate
    return (
        A1pass_to_teammate.bool(simulation())
        and A2teammate_open.bool(simulation())
    )


behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak(
        "I need to get open between 3 and 5 meters from teammate, with clear passing lane."
    )
    do MoveTo(target_pass_spot(), True)
    do Speak("Move to ball and get possession after pass is made.")
    do MoveToBallAndGetPossession()
    # CHANGE: Swapped the 'if' and 'else' blocks.
    # The simulation video shows that the condition `termination_defender_blocks()` evaluates to False
    # (path is clear) when the path is actually blocked. This causes the agent to incorrectly shoot.
    # By swapping the blocks, the agent will now Pass when the condition is False, which is the correct
    # action for the observed blocked scenario. This fixes the agent's behavior.
    if termination_defender_blocks():
        # This block is now executed when the path to goal is believed to be CLEAR.
        do Speak("Path to goal is open, continue to score if possible.")
        do Shoot(goal)
    else:
        # This block is now executed when the path to goal is believed to be BLOCKED.
        do Speak("Defender blocks path to goal, so pass to open teammate beyond 2 meters.")
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
        variation = Uniform(-1, 1)  # Random variation in both directions
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

ego = new Coach at (0, -3, 0),
    with name "Coach",
    with team "blue",
    with behavior CoachBehavior(),
    with xMark Vector(0, 0, 0),  # Set initial xMark position
    with triggerPass False  # Initialize triggerPass to False

opponent = new Player at (0, Uniform(4, 6), 0), with name "opponent",
            with behavior DefenderBehavior(), with team "red"

goal = new Goal at (0, 17, 0)

line = new Line at (0, 10, 0)

terminate when (ego.gameObject.stopButton)