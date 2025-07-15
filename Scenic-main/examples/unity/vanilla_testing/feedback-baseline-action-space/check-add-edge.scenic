from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("I will check to an open space at an angle to receive a pass.")
    do MoveTo(λ_target0())
    do Speak("Now I will wait for my teammate to make the pass.")
    do Idle() until λ_termination0(simulation(), None)

    do Speak("I see the pass coming, I will move to get possession of the ball.")
    do GetBallPossession(ball)

    if λ_precondition0(simulation(), None):
        do Speak("The opponent is pressuring me, so I will pass the ball back to my teammate.")
        do Pass(teammate)

        # CHANGE: Added actions to wait for a return pass and then shoot, as per feedback.
        # The original behavior ended after passing, which was incorrect.
        do Speak("I'll wait for the return pass.")
        do ReceiveBall()
        do Speak("Now I have the ball again, I'll take a shot.")
        do Shoot(goal)
    else:
        do Speak("I have enough space, so I will dribble forward and take a shot on goal.")
        do Shoot(goal)


# Constraints and conditions
A1target_0 = AtAngle({
    'player': 'Coach',
    'ball': 'ball',
    'left': {
        'theta': {'avg': 40.32, 'std': 6.4},
        'dist': {'avg': 5.8, 'std': 0.8}
    },
    'right': {
        'theta': {'avg': 40.32, 'std': 6.4},
        'dist': {'avg': 5.8, 'std': 0.8}
    }
})

A1termination_0 = MakePass({'player': 'teammate'})
A1precondition_0 = CloseTo({'obj': 'opponent', 'ref': 'Coach', 'max': {'avg': 2.1, 'std': 0.6}})

# Lambda functions
def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(simulation())

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
    do Idle() for 0.5 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    while True:
        do MoveToBehavior(ego.position)
        do Idle() for 0.1 seconds

# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "green", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Coach ahead of teammate by coach_start_dist, with name "Coach", with team "blue", with behavior CoachBehavior()

# Place opponent ahead of coach (further from goal than coach)
opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)