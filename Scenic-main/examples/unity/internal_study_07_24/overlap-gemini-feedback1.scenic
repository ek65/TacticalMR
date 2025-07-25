from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####
A1_target_overlap = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 54.0, 'std': 3.0},
    'dist': {'avg': 5.5, 'std': 0.5}
})

A1_term_pass_from_teammate = MakePass({'player': 'teammate'})

A1_precond_pressure = Pressure({'player1': 'opponent', 'player2': 'Coach'})

A2_precond_clear_shot = HasPath({
    'obj1': 'Coach',
    'obj2': 'goal',
    'path_width': {'avg': 3.0, 'std': 0.5}
})

A3_precond_clear_pass = HasPath({
    'obj1': 'Coach',
    'obj2': 'teammate',
    'path_width': {'avg': 3.0, 'std': 0.5}
})

A4_precond_coach_has_ball = HasBallPossession({'player': 'Coach'})


def λ_target0():
    return A1_target_overlap.dist(simulation(), ego=True)


def λ_termination0():
    return A1_term_pass_from_teammate.bool(simulation())


def λ_precondition_ball_received():
    return A4_precond_coach_has_ball.bool(simulation())


def λ_precondition_pass_back():
    return (
        A1_precond_pressure.bool(simulation()) and
        A3_precond_clear_pass.bool(simulation())
    )


def λ_precondition_shoot_goal():
    return (
        not A1_precond_pressure.bool(simulation())
        and A2_precond_clear_shot.bool(simulation())
    )


behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak(
        "My teammate is blocked by a defender. "
        "I need to overlap to create a passing option for him."
    )

    # CHANGE: The termination condition for MoveTo was updated based on feedback.
    # The coach was stopping as soon as the teammate passed the ball.
    # The fix makes the coach continue moving until it actually receives the ball.
    # The 'until' condition was changed from λ_termination0() to λ_precondition_ball_received().
    do MoveTo(λ_target0(), True) until λ_precondition_ball_received()

    # CHANGE: The following lines were removed as they are now obsolete.
    # The original logic was to move, then stop, then wait for the ball.
    # The corrected logic handles moving until the ball is received within the MoveTo action itself.
    # REMOVED: do Speak("He's making the pass. I'll stop and receive the ball now.")
    # REMOVED: do StopAndReceiveBall()
    # REMOVED: do Idle() until λ_precondition_ball_received()

    do Speak(
        "I have the ball. Now I'll decide my next move based on the opponent's position."
    )

    if λ_precondition_pass_back():
        do Speak(
            "The opponent is closing in on me, but my teammate is open. "
            "I'll pass it back to him."
        )
        do Pass(teammate)
    elif λ_precondition_shoot_goal():
        do Speak(
            "The opponent isn't too close and I have a clear shot at the goal. "
            "I'm going to take it."
        )
        do Shoot(goal)
    else:
        do Speak(
            "I don't have a clear pass or shot, so I'll hold my position for now."
        )
        do Idle()
    do Idle()
####Environment Behavior START####


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
    try:
        do Idle() for 1 seconds
        do MoveToBallAndGetPossession()
        do Idle()
    interrupt when ego.triggerPass and self.gameObject.ballPossession:
        ego.triggerPass = False
        do Idle() until ego.position.y > 2
        do Pass(ego.xMark)
        do Idle() until (distance from opponent to ego) <= 3
        do DribbleTo(goal) until (distance from opponent to ego) > 3
    
    do Idle()
    

behavior DefenderBehavior():
    do Idle() for 1 seconds
    do Idle() until ego.position.y > 1
    while True:
        if distance from self to ego > 2.0:
            do MoveToBehavior(ego.position, distance=2.0)
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