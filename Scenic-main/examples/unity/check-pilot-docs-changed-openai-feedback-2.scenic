from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random



behavior CoachBehavior():
    do Idle() for 3 seconds

    # MODIFIED: Changed narration to reflect the 'overlap' action, as per coach feedback.
    # The original narration was misleading as the coach does not have the ball and was moving away.
    do Speak("I will overlap my teammate to attract the defender.")
    do MoveTo(λ_target0())

    do Speak("Waiting until the defender pressures or a pass angle opens.")
    do Idle() until λ_precondition_0(simulation(), None)

    do Speak("Get possession of the ball to threaten the defense.")
    do GetBallPossession(ball)

    do Speak("Waiting for the defender to close me, creating space for my teammate.")
    do Idle() until λ_precondition_1(simulation(), None)

    do Speak("Move into space to the side for a pass opportunity.")
    do MoveTo(λ_target1())

    do Speak("Waiting for the perfect moment to receive the ball.")
    do Idle() until λ_precondition_2(simulation(), None)

    do Speak("Receive the ball and look for an option.")
    do ReceiveBall()

    do Speak("Wait until I'm blocked. Will play back to teammate.")
    do Idle() until λ_precondition_3(simulation(), None)

    do Speak("Make a safe pass back to teammate.")
    do Pass(teammate)

    do Speak("Wait for teammate to be in position for a shot.")
    do Idle() until λ_precondition_4(simulation(), None)

    do Speak("Teammate should shoot for goal after my pass.")
    do Shoot(goal)

    do Idle()


# Constraint Instances

# MODIFIED: Replaced the 'DistanceTo' constraint with an 'Overlap' constraint.
# The original 'DistanceTo' caused the coach to move backward incorrectly.
# The new 'Overlap' constraint implements the coach's feedback to "overlap the teammate to attract the defender",
# which was explicitly mentioned in original narrated demonstration 3.
Demo0_MoveTo_draw_defender = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 35.0, 'std': 5.0},
    'dist': {'avg': 4.0, 'std': 0.5}
})

Demo0_Pressure_on_Coach = Pressure({
    'player1': 'opponent',
    'player2': 'Coach'
})

# ADDED: This new constraint checks for an open pass angle from the teammate to the coach.
# It is used in the updated 'λ_precondition_0' to resolve the deadlock identified in the coach's feedback,
# where the simulation would stall waiting for a condition that might not occur.
Demo0_Pass_angle_to_Coach = HasPath({
    'obj1': 'teammate',
    'obj2': 'Coach',
    'path_width': {'avg': 2.0, 'std': 0.5}
})

Demo0_MoveTo_space = AtAngle({
    'player': 'Coach',
    'ball': 'ball',
    'right': {
        'theta': {'avg': 43.0, 'std': 6.0},
        'dist': {'avg': 8.2, 'std': 0.88}
    }
})

Demo0_ReceiveBall = MakePass({
    'player': 'teammate'
})

Demo0_Close_blocked = CloseTo({
    'obj': 'opponent',
    'ref': 'Coach',
    'max': {'avg': 1.7, 'std': 0.2}
})

Demo0_PassBack = HasBallPossession({
    'player': 'Coach'
})

Demo0_Teammate_ready = HasBallPossession({
    'player': 'teammate'
})

Demo0_Coach_shot_angle = HasPath({
    'obj1': 'teammate',
    'obj2': 'goal',
    'path_width': {'avg': 2.6, 'std': 0.25}
})


# Lambda/Helper Functions

def λ_target0():
    # MODIFIED: This function now uses the corrected 'Overlap' constraint defined above
    # to ensure the coach performs the correct initial movement.
    return Demo0_MoveTo_draw_defender.dist(simulation(), ego=True)

def λ_precondition_0(scene, sample):
    # MODIFIED: The original precondition only checked for defender pressure, causing a deadlock.
    # Added a check for an open pass angle from the teammate, as suggested by the narration
    # "Waiting until the defender pressures or a pass angle opens."
    # This resolves the issue where the simulation would stall if the defender didn't apply pressure.
    return (
        Demo0_Pressure_on_Coach.bool(simulation()) or
        Demo0_Pass_angle_to_Coach.bool(simulation())
    )

def λ_precondition_1(scene, sample):
    return Demo0_Pressure_on_Coach.bool(simulation())

def λ_target1():
    return Demo0_MoveTo_space.dist(simulation(), ego=True)

def λ_precondition_2(scene, sample):
    return Demo0_ReceiveBall.bool(simulation())

def λ_precondition_3(scene, sample):
    return Demo0_Close_blocked.bool(simulation())

def λ_precondition_4(scene, sample):
    return (
        Demo0_Teammate_ready.bool(simulation()) and
        Demo0_Coach_shot_angle.bool(simulation())
    )



# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do GetBallPossession(ball)
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