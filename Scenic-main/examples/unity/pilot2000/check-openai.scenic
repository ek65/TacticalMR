from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_0 = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 2.0, 'std': 0.5}, 'max': {'avg': 4.0, 'std': 0.2}, 'operator': 'within'})
A2target_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 3.0, 'std': 0.5}, 'max': None, 'operator': 'greater_than'})
A3target_1 = DistanceTo({'from': 'Coach', 'to': 'teammate', 'min': {'avg': 2.5, 'std': 0.4}, 'max': {'avg': 5.0, 'std': 0.3}, 'operator': 'within'})
A4target_2 = DistanceTo({'from': 'teammate', 'to': 'Coach', 'min': None, 'max': {'avg': 0.7, 'std': 0.1}, 'operator': 'less_than'})

A1precondition_0 = HasBallPossession({'player': 'teammate'})
A2precondition_1 = MakePass({'player': 'teammate'})
A3precondition_2 = HasBallPossession({'player': 'Coach'})
A4precondition_3 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': None, 'max': {'avg': 2.0, 'std': 0.2}, 'operator': 'less_than'})

def λ_target0():
    cond = A1target_0 and A2target_0
    return cond.dist(simulation(), ego=True)

def λ_target1():
    return A3target_1.dist(simulation(), ego=True)

def λ_target2():
    return A4target_2.dist(simulation(), ego=True)

def λ_precondition_0():
    return A1precondition_0.bool(simulation())

def λ_precondition_1():
    return A2precondition_1.bool(simulation())

def λ_precondition_2():
    return A3precondition_2.bool(simulation())

def λ_precondition_3():
    return A4precondition_3.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Wait for the teammate to have ball possession before making space.")
    do Idle() until λ_precondition_0()
    do Speak("Move 2 to 4 meters closer to your teammate, and more than 3 meters from the opponent.")
    do MoveTo(λ_target0(), False)
    do Speak("Wait until teammate makes a pass.")
    do Idle() until λ_precondition_1()
    do Speak("Move within 5 meters of your teammate to be ready to receive the pass.")
    do MoveTo(λ_target1(), False)
    do Speak("Wait until you have ball possession from a pass.")
    do Idle() until λ_precondition_2()
    do Speak("Check if opponent is less than 2 meters away after receiving ball.")
    if λ_precondition_3():
        do Speak("Opponent is too close, pass back to teammate to avoid losing the ball.")
        do Pass(teammate)
    else:
        do Speak("Opponent is not too close, consider turning and shooting at the goal.")
        do Shoot(goal)
    do Idle()

####Environment Behavior START####
# Parameters for variance
coach_start_dist = Range(5, 8)  # initial distance from teammate
coach_check_dist = Range(4, 6)   # how much closer coach checks
coach_check_angle = Range(-45, 45)  # angle of check (degrees)
opponent_dist = Range(2, 7)         # distance behind coach

# Behaviors
behavior TeammatePass():
    # Double checking gotBall to ensure the pass is triggered correctly
    # since MoveToBallAndGetPossession() might get interrupted
    gotBall = False
    try:
        do Idle() for 1.0 seconds  # Give coach time to start 
        do MoveToBallAndGetPossession()
        print("got ball")
        gotBall = True
        do Idle()
    interrupt when ego.triggerPass and self.gameObject.ballPossession and gotBall:
        ego.triggerPass = False
        print("trigger pass")
        do Idle() for 1.0 seconds
        do Pass(ego.xMark)
        # Idle after the pass happens
        do Idle() for 2.0 seconds
        
        # Wait to receive ball back from coach
        do Idle() until self.gameObject.ballPossession
        
        # When receiving ball back, move forward to opposite side of field
        if self.gameObject.ballPossession:
            # Determine which side coach and opponent are on
            coach_x = ego.position.x
            opponent_x = opponent.position.x
            
            # Calculate target position on opposite side
            # X-axis ranges from -10 to +10, with 0 at center
            # If coach and opponent are on positive side, go to negative side
            # If coach and opponent are on negative side, go to positive side
            if coach_x > 0 and opponent_x > 0:
                # Both on positive side (right), go to negative side (left)
                target_x = -6.0
            elif coach_x < 0 and opponent_x < 0:
                # Both on negative side (left), go to positive side (right)
                target_x = 6.0
            else:
                # Mixed positions, go to the side with more space
                # If coach is on left (negative), go right (positive)
                # If coach is on right (positive), go left (negative)
                target_x = 6.0 if coach_x < 0 else -6.0
            
            # Move forward to the target position (toward goal, so positive Y)
            target_position = Vector(target_x, 11.0, 0)
            do MoveToBehavior(target_position, distance=0.5)
            do Idle() for 1.0 seconds


    do Idle()

behavior OpponentFollowCoach():

    do Idle() for 5.5 seconds  # Wait 6 seconds before starting to follow
    
    # Set opponent speed
    do SetPlayerSpeed(4.0)
    
    while True:
        # Follow coach only until coach receives the ball
        if not ego.gameObject.ballPossession:
            # Follow coach and try to get close to them
            do MoveToBehavior(ego.position, distance=1.5)
        else:
            # Stop following - coach received the ball
            do Idle()
            
    





# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "blue", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Coach ahead of teammate by coach_start_dist, 
    with name "Coach", 
    with team "blue", 
    with behavior CoachBehavior(),
    with xMark Vector(0, 0, 0),  # Set initial xMark position
    with triggerPass False  # Initialize triggerPass to False

# Place opponent ahead of coach (closer to goal than coach)
opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)