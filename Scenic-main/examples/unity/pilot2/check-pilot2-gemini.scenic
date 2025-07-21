from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1target_0 = AtAngle({'player': 'Coach', 'ball': 'ball', 'left': {'theta': {'avg': 45.0, 'std': 10.0}, 'dist': {'avg': 7.5, 'std': 1.0}}, 'right': {'theta': {'avg': 45.0, 'std': 10.0}, 'dist': {'avg': 7.5, 'std': 1.0}}})
A2target_0 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 4.0, 'std': 0.5}, 'operator': 'greater_than'})
A1termination_0 = MovingTowards({'obj': 'opponent', 'ref': 'Coach'})
A1precondition_1 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 1.5, 'std': 0.5}})
A1termination_1 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'max': {'avg': 2.0, 'std': 0.2}, 'operator': 'less_than'})
A1precondition_2 = HasBallPossession({'player': 'Coach'})
A1precondition_shoot = DistanceTo({'from': 'Coach', 'to': 'opponent', 'max': {'avg': 3.5, 'std': 0.5}, 'operator': 'less_than'})
A2precondition_shoot = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.5}})

def λ_target0():
    cond = A1target_0 and A2target_0
    return cond.dist(simulation(), ego=True)

def λ_termination0(simulation, sample):
    return A1termination_0.bool(simulation)

def λ_precondition1(simulation, sample):
    return A1precondition_1.bool(simulation)

def λ_termination1(simulation, sample):
    return A1termination_1.bool(simulation)

def λ_precondition2(simulation, sample):
    return A1precondition_2.bool(simulation)

def λ_precondition_shoot(simulation, sample):
    return (not A1precondition_shoot.bool(simulation)) and A2precondition_shoot.bool(simulation)

behavior CoachBehavior():
    do Idle() for 3 seconds
    
    do Speak("I'll move to create space and an angle for a pass.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    
    do Speak("Now I am in a good position, I will wait for my teammate to make the pass.")
    do Idle() until λ_precondition1(simulation(), None)
    do StopAndReceiveBall() until λ_termination1(simulation(), None)
    
    do Speak("I have the ball, I need to decide my next move based on the defender's position.")
    do Idle() until λ_precondition2(simulation(), None)
    
    if λ_precondition_shoot(simulation(), None):
        do Speak("The defender is far enough away, I have a clear path to shoot.")
        do Shoot(goal)
    else:
        do Speak("The defender is pressuring me, so I must pass back to my teammate.")
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
terminate when (ego.gameObject.stopButton)