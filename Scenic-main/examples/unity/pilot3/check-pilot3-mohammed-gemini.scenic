from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1_precondition1 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'max': {'avg': 1.5, 'std': 0.2}, 'operator': 'less_than'})
A1_precondition2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 1.5, 'std': 0.2}, 'max': {'avg': 5.0, 'std': 0.5}, 'operator': 'within'})
A1_target0 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 4.0, 'std': 0.5}, 'operator': 'greater_than'})
A2_target0 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.1}})
A1_precondition3 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.1}})
A1_termination0 = MakePass({'player': 'teammate'})
A1_termination1 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 3.0, 'std': 0.2}, 'operator': 'greater_than'})

def λ_target0():
    cond = A1_target0 and A2_target0
    return cond.dist(simulation(), ego=True)

def λ_termination0(self):
    return A1_termination0.bool(self)

def λ_termination1(self):
    return A1_termination1.bool(self)

def λ_precondition1(scene, sample):
    return A1_precondition1.bool(simulation())

def λ_precondition2(scene, sample):
    return A1_precondition2.bool(simulation())

def λ_precondition3(scene, sample):
    return A1_precondition3.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I'm running towards my teammate to get open and receive the ball.")
    do MoveToBallAndGetPossession(ball)
    do Speak("Now I have the ball. I'll check the defender's position to decide my next move.")
    if λ_precondition1(simulation(), None):
        do Speak("He's marking me too tightly. I'll try to get past him and then shoot.")
        do MoveTo(λ_target0(), termination=λ_termination1)
        do Speak("Now that I've created some space, I'll take the shot.")
        do Idle() until λ_precondition3(simulation(), None)
        do Shoot(goal)
    elif λ_precondition2(simulation(), None):
        do Speak("He's in a tricky spot. I'll play it safe and pass back to my teammate.")
        do Pass(teammate)
    else:
        do Speak("He's giving me a lot of space. I'll take the opportunity to shoot.")
        do Idle() until λ_precondition3(simulation(), None)
        do Shoot(goal)
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