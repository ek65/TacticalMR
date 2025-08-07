from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_move_left = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 40, 'std': 7},
    'dist': {'avg': 8, 'std': 1}
})

# Added: Right side overlap, same angle/distance but for the other side
A1target_move_right = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 40, 'std': 7},
    'dist': {'avg': 8, 'std': 1}
})

A1precondition_teammate_has_ball = HasBallPossession({'player': 'teammate'})
A1precondition_teammate_passed = MakePass({'player': 'teammate'})
A1precondition_coach_has_ball = HasBallPossession({'player': 'Coach'})
A1precondition_coach_passed = MakePass({'player': 'Coach'})
A1precondition_teammate_ready = MovingTowards({'obj': 'teammate', 'ref': 'goal'})
A1target_forward = DistanceTo({
    'from': 'Coach',
    'to': 'goal',
    'min': None,
    'max': {'avg': 12, 'std': 1},
    'operator': 'less_than'
})

def λ_target_move_left():
    return A1target_move_left.dist(simulation(), ego=True)

# Added for right-side
def λ_target_move_right():
    return A1target_move_right.dist(simulation(), ego=True)

def λ_precondition_teammate_has_ball():
    return A1precondition_teammate_has_ball.bool(simulation())

def λ_precondition_teammate_passed():
    return A1precondition_teammate_passed.bool(simulation())

def λ_precondition_coach_has_ball():
    return A1precondition_coach_has_ball.bool(simulation())

def λ_precondition_coach_passed():
    return A1precondition_coach_passed.bool(simulation())

def λ_precondition_teammate_ready():
    return A1precondition_teammate_ready.bool(simulation())

def λ_target_forward():
    return A1target_forward.dist(simulation(), ego=True)

# Helper: Choose left or right side to overlap based on which one is open (simple heuristic)
def λ_choose_overlap_side():
    # If left side is open, use it, else use right
    left_prob = λ_target_move_left().max()
    right_prob = λ_target_move_right().max()
    if left_prob >= right_prob:
        return λ_target_move_left()
    else:
        return λ_target_move_right()

behavior CoachBehavior():
    do Idle() for 3 seconds

    do SetPlayerSpeed(1.0)
    # Allow both left and right overlap; narrate accordingly, logic decides side
    # do Speak("Move to overlapping position, either left or right at about 40 degrees and 8 meters from ball, to receive pass from teammate")
    # Decision: choose left or right side based on which is more open
    do MoveTo(λ_choose_overlap_side(), True)

    # do Speak("Wait until teammate passes the ball")
    do Idle() until λ_precondition_teammate_passed()

    # do Speak("Stop to receive the ball from teammate after pass")
    do StopAndReceiveBall()

    # Insert pause (Idle) after receiving to make this a true transition (per instructor's request)
    # do Speak("Pause momentarily to clearly separate ball reception and next action")
    do Idle() for 1 seconds

    # do Speak("Wait until you get ball possession")
    do Idle() until λ_precondition_coach_has_ball()

    # do Speak("Wait until teammate moves forward towards goal")
    do Idle() until λ_precondition_teammate_ready()

    # do Speak("Pass to your teammate who is advancing towards goal")
    do Pass('teammate')

    # do Speak("Wait until teammate passes towards goal")
    do Idle() until λ_precondition_coach_passed()

    # do Speak("Idle while teammate moves to score after receiving pass")
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