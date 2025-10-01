from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 3, 'std': 0.2}, 'max': None, 'operator': 'greater_than'})
A2target_0 = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 2, 'std': 0.1}, 'max': {'avg': 6, 'std': 0.2}, 'operator': 'within'})

A1precondition_pass_to_Coach = MakePass({'player': 'teammate'})
A2precondition_Coach_has_ball = HasBallPossession({'player': 'Coach'})
A3precondition_opponent_approaching = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A4precondition_pass_to_teammate = MakePass({'player': 'Coach'})
A5precondition_teammate_has_ball = HasBallPossession({'player': 'teammate'})

A1target_pass_to_teammate = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 2, 'std': 0.1}})
A2target_pass_forward = DistanceTo({'from': 'teammate', 'to': 'goal', 'min': None, 'max': {'avg': 17, 'std': 0.1}, 'operator': 'less_than'})

# --- NEW: Side movement constraint ---
A_side_space = AtAngle({
    'player': 'Coach', 
    'ball': 'ball', 
    'left': {
        'theta': {'avg': 35, 'std': 5}, # move about 35 degrees to side
        'dist': {'avg': 3, 'std': 0.3}
    },
    'right': {
        'theta': {'avg': 35, 'std': 5}, # either side
        'dist': {'avg': 3, 'std': 0.3}
    }
})
def λ_side_space():
    return A_side_space.dist(simulation(), ego=True)

def λ_target0():
    cond = A1target_0 & A2target_0
    return cond.dist(simulation(), ego = True)

def λ_precondition_pass_to_Coach():
    return A1precondition_pass_to_Coach.bool(simulation())

def λ_precondition_Coach_has_ball():
    return A2precondition_Coach_has_ball.bool(simulation())

def λ_precondition_opponent_approaching():
    return A3precondition_opponent_approaching.bool(simulation())

def λ_precondition_pass_to_teammate():
    return A4precondition_pass_to_teammate.bool(simulation())

def λ_precondition_teammate_has_ball():
    return A5precondition_teammate_has_ball.bool(simulation())

def λ_target_pass_to_teammate():
    return A1target_pass_to_teammate.bool(simulation())

def λ_target_pass_forward():
    return A2target_pass_forward.dist(simulation(), ego = True)

behavior CoachBehavior():
    do Idle() for 3 seconds
    #do Speak("Wait for teammate to pass the ball. Precondition: teammate passes to Coach.")
    #do Idle() until λ_precondition_pass_to_Coach()
    # --- CHANGED: Move to a side after waiting for the pass, before creating main space ---
    #do Speak("Move to the side to create more space for your teammate before asking for the pass.")
    #do MoveTo(λ_side_space(), False)
    do Speak("Now move to create space more than 3 meters from opponent and within 2-6 meters of teammate, and call for the pass.")
    do MoveTo(λ_target0(), True)
    do Speak("Wait to receive the ball. Precondition: Coach has ball possession.")
    do Idle() until λ_precondition_Coach_has_ball()
    do Speak("If opponent approaches Coach with the ball, act quickly.")
    if λ_precondition_opponent_approaching():
        do Speak("Opponent is pressuring. Pass to teammate if there is a clear path, path_width 2 meters.")
        do Pass('teammate')
    else:
        do Speak("Opponent is not pressuring. Shoot at the goal directly.")
        do Shoot('goal')
    do Speak("Wait for teammate to receive and be ready to shoot. Precondition: teammate has ball possession.")
    do Idle() until λ_precondition_teammate_has_ball()
    do Speak("Teammate should now shoot towards the goal.")
    do Shoot('goal')
    do Idle()

####Environment Behavior START####
# Parameters for variance
coach_start_dist = Range(5, 6)  # initial distance from teammate
opponent_dist = Range(4, 6)         # distance behind coach

# Behaviors
behavior TeammatePass():
    # Double checking gotBall to ensure the pass is triggered correctly
    # since MoveToBallAndGetPossession() might get interrupted
    gotBall = False
    try:
        do Idle() for 1.0 seconds  # Give coach time to start 
        do MoveToBallAndGetPossession()
        print("got ball")
        gotBall = True
        do Idle()
    interrupt when ego.triggerPass and self.gameObject.ballPossession and gotBall:
        ego.triggerPass = False
        print("trigger pass")
        do Idle() for 1.0 seconds
        do Pass(ego.xMark)
        # Idle after the pass happens
        do Idle() for 1.0 seconds
        
        # move forward to opposite side of field
        # Determine which side coach and opponent are on
        coach_x = ego.position.x
        opponent_x = opponent.position.x
        
        # Calculate target position on opposite side
        # X-axis ranges from -10 to +10, with 0 at center
        # If coach and opponent are on positive side, go to negative side
        # If coach and opponent are on negative side, go to positive side
        if coach_x > 0 and opponent_x > 0:
            # Both on positive side (right), go to negative side (left)
            target_x = -6.0
        elif coach_x < 0 and opponent_x < 0:
            # Both on negative side (left), go to positive side (right)
            target_x = 6.0
        else:
            # Mixed positions, go to the side with more space
            # If coach is on left (negative), go right (positive)
            # If coach is on right (positive), go left (negative)
            target_x = 6.0 if coach_x < 0 else -6.0
        
        # Move forward to the target position (toward goal, so positive Y)
        target_position = Vector(target_x, ego.position.y, 0)
        do MoveToBehavior(target_position, distance=0.5)
        do Idle() for 1.0 seconds

        do Idle() until self.gameObject.ballPossession
        do Shoot(goal)
        do Idle() for 1.0 seconds
        do Shoot(goal)

    do Idle()

behavior OpponentFollowCoach():

    do Idle() until ego.gameObject.ballPossession
    
    # Set opponent speed
    do SetPlayerSpeed(4.0)
    
    while True:
        # Follow coach only until coach receives the ball
        do MoveToBehavior(ego.position, distance=4)
            
    





# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "blue", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Coach ahead of teammate by coach_start_dist, 
    with name "Coach", 
    with team "blue", 
    with behavior CoachBehavior(),
    with xMark Vector(0, 0, 0),  # Set initial xMark position
    with triggerPass False  # Initialize triggerPass to False

# Place opponent ahead of coach (closer to goal than coach)
opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)

terminate when (ego.gameObject.stopButton)