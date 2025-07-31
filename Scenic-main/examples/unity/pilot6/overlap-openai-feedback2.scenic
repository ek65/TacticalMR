from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_behind = DistanceTo({
    'from': 'Coach',
    'to': 'teammate',
    'min': {'avg': 3.5, 'std': 0.2},
    'max': {'avg': 5.5, 'std': 0.2},
    'operator': 'within'
})

# ADDED: New constraint based on feedback to ensure a clear passing lane from the teammate,
# avoiding the opponent's obstruction.
A1_has_path_from_teammate = HasPath({
    'obj1': 'teammate',
    'obj2': 'Coach',
    'path_width': {'avg': 2.0, 'std': 0.5}
})

# ADDED: New constraint based on feedback to move to a more advantageous position
# closer to the goal (i.e., 'above' the opponent in the y-axis).
A1_is_in_front_of_opponent = HeightRelation({
    'obj': 'Coach',
    'relation': 'above',
    'ref': 'opponent',
    'height_threshold': {'avg': 1.0, 'std': 0.5}
})

# FIX: Added a new constraint to ensure the coach moves to a spot sufficiently
# far from the opponent. The previous logic only considered having a clear
# path initially, but didn't prevent the opponent from quickly closing down
# and blocking the pass. This addresses the core issue in the feedback video.
A1_away_from_opponent = DistanceTo({
    'from': 'Coach',
    'to': 'opponent',
    'min': {'avg': 4.0, 'std': 1.0},
    'max': None,
    'operator': 'greater_than'
})

A1precondition_receive1 = MakePass({'player': 'teammate'})
A1haspossession_Coach = HasBallPossession({'player': 'Coach'})

A1target_passback = DistanceTo({
    'from': 'teammate',
    'to': 'Coach',
    'min': {'avg': 2.5, 'std': 0.2},
    'max': {'avg': 4.5, 'std': 0.3},
    'operator': 'within'
})

A1precondition_receive2 = MakePass({'player': 'Coach'})

A1target_infront_space = DistanceTo({
    'from': 'Coach',
    'to': 'teammate',
    'min': {'avg': 3.0, 'std': 0.3},
    'max': {'avg': 7.0, 'std': 0.3},
    'operator': 'within'
})

A1precondition_receive3 = MakePass({'player': 'teammate'})
A1haspossession2_Coach = HasBallPossession({'player': 'Coach'})

A1target_turn_shoot = DistanceTo({
    'from': 'goal',
    'to': 'Coach',
    'min': None,
    'max': {'avg': 14.0, 'std': 0.2},
    'operator': 'less_than'
})

# ADDED: New constraints and lambda function for conditional shooting based on coach feedback.
# This checks if the opponent is pressuring the coach.
A1_no_pressure = Pressure({'player1': 'opponent', 'player2': 'Coach'})

# This checks for a clear path to the goal. A path_width of 2.0m with 0.5m std is a reasonable assumption.
A1_path_to_goal = HasPath({
    'obj1': 'Coach',
    'obj2': 'goal',
    'path_width': {'avg': 2.0, 'std': 0.5}
})

def λ_target_behind():
    return A1target_behind.dist(simulation(), ego=True)

# ADDED: This lambda function combines multiple criteria to find an intelligent
# receiving position, as per the coach's feedback.
def λ_smart_receive_space():
    # The destination must satisfy multiple conditions:
    # 1. Be at a reasonable distance from the teammate to receive a pass.
    # 2. Have a clear path from the teammate, free of opponents.
    # 3. Be in a better attacking position, further upfield than the opponent.
    # FIX: Added the A1_away_from_opponent constraint to ensure the chosen space is not
    # easily reachable by the opponent, creating a safer passing option.
    smart_spot = (A1target_behind and
                  A1_has_path_from_teammate and
                  A1_is_in_front_of_opponent and
                  A1_away_from_opponent)
    return smart_spot.dist(simulation(), ego=True)

def λ_precondition_receive1():
    return A1precondition_receive1.bool(simulation())

def λ_haspossession_Coach():
    return A1haspossession_Coach.bool(simulation())

def λ_target_passback():
    return A1target_passback.dist(simulation(), ego=True)

def λ_precondition_receive2():
    return A1precondition_receive2.bool(simulation())

def λ_target_infront_space():
    return A1target_infront_space.dist(simulation(), ego=True)

def λ_precondition_receive3():
    return A1precondition_receive3.bool(simulation())

def λ_haspossession2_Coach():
    return A1haspossession2_Coach.bool(simulation())

def λ_target_turn_shoot():
    return A1target_turn_shoot.dist(simulation(), ego=True)

def λ_can_shoot_early():
    # This function combines the conditions for an early shot opportunity:
    # no pressure from the opponent and a clear path to the goal.
    return (not A1_no_pressure.bool(simulation())) and A1_path_to_goal.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds

    # FIX: The original 'MoveTo' was too simple and led the coach into an obstructed path.
    # The logic is updated to find a space that has a clear passing lane from the teammate
    # and is in a more advanced position, as per the coach's feedback.
    do Speak("Move to an open, advanced space with a clear pass from my teammate.")
    do MoveTo(λ_smart_receive_space(), True)

    do Speak("Wait to receive a pass from teammate before moving to ball")
    do Idle() until λ_precondition_receive1()

    # FIX 1: Per feedback, the coach doesn't always need to move to the ball.
    # Changed MoveToBallAndGetPossession() to StopAndReceiveBall() for a more passive reception.
    do Speak("Receive the ball from teammate")
    do StopAndReceiveBall()

    do Speak("Wait until you have ball possession before acting")
    do Idle() until λ_haspossession_Coach()

    # FIX 2: Added a conditional branch based on coach feedback. The coach can now
    # shoot directly if the opportunity arises (no pressure, clear path),
    # instead of always passing back to the teammate.
    if λ_can_shoot_early():
        do Speak("Opportunity to shoot directly, taking the shot")
        do Shoot('goal')
    else:
        # This is the original logic path for when an early shot is not possible.
        do Speak("No clear shot, passing back to teammate at distance 4 meters")
        do Pass('teammate')

        do Speak("Wait to receive a return pass from teammate")
        do Idle() until λ_precondition_receive2()

        do Speak("Move to ball and get possession from teammate's return pass")
        do MoveToBallAndGetPossession()  # Kept, as coach needs to retrieve the pass-back.

        do Speak("Move to open space 5 meters in front of teammate after receiving ball")
        do MoveTo(λ_target_infront_space(), False)

        do Speak("Wait to receive the ball in advanced position")
        do Idle() until λ_precondition_receive3()

        # FIX 3: As per feedback, the coach is already in an open position
        # and can just receive the ball. Changed MoveToBallAndGetPossession() to StopAndReceiveBall().
        do Speak("Receive the ball for final attack")
        do StopAndReceiveBall()

        # Corrected logic to wait for both possession and being in shooting range,
        # which was implied but not fully implemented in the original code.
        do Speak("Wait for possession and to be in shooting range (< 14m from goal)")
        do Idle() until λ_haspossession2_Coach() and λ_target_turn_shoot()

        do Speak("Shoot at the goal now")
        do Shoot('goal')

    do Idle()

####Environment Behavior START####

opponent_y_distance = Uniform(3, 5)
opponent_x_distance = Uniform(-2, 2)
ego_x_distance = Uniform(-2, 2)
ego_y_distance = Uniform(-1, -2)

# Ensure teammate and opponent are on the same side
#require (opponent_x_distance < 0 and ego_x_distance < 0) or (opponent_x_distance >= 0 and ego_x_distance >= 0)

behavior Follow(obj):
    while ego.position.y > 1:
        do MoveToBehavior(obj, distance = 2, status = f"Follow {obj.name}")

behavior TeammateBehavior():
    # Double checking gotBall to ensure the pass is triggered correctly
    # since MoveToBallAndGetPossession() might get interrupted
    gotBall = False
    try:
        do Idle() for 1 seconds
        do MoveToBallAndGetPossession()
        gotBall = True
        do Idle()
    interrupt when ego.triggerPass and self.gameObject.ballPossession and gotBall:
        ego.triggerPass = False
        do Idle() for 1 seconds
        do Pass(ego.xMark)
        do Idle() for 1 seconds
        if self.gameObject.ballPossession:
            do Idle() until (distance from opponent to ego) <= 3
            do DribbleTo(goal) until (distance from opponent to ego) > 3
    
    do Idle()
    

behavior DefenderBehavior():
    do Idle() for 1 seconds
    do Idle() until ego.gameObject.ballPossession
    while True:
        if distance from self to ego > 3.5:
            do MoveToBehavior(ego.position, distance=3.5)
        else:
            do Idle() for 0.1 seconds   
    

teammate = new Player at (0, 0, 0),
      with behavior TeammateBehavior(), with name "teammate", with team "blue"

ball = new Ball ahead of teammate by 1

ego = new Coach at (ego_x_distance, ego_y_distance, 0),
    with name "Coach",
    with team "blue",
    with behavior CoachBehavior(),
    with xMark Vector(0, 0, 0),  # Set initial xMark position
    with triggerPass False  # Initialize triggerPass to False

opponent = new Player at (0, Uniform(4, 6), 0), with name "opponent",
            with behavior DefenderBehavior(), with team "red"

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)