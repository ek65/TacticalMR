from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1_target0 = AtAngle({'player': 'Coach', 'ball': 'ball', 'left': {'theta': {'avg': 25, 'std': 2.0}, 'dist': {'avg': 8, 'std': 1.0}}, 'right': {'theta': {'avg': 25, 'std': 2.0}, 'dist': {'avg': 8, 'std': 1.0}}})
A1_precondition1 = HasBallPossession({'player': 'Coach'})
A1_precondition2 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1_target3 = HeightRelation({'obj': 'Coach', 'relation': 'above', 'ref': None, 'height_threshold': {'avg': 4.0, 'std': 1.0}})
T1_termination_dribble = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 2.0, 'std': 0.5}})

def λ_target0():
	return A1_target0.dist(simulation(), ego=True)

def λ_precondition1():
	return A1_precondition1.bool(simulation())

def λ_precondition2():
	return A1_precondition2.bool(simulation())

def λ_target3():
	return A1_target3.dist(simulation(), ego=True)

def λ_termination_dribble():
	return T1_termination_dribble.bool(simulation())

behavior CoachBehavior():
	do Idle() for 3 seconds
	do Speak("I will move to a spot at an angle of about 25 degrees and 8 meters from the ball to get open for a pass.")
	do MoveTo(λ_target0(), True)
	do Speak("I will wait until I receive the ball from my teammate.")
	do Idle() until λ_precondition1()
	do Speak("Now that I have the ball, I check if the opponent is pressuring me.")
	if λ_precondition2():
		do Speak("The opponent is pressuring me, so I will pass the ball back to my teammate.")
		do Pass(teammate)
	else:
		do Speak("The opponent is not pressuring me, so I will dribble the ball forward about 4 meters.")
		do MoveTo(λ_target3(), False) until λ_termination_dribble()
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