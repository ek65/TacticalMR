from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

C1_target0 = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 90.0, 'std': 20.0},
    'dist': {'avg': 8.0, 'std': 1.0}
})

C1_termination0 = MovingTowards({'obj': 'opponent', 'ref': 'Coach'})

C1_precondition_pass = MakePass({'player': 'teammate'})

C1_precondition_pass_back = HasPath({
    'obj1': 'teammate',
    'obj2': 'goal',
    'path_width': {'avg': 4.0, 'std': 0.5}
})

def λ_target0():
    return C1_target0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return C1_termination0.bool(simulation())

def λ_precondition_pass(scene, sample):
    return C1_precondition_pass.bool(simulation())

def λ_precondition_pass_back(scene, sample):
    return C1_precondition_pass_back.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds

    do Speak("I'll move to the side to pull the defender out and create an open passing lane.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)

    do Speak("The defender is following me. Now, I'll wait here for the pass from my teammate.")
    do Idle() until λ_precondition_pass(simulation(), None)

    do Speak("Here comes the pass. I am moving to intercept the ball and get possession.")
    do MoveToBallAndGetPossession()

    do Speak("Now I have the ball. I will wait until my teammate gets an open line to the goal.")
    do Idle() until λ_precondition_pass_back(simulation(), None)

    do Speak("My teammate is open! I'm passing the ball back to them for the shot.")
    do Pass(teammate)
    
    do Idle()
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