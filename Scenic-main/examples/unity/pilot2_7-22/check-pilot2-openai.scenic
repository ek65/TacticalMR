from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1_distance_close = DistanceTo({
    'from': 'Coach',
    'to': 'teammate',
    'min': None,
    'max': {'avg': 2.2, 'std': 0.3},
    'operator': 'less_than'
})

A2_pressure = Pressure({
    'player1': 'opponent',
    'player2': 'teammate'
})

A3_distance_opponent = DistanceTo({
    'from': 'opponent',
    'to': 'Coach',
    'min': {'avg': 1.5, 'std': 0.2},
    'max': None,
    'operator': 'greater_than'
})

A4_moving_away_from_opponent = DistanceTo({
    'from': 'Coach',
    'to': 'opponent',
    'min': {'avg': 1.3, 'std': 0.3},
    'max': None,
    'operator': 'greater_than'
})

A5_make_pass = MakePass({'player': 'teammate'})

A6_has_possession = HasBallPossession({'player': 'Coach'})

A7_defender_very_close = DistanceTo({
    'from': 'opponent',
    'to': 'Coach',
    'min': None,
    'max': {'avg': 1.2, 'std': 0.25},
    'operator': 'less_than'
})

A8_clear_path_shot = HasPath({
    'obj1': 'Coach',
    'obj2': 'goal',
    'path_width': {'avg': 2.2, 'std': 0.25}
})

A9_clear_path_pass = HasPath({
    'obj1': 'Coach',
    'obj2': 'teammate',
    'path_width': {'avg': 2.2, 'std': 0.25}
})

def λ_target_close():
    # Move closer to teammate while away from opponent
    cond = A1_distance_close and A4_moving_away_from_opponent
    return cond.dist(simulation(), ego=True)

def λ_target_space_opposite():
    # Move to space opposite from where the defender went (simulate +/-2 meters laterally)
    return A4_moving_away_from_opponent.dist(simulation(), ego=True)

def λ_precondition_receive_pass(scene, sample):
    # Triggered when teammate makes a pass
    return A5_make_pass.bool(simulation())

def λ_precondition_receive_ball(scene, sample):
    return A6_has_possession.bool(simulation())

def λ_termination_move_close(scene, sample):
    # Terminates when distance close enough to teammate, but doesn't denote optimal spacing
    return A1_distance_close.bool(simulation())

def λ_precondition_pass_back(scene, sample):
    # Defender is very close and Coach has possession
    return A7_defender_very_close.bool(simulation()) and A6_has_possession.bool(simulation())

def λ_termination_receive_ball(scene, sample):
    # Terminate once Coach receives ball (not scoring/turn)
    return A6_has_possession.bool(simulation())

def λ_precondition_shoot(scene, sample):
    # Defender not too close, clear shot path, and possession
    return (not A7_defender_very_close.bool(simulation())) and A8_clear_path_shot.bool(simulation()) and A6_has_possession.bool(simulation())

def λ_precondition_can_pass(scene, sample):
    return A9_clear_path_pass.bool(simulation()) and A6_has_possession.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Step closer to teammate to create space from defender")
    do MoveTo(λ_target_close())
    do Speak("Wait for teammate to pass; be ready for ball")
    do Idle() until λ_precondition_receive_pass(simulation(), None)
    do Speak("Move into space away from defender to receive pass")
    do MoveTo(λ_target_space_opposite())
    do Speak("Wait to receive possession of the ball")
    do Idle() until λ_precondition_receive_ball(simulation(), None)
    if λ_precondition_pass_back(simulation(), None):
        do Speak("Defender is too close, pass the ball back to teammate")
        do Pass(teammate)
    elif λ_precondition_shoot(simulation(), None):
        do Speak("Turn and try to shoot towards goal")
        do Shoot(goal)
    elif λ_precondition_can_pass(simulation(), None):
        do Speak("Pass to teammate if a pass lane is available")
        do Pass(teammate)
    else:
        do Speak("Stop, prepare to receive or shield ball")
        do StopAndReceiveBall()
    do Speak("Idle, play sequence complete")
    do Idle()
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