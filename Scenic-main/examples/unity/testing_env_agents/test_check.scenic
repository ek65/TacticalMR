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
    do Speak("Check opponent position at an angle")
    do MoveTo(λ_target_check(), True)    
    # Coach receives ball immediately after MoveTo finishes
    do Idle() for 1 seconds  # Brief pause to assess situation
    # Decide based on opponent pressure
    pressure_result = λ_precondition2(simulation(), None)
    print("DEBUG: Pressure check result =", pressure_result)
    if pressure_result:

        do Speak("Opponent is attacking, pass back to teammate")
        do Pass(teammate)
    else:
        do Idle() for 2 seconds
        do Speak("Opponent is not attacking, go forward")
        do Idle() for 1 seconds
        do MoveTo(λ_target1())
        do Speak("Shoot to goal")
        do Shoot(goal)
    do Idle()

A_target0 = DistanceTo({
    'from': 'teammate',
    'to': 'Coach',
    'min': {'avg': 4.0, 'std': 1.0},
    'max': {'avg': 12.0, 'std': 2.0},
    'operator': 'within'
})

# COACH FEEDBACK: Added a height relation constraint.
# This ensures the coach stays "above" (in front of) the teammate,
# preventing them from moving into an invalid position behind the teammate.
A_height_relation = HeightRelation({
    'obj': 'Coach',
    'relation': 'above',
    'ref': 'teammate',
    'height_threshold': {'avg': 0.5, 'std': 0.2}
})

A_target1 = DistanceTo({
    'from': 'Coach',
    'to': 'goal',
    'min': {'avg': 8.0, 'std': 2.0},
    'max': {'avg': 15.0, 'std': 2.0},
    'operator': 'within'
})

A_target_check = AtAngle({
    'player': 'Coach',
    'ball': 'ball',
    'left': {'theta': {'avg': 45.0, 'std': 15.0}, 'dist': {'avg': 3.0, 'std': 1.0}},
    'right': {'theta': {'avg': 45.0, 'std': 15.0}, 'dist': {'avg': 3.0, 'std': 1.0}}
})

A_precondition0 = MakePass({'player': 'teammate'})
A_precondition1 = HasBallPossession({'player': 'Coach'})
A_precondition2 = Pressure({'player1': 'opponent', 'player2': 'Coach'})

def λ_target0():
    # COACH FEEDBACK: Modified to combine the distance constraint with the new height relation constraint.
    # This ensures the coach moves to a valid passing position while staying in front of the teammate.
    cond = A_target0 and A_height_relation
    return cond.dist(simulation(), ego=True)

def λ_target1():
    return A_target1.dist(simulation(), ego=True)

def λ_target_check():
    return A_target_check.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    return A_precondition0.bool(simulation())

def λ_precondition_1(scene, sample):
    return A_precondition1.bool(simulation())

def λ_precondition2(scene, sample):
    # Opponent is attacking (applying pressure)
    return A_precondition2.bool(simulation())


####Environment Behavior START####
# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(2, 7)         # distance behind coach


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


# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)