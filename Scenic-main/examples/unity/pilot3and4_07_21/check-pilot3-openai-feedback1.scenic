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
    do Speak("Move to create passing option and space for teammate")
    do MoveTo(λ_target0())
    do Speak("Wait for the ball to be passed to you")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Move to the ball and get possession")
    do MoveToBallAndGetPossession()
    do Speak("Wait until you have the ball")
    do Idle() until λ_precondition_1(simulation(), None)
    if λ_precondition2(simulation(), None):
        do Speak("If pressed very closely, fake one way and go the other")
        do MoveTo(λ_target1())
        do Speak("Now you have time to shoot to goal")
        # COACH FEEDBACK: Removed the Idle action that was causing the behavior to hang.
        # The coach should shoot immediately after the fake move, not wait for an unmet condition.
        # do Idle() until λ_termination_1(simulation(), None)
        do Speak("Shoot to finish the move")
        do Shoot(goal)
    elif λ_precondition3(simulation(), None):
        do Speak("Opponent is at medium distance, play safe and pass back")
        do Pass(teammate)
    else:
        do Speak("Opponent is far, turn, gain time and shoot")
        do Idle() until λ_termination_2(simulation(), None)
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
    'from': 'opponent',
    'to': 'Coach',
    'min': {'avg': 1.2, 'std': 0.4},
    'max': {'avg': 2.0, 'std': 0.4},
    'operator': 'within'
})

A_precondition0 = MakePass({'player': 'teammate'})
A_precondition1 = HasBallPossession({'player': 'Coach'})
A_precondition2 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A_precondition3 = DistanceTo({
    'from': 'opponent',
    'to': 'Coach',
    'min': {'avg': 2.3, 'std': 0.5},
    'max': {'avg': 4.0, 'std': 1.0},
    'operator': 'within'
})
A_precondition_far = DistanceTo({
    'from': 'opponent',
    'to': 'Coach',
    'min': {'avg': 4.5, 'std': 1.5},
    'max': None,
    'operator': 'greater_than'
})

def λ_target0():
    # COACH FEEDBACK: Modified to combine the distance constraint with the new height relation constraint.
    # This ensures the coach moves to a valid passing position while staying in front of the teammate.
    cond = A_target0 and A_height_relation
    return cond.dist(simulation(), ego=True)

def λ_target1():
    return A_target1.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    return A_precondition0.bool(simulation())

def λ_precondition_1(scene, sample):
    return A_precondition1.bool(simulation())

def λ_precondition2(scene, sample):
    # Opponent is pressing very closely (tight pressure, close contact)
    return (A_precondition2.bool(simulation()) and
            (A_target1.dist(simulation(), ego=True) is not None))

def λ_precondition3(scene, sample):
    # Opponent at a medium distance
    return A_precondition3.bool(simulation())

def λ_precondition_far(scene, sample):
    return A_precondition_far.bool(simulation())

def λ_termination_1(scene, sample):
    # Terminate move/fake when Coach has separated/distance increases
    return λ_precondition_far(simulation(), sample)

def λ_termination_2(scene, sample):
    # Terminate after Coach has turned to face and oriented to goal
    # Use operator as time or angle or extra separation (for demonstration, wait for separation to increase)
    return λ_precondition_far(simulation(), sample)
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
    do Idle() for 1.0 seconds  # Give coach time to start 
    do MoveToBallAndGetPossession()
    print("got ball")
    do Idle() for 10.0 seconds
    do Pass(ego)
    do Idle()
####Environment Behavior START####
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