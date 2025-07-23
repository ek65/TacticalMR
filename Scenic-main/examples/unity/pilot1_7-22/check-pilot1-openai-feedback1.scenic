from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####
behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("First, move to the flank to create space and pull the defender away from goal")
    do MoveTo(λ_target0())
    do Speak("Wait to receive the ball from your teammate")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Receive and stop with the ball")
    do StopAndReceiveBall()
    do Speak("Wait until you are under pressure and cannot shoot")
    do Idle() until λ_precondition_1(simulation(), None)
    do Speak("Return the ball to your teammate for a shot")
    do Pass(teammate)
    do Speak("Wait for your teammate to shoot towards the goal")
    do Idle() until λ_precondition_2(simulation(), None)
    do Speak("End with an idle action")
    do Idle()

# Constraint API instantiations

A1target_0 = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'defender',
    'theta': {'avg': 30.0, 'std': 5.0},
    # FEEDBACK: The coach stated that the agent "moved a little bit too far".
    # CHANGE: Reduced the average distance from 7.0 to 5.0 to ensure the agent
    # moves to a closer flank position as indicated by the coach.
    'dist': {'avg': 5.0, 'std': 1.0},
})

A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = Pressure({'player1': 'defender', 'player2': 'Coach'})
A1precondition_2 = HasPath({'obj1': 'teammate', 'obj2': 'goal', 'path_width': {'avg': 2.5, 'std': 0.2}})

# Target, precondition, and termination lambdas

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_1(scene, sample):
    # Coach is under pressure and cannot shoot
    return A1precondition_1.bool(simulation())

def λ_precondition_2(scene, sample):
    # Teammate (after receiving pass) has a path to the goal to shoot
    return A1precondition_2.bool(simulation())
####Environment Behavior START####

####Environment Behavior START####
# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do MoveToBallAndGetPossession()
    print("got ball")
    do Idle() for 10.0 seconds
    do Pass(ego)
    do Idle()
####Environment Behavior START####
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