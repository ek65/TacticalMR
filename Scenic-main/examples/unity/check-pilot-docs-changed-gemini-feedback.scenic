from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random


behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I'll move to the side to pull the defender away and create space for a pass.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)

    do Speak("The defender is following me. I'll wait here for the pass from my teammate.")
    do Idle() until λ_precondition1(simulation(), None)

    do Speak("Receiving the pass now that my teammate has made it.")
    do ReceiveBall() until λ_termination1(simulation(), None)

    do Speak("I have the ball and the defender is closing in. I'll pass it back to my open teammate.")
    do Idle() until λ_precondition2(simulation(), None)

    do Pass(teammate)
    do Idle()


# Constraint Instances

A1target_0 = AtAngle({
    'player': 'coach',
    'ball': 'ball',
    'left': {
        'theta': {'avg': 32.3, 'std': 6.4},
        'dist': {'avg': 6.8, 'std': 0.5}
    },
    'right': {
        'theta': {'avg': 32.3, 'std': 6.4},
        'dist': {'avg': 6.8, 'std': 0.5}
    }
})

A1termination_0 = Pressure({
    'player1': 'opponent',
    'player2': 'Coach'
})

A1precondition_1 = MakePass({
    'player': 'teammate'
})

A1termination_1 = MovingTowards({
    'obj': 'opponent',
    'ref': 'Coach'
})

A1precondition_2 = HasBallPossession({
    'player': 'Coach'
})

A2precondition_2 = Pressure({
    'player1': 'opponent',
    'player2': 'Coach'
})

# BUG FIX: The original code incorrectly checked for a path from the teammate to the goal.
# This was corrected to check for a path from the coach to the teammate, which is necessary for the coach to make the pass.
A3precondition_2 = HasPath({
    'obj1': 'Coach',
    'obj2': 'teammate',
    'path_width': {'avg': 3.0, 'std': 0.5}
})


# Lambda/Helper Functions

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_termination1(scene, sample):
    return A1termination_1.bool(simulation())

def λ_precondition2(scene, sample):
    return (
        A1precondition_2.bool(simulation()) and
        A2precondition_2.bool(simulation()) and
        A3precondition_2.bool(simulation())
    )
    
# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do GetBallPossession(ball)
    print("got ball")
    do Idle() for 5.0 seconds
    do Pass(ego)
    do Idle()

behavior OpponentFollowCoach():
    do Idle() for 1.0 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    while True:
        if distance from self to ego > 2.0:
            do MoveToBehavior(ego.position, distance=2.0)
        else:
            do Idle() for 0.1 seconds

# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "blue", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Coach ahead of teammate by coach_start_dist, with name "Coach", with team "blue", with behavior CoachBehavior()

# Place opponent ahead of coach (further from goal than coach)
opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)