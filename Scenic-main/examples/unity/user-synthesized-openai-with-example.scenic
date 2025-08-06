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
    do Speak("You should move to the side of the field, away from the opponent, to create space. Move to a position about 3 meters from the center and between 7 and 9.5 meters up the field.")
    do MoveTo(λ_check_side(simulation()), True)
    do Speak("Wait until your teammate passes the ball to you.")
    do Idle() until λ_teammate_passed(simulation(), None)
    do Speak("Move to the ball and get possession.")
    do MoveToBallAndGetPossession()
    do Speak("Wait to see if the opponent decides to pressure you or not before taking your next action.")
    do Idle() until True  # Wait a decision step
    if λ_opponent_pressures(simulation(), None):
        do Speak("The opponent is pressuring you, so you should pass the ball back to your teammate. Look for the pass as soon as the opponent puts you under pressure and make a quick pass.")
        do Pass(teammate)
    else:
        do Speak("The opponent is not pressuring you, so you should dribble the ball up the field yourself. Keep close control and move forward up the field.")
        do MoveTo(λ_dribble_upfield(simulation()), False)
    do Idle()

A1_check_side_left = DistanceTo({'from': 'Coach', 'to': {'x': -2.9, 'y': 6.7}, 'min': None, 'max': {'avg': 1.0, 'std': 0.2}, 'operator': 'less_than'})
A1_check_side_right = DistanceTo({'from': 'Coach', 'to': {'x': 3.0, 'y': 9.6}, 'min': None, 'max': {'avg': 1.0, 'std': 0.2}, 'operator': 'less_than'})
A1_check_side_alt = DistanceTo({'from': 'Coach', 'to': {'x': -4.2, 'y': 8.4}, 'min': None, 'max': {'avg': 1.0, 'std': 0.2}, 'operator': 'less_than'})

A1_teammate_pass = MakePass({'player': 'teammate'})
A1_coach_has_ball = HasBallPossession({'player': 'Coach'})
A1_opponent_pressure = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1_dribble_upfield = DistanceTo({'from': 'Coach', 'to': {'x': 0.0, 'y': 13.5}, 'min': None, 'max': {'avg': 4.0, 'std': 1.0}, 'operator': 'less_than'})

def λ_check_side(scene):
    # Prioritize left, right, or alternate check run, depending on where available.
    # Instruct the avatar to move to the closest one the current scenario allows
    # This function is representative and can be expanded by actual scenario knowledge.
    if A1_check_side_left.bool(scene):
        return A1_check_side_left.dist(scene, ego=True)
    elif A1_check_side_right.bool(scene):
        return A1_check_side_right.dist(scene, ego=True)
    else:
        return A1_check_side_alt.dist(scene, ego=True)

def λ_teammate_passed(scene, sample):
    # Wait until teammate makes the pass (as seen in all narrations)
    return A1_teammate_pass.bool(scene)

def λ_opponent_pressures(scene, sample):
    # If the opponent pressures upon receiving, branch to quick pass, else dribble up.
    return A1_opponent_pressure.bool(scene)

def λ_dribble_upfield(scene):
    return A1_dribble_upfield.dist(scene, ego=True)

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