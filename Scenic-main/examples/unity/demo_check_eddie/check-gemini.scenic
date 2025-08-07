from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_0 = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'opponent', 'theta': {'avg': 25.0, 'std': 2.5}, 'dist': {'avg': 8.0, 'std': 0.8}})
A1precondition_0 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1termination_0 = MakePass({'player': 'teammate'})

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0():
    return A1termination_0.bool(simulation())

def λ_precondition0():
    return A1precondition_0.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("I will move to the side at an angle of 25 degrees and distance of 8 meters to lure the opponent.")
    do MoveTo(λ_target0(), True) #until λ_termination0()
    do Speak("Now I will stop and receive the pass from my teammate.")
    do StopAndReceiveBall()
    do Speak("I'll check if the opponent is pressuring me.")
    do Idle() until True
    if λ_precondition0():
        do Speak("The opponent is pressuring me, so I'll pass back to my teammate.")
        do Pass(teammate)
    else:
        do Speak("The opponent is not pressuring me, so I'll dribble towards the goal.")
        do MoveTo('goal', False)
    do Idle()

####Environment Behavior START####
# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(4, 7)         # distance behind coach
opponent_speed = 2        # opponent's movement speed

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
            target_position = Vector(target_x, 10.0, 0)
            do MoveToBehavior(target_position, distance=0.5)
            do Idle() for 1.0 seconds

    do Idle()

behavior OpponentFollowCoach():
    do Idle() for 1.0 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    
    # Track if we've already made a decision when ego received the ball
    decision_made = False
    go_to_coach = False
    
    while True:
        # Follow coach and maintain exactly 5m distance from ego
        if distance from self to ego > 5.1:
            do MoveToBehavior(ego.position, distance=5)
        elif distance from self to ego < 4.9:
            do MoveToBehavior(ego.position, distance=5)
        else:
            do Idle() for 0.1 seconds
            
        # Check if ego has received the ball and we haven't made a decision yet
        if ego.gameObject.ballPossession and not decision_made:
            # Wait a bit before making decision to give coach time
            do Idle() for 1.5 seconds
            print("DEBUG: Opponent making attack decision")
            # Uniformly decide whether to go to coach or stay on radius
            decision = Uniform(0, 1)
            go_to_coach = (decision < 0.5)
            decision_made = True
            
            if True:
                do Idle() for 1.0 seconds  # Additional wait before attack
                # Go to coach (closer distance)
                print('DEBUG: Opponent attacking!')
                do MoveToBehavior(ego.position, distance=2)
                do InterceptBall()
            else:
                # Stay on current radius (3.5m from ego)
                do Idle() for 0.1 seconds
                # Continue maintaining 5m distance
                while ego.gameObject.ballPossession:
                    if distance from self to ego > 5:
                        do MoveToBehavior(ego.position, distance=5)
                    else:
                        do Idle() for 0.1 seconds
                        
        # If ego no longer has the ball, reset decision tracking
        if not ego.gameObject.ballPossession:
            decision_made = False
            go_to_coach = False
            
        # If opponent is close to coach and ball is nearby, intercept it
        if distance from self to ego <= 1.0 and distance from self to ball <= 1.0:
            do InterceptBall()

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