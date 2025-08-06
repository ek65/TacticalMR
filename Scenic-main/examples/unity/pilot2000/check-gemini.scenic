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
    do Speak("I'll move about 30 degrees and 7 meters away from the ball to create an open passing lane.")
    do MoveTo(λ_target0(), True)
    do Speak("Now I'll stop my movement to receive the incoming pass from my teammate.")
    do StopAndReceiveBall()
    do Speak("I'll check if the opponent is more than 3 meters away to decide whether to shoot or pass.")
    if λ_precondition_shoot():
        do Speak("The opponent is far enough away, giving me space to take a shot at the goal.")
        do Shoot(goal)
    else:
        do Speak("The opponent is too close, so I'll pass the ball back to my teammate to maintain possession.")
        do Pass(teammate)
    do Idle()

C1_AtAngle_Move = AtAngle({
    'player': 'Coach',
    'ball': 'ball',
    'left': {'theta': {'avg': 30.0, 'std': 10.0}, 'dist': {'avg': 7.0, 'std': 2.0}},
    'right': {'theta': {'avg': 30.0, 'std': 10.0}, 'dist': {'avg': 7.0, 'std': 2.0}}
})

C2_Dist_Shoot_Decision = DistanceTo({
    'from': 'Coach',
    'to': 'opponent',
    'min': {'avg': 2.5, 'std': 0.5},
    'max': None,
    'operator': 'greater_than'
})

def λ_target0():
    return C1_AtAngle_Move.dist(simulation(), ego=True)

def λ_precondition_shoot():
    return C2_Dist_Shoot_Decision.bool(simulation())

def λ_termination():
    return False

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