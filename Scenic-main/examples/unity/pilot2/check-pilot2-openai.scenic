from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Step closer to teammate to create space from defender.")
    do MoveTo(λ_target0())
    do Speak("Wait until teammate passes the ball.")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Move to receive the ball and get possession.")
    do MoveToBallAndGetPossession()
    do Speak("Quickly check defender's position; prepare to turn or pass.")
    do Idle() until λ_precondition_1(simulation(), None)
    if λ_precondition_2(simulation(), None):
        do Speak("Defender is too close to turn, pass back to teammate.")
        do Pass('teammmate')
        do Speak("Wait until teammate receives the ball.")
        do Idle() until λ_precondition_3(simulation(), None)
    else:
        do Speak("You have space, turn and shoot towards goal.")
        do Shoot('goal')
    do Idle()

# ----- CONSTRAINT CLASS INSTANTIATIONS -----
A1target_0 = DistanceTo({'from': 'Coach', 'to': 'teammmate', 'min': None, 'max': {'avg': 2.2, 'std': 0.3}, 'operator': 'less_than'})
A1precondition_0 = MakePass({'player': 'teammmate'})
A1precondition_1 = HasBallPossession({'player': 'Coach'})
A2precondition_2 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': None, 'max': {'avg': 1.7, 'std': 0.2}, 'operator': 'less_than'})
A1precondition_2 = HasBallPossession({'player': 'Coach'})
A1precondition_3 = HasBallPossession({'player': 'teammmate'})

# ----- LAMBDAS FOR TARGETS, PRECONDITIONS, TERMINATION -----
def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    # Wait for teammate to execute the pass
    return A1precondition_0.bool(simulation())

def λ_precondition_1(scene, sample):
    # Wait for Coach to have possession (receive the ball)
    return A1precondition_1.bool(simulation())

def λ_precondition_2(scene, sample):
    # Defender is very close, i.e., within 1.7 meters (must pass back)
    return (A1precondition_2.bool(simulation()) and A2precondition_2.bool(simulation()))

def λ_precondition_3(scene, sample):
    # Wait until teammate has ball again
    return A1precondition_3.bool(simulation())




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
terminate when (ego.gameObject.stopButton)