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
    do Speak("Lure the defender out wide to open up space for teammate")
    do MoveTo(λ_target0())
    do Speak("Wait for teammate to pass you the ball")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Move to the ball and get possession")
    do MoveToBallAndGetPossession()
    do Speak("Wait for defender to pressure you")
    do Idle() until λ_precondition_1(simulation(), None)
    do Speak("The defender is closing down, so you cannot shoot now")
    do Idle() until λ_precondition_2(simulation(), None)
    do Speak("Look, your teammate has a clear line to goal")
    do Idle() until λ_precondition_3(simulation(), None)
    do Speak("Return the ball to your teammate in space")
    do Pass(teammate)
    do Speak("Wait for your teammate to shoot at goal")
    do Idle() until λ_precondition_4(simulation(), None)
    do Speak("Scenario ends after teammate shoots")
    do Idle()

A1target_0 = DistanceTo({'from': 'Coach', 'to': 'defender', 'min': {'avg': 5.6, 'std': 0.5}, 'max': None, 'operator': 'greater_than'})
A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = Pressure({'player1': 'defender', 'player2': 'Coach'})
A1precondition_2 = HasBallPossession({'player': 'Coach'})
A1precondition_3 = HasPath({'obj1': 'teammate', 'obj2': 'goal', 'path_width': {'avg': 2.8, 'std': 0.3}})
A1precondition_4 = MakePass({'player': 'teammate'})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition_2(scene, sample):
    return A1precondition_2.bool(simulation())

def λ_precondition_3(scene, sample):
    return A1precondition_3.bool(simulation())

def λ_precondition_4(scene, sample):
    return A1precondition_4.bool(simulation())

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
    try:
        do Idle() for 1.0 seconds  # Give coach time to start 
        do MoveToBallAndGetPossession()
        print("got ball")
        do Idle()
    interrupt when ego.triggerPass and self.gameObject.ballPossession:
        ego.triggerPass = False
        print("trigger pass")
        do Idle() for 1.0 seconds
        do Pass(ego.xMark)

    do Idle()

####Environment Behavior START####
behavior OpponentFollowCoach():
    do Idle() for 1.0 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    while True:
        if distance from self to ego > 3.5:
            do MoveToBehavior(ego.position, distance=3.5)
        else:
            do Idle() for 0.1 seconds

# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "blue", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Coach ahead of teammate by coach_start_dist, 
    with name "Coach", 
    with team "blue", 
    with behavior CoachBehavior(),
    with xMark Vector(0, 0, 0),  # Set initial xMark position
    with triggerPass False  # Initialize triggerPass to False

# Place opponent ahead of coach (further from goal than coach)
opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)