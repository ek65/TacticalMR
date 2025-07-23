from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    # do Speak("Move into space to create a passing option for your teammate")
    do MoveTo(λ_target0(), True)
    # do Speak("Wait until teammate passes you the ball")
    do Idle() until λ_precondition_0(simulation(), None)
    # do Speak("Move to ball and get possession")
    do MoveToBallAndGetPossession()
    # do Speak("Wait until you receive the ball")
    do Idle() until λ_precondition_1(simulation(), None)
    if λ_precondition_2(simulation(), None):
        # do Speak("Opponent is following closely, fake one way and go the other")
        do MoveTo(λ_target1())
        # do Speak("Prepare for shot after creating space")
        do Idle() until λ_termination_0(simulation(), None)
        # do Speak("Now, shoot to goal")
        do Shoot(goal)
    elif λ_precondition_3(simulation(), None):
        # do Speak("Opponent is at medium distance, stop and play a safe pass back")
        do StopAndReceiveBall()
        # do Speak("Wait for control, then pass back to teammate")
        do Idle() until λ_termination_1(simulation(), None)
        # do Speak("Pass to your teammate")
        do Pass(teammate)
    else:
        # do Speak("Opponent far, turn and face goal for shot or pass")
        do MoveTo(λ_target2())
        # do Speak("Wait for chance to shoot or pass")
        do Idle() until λ_termination_2(simulation(), None)
        # do Speak("Take the shot if opportunity arises")
        do Shoot(goal)
    do Idle()


# Constraints and lambdas

A1target_0 = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg':3.5, 'std':0.5}, 'max': {'avg':8.0, 'std':1.0}, 'operator':'within'})
A1target_1 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg':1.0, 'std':0.1}, 'max': {'avg':1.5, 'std':0.2}, 'operator':'within'})
A2target_1 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1target_2 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg':7.0, 'std':2.0}, 'operator':'less_than'})
A2target_2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg':4.0, 'std':1.0}, 'max': None, 'operator':'greater_than'})
A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = HasBallPossession({'player': 'Coach'})
A1precondition_2 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A2precondition_2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': None, 'max': {'avg':1.5, 'std':0.2}, 'operator':'less_than'})
A1precondition_3 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg':1.5, 'std':0.2}, 'max': {'avg':4.0, 'std':0.7}, 'operator':'within'})
A1termination_0 = HasBallPossession({'player': 'Coach'})
A1termination_1 = HasBallPossession({'player': 'Coach'})
A1termination_2 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg':2.5, 'std':0.5}})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_target1():
    cond = A1target_1 and A2target_1
    return cond.dist(simulation(), ego=True)

def λ_target2():
    cond = A1target_2 and A2target_2
    return cond.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition_2(scene, sample):
    return (A1precondition_2.bool(simulation()) and A2precondition_2.bool(simulation()))

def λ_precondition_3(scene, sample):
    return A1precondition_3.bool(simulation())

def λ_termination_0(scene, sample):
    # Terminate when Coach no longer has the ball (intermediate event, not goal)
    return not A1termination_0.bool(simulation())

def λ_termination_1(scene, sample):
    # Terminate when Coach is no longer in safe control (i.e., on safe possession change, not goal event)
    return not A1termination_1.bool(simulation())

def λ_termination_2(scene, sample):
    # Terminate when Coach loses clear path to goal (not the shot event itself)
    return not A1termination_2.bool(simulation())



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

behavior TeammatePass():
    try:
        do Idle() for 1.0 seconds  # Give coach time to start 
        do MoveToBallAndGetPossession()
        print("got ball")
        while True:
            do Idle() for 0.1 seconds
            print("trigger pass val: " + str(ego.triggerPass))
        do Idle()
    interrupt when ego.triggerPass and self.gameObject.ballPossession:
        print("trigger pass1: " + str(ego.triggerPass))
        ego.triggerPass = False  # Reset triggerPass
        print("trigger pass2: " + str(ego.triggerPass))
        do Idle() for 1.0 seconds
        do Pass(ego.xMark)

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