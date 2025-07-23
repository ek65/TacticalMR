from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Hold position, waiting for teammate's pass and possession")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Move diagonally away from opponent to check into space")
    do MoveTo(λ_target0())
    do Speak("Wait for the ball and get possession after the pass")
    do Idle() until λ_precondition_1(simulation(), None)
    do Speak("Move to the ball and get possession to receive the pass")
    do MoveToBallAndGetPossession(ball)
    do Speak("Hold position, evaluating next action with the ball and opponent in front")
    do Idle() until λ_precondition_2(simulation(), None)
    if λ_precondition_3(simulation(), None):
        do Speak("Pass back to teammate on the side for direct attack")
        do Pass('teammate')
    else:
        do Speak("If path to goal is clear, proceed to shoot")
        do Shoot(goal)
    do Idle()

# Constraint and helper definitions:

# Wait for teammate to receive or gain possession first
A1precondition_0 = HasBallPossession({'player': 'teammate'})

# After Coach checks, wait for pass and ball approaching Coach
A1target_0 = AtAngle({
    'player': 'Coach',
    'ball': 'ball',
    'left': {
        'theta': {'avg': 43.0, 'std': 6.0},
        'dist': {'avg': 5.7, 'std': 0.7}
    },
    'right': {
        'theta': {'avg': 43.0, 'std': 6.0},
        'dist': {'avg': 5.7, 'std': 0.7}
    }
})

A1precondition_1 = MakePass({'player': 'teammate'})

# After move to and get possession, wait until Coach has the ball
A1precondition_2 = HasBallPossession({'player': 'Coach'})

# Branching: If Coach doesn't have a good path to goal or is pressured, pass back to teammate;
# otherwise, shoot.
A1precondition_3a = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.2}})
A2precondition_3a = Pressure({'player1': 'opponent', 'player2': 'Coach'})

def λ_target0():
    # Diagonal movement to left or right away from opponent; use either side as both are described
    return A1target_0.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition_2(scene, sample):
    return A1precondition_2.bool(simulation())

def λ_precondition_3(scene, sample):
    # Choose to pass back if there is NOT a clear path to goal OR under pressure
    return not A1precondition_3a.bool(simulation()) or A2precondition_3a.bool(simulation())



# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do MoveToBallAndGetPossession(ball)
    print("got ball")
    do Idle() for 10.0 seconds
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
terminate when (ego.gameObject.stopButton)