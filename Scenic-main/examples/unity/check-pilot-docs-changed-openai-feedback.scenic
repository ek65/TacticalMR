from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random


behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Let's pull the defender to the side and open the goal for teammate.")
    do MoveTo(λ_target0())

    do Speak("Wait until pass from teammate.")
    do Idle() until λ_precondition_0(simulation(), None)

    do Speak("Receive the ball and secure possession.")
    do ReceiveBall()

    do Speak("Defender is closing down, let's wait and look for a pass or line to goal.")
    do Idle() until λ_precondition_1_2(simulation(), None)

    if λ_precondition1(simulation(), None):
        do Speak("Defender blocks my shot, pass back to teammate for better shot.")
        do Pass(teammate)
        do Speak("Idle while teammate shoots at open goal.")
        do Idle()
    else:
        do Speak("Path is clear, go for the shot!")
        do Shoot(goal)
        do Idle()


# Constraint Instances

# BUG FIX: A_target0 was incorrectly defined as a MoveTo action object.
# It should be a dictionary of parameters specifying the destination for the action.
# I have changed it to be a dictionary, which will be passed to the MoveTo action.
A_target0 = {
    'player': 'Coach',
    'zone': 'side_area'  # destination on the side, assumed as per demonstration intent
}

A_precondition_0 = MakePass({
    'player': 'teammate'
})

A_precondition_1 = Pressure({
    'player1': 'opponent',
    'player2': 'Coach'
})

A_precondition_2 = HasPath({
    'obj1': 'teammate',
    'obj2': 'goal',
    'path_width': {'avg': 2.1, 'std': 0.2}
})

A_precondition_3 = HasPath({
    'obj1': 'Coach',
    'obj2': 'goal',
    'path_width': {'avg': 2.1, 'std': 0.2}
})


# Lambda/Helper Functions

def λ_target0():
    # BUG FIX: The original code called a non-existent '.dist' method, causing the error.
    # The function now returns the parameter dictionary directly, providing a valid
    # destination for the MoveTo action in the behavior.
    return A_target0

def λ_precondition_0(scene, sample):
    return A_precondition_0.bool(simulation())

def λ_precondition1(scene, sample):
    # Defender pressure and no clear path to goal
    return A_precondition_1.bool(simulation()) and not A_precondition_3.bool(simulation())

def λ_precondition2(scene, sample):
    # Teammate has a clear path to the goal
    return A_precondition_2.bool(simulation())

def λ_precondition_1_2(scene, sample):
    # Wait for defender pressure and teammate has clear path, or Coach gets clear shot
    return λ_precondition1(scene, sample) or A_precondition_3.bool(simulation())
    

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