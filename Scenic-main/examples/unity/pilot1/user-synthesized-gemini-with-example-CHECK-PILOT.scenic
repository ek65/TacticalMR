from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1target_0 = AtAngle(player='coach', ball='ball', left={'theta': {'avg': 38.5, 'std': 4.3}, 'dist': {'avg': 8.0, 'std': 0.8}}, right=None)
A1termination_0 = MakePass(player='teammate')
A1precondition_1 = HasBallPossession(player='coach')
A2precondition_1 = Pressure(player1='opponent', player2='coach')
A3precondition_1 = HasPath(obj1='coach', obj2='teammate', path_width={'avg': 2.0, 'std': 0.5})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_precondition1(scene, sample):
    return (A1precondition_1.bool(simulation()) and
            A2precondition_1.bool(simulation()) and
            A3precondition_1.bool(simulation()))

behavior CoachBehavior():
    do Idle() for 3 seconds

    do Speak("I'll move to the side to pull the defender away and create an open lane for my teammate.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)

    do Speak("The pass is on its way. I'm getting in position to receive the ball.")
    do StopAndReceiveBall()

    do Speak("The defender is closing me down, but my teammate has a clear shot. I'll pass it back.")
    do Idle() until λ_precondition1(simulation(), None)
    do Pass(teammate)

    do Idle()



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