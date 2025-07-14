from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# Define a world model for the soccer domain
model scenic.domains.soccer.model

# Constraint class instantiations are used within the lambda functions.
# These classes are assumed to be available from the environment.

# λ_target function for the MoveTo action
def λ_target_move_position(pos):
    """
    Defines the strategic position(s) the Coach aims to move to.
    The coach will choose one of these pre-defined favorable positions.
    """
    strategic_positions = [Point(5.0, 11.0), Point(-5.0, 8.0), Point(4.0, 9.0)]
    return pos == select(strategic_positions)

# λ_precondition functions, which evaluate the current scene state
def λ_precondition_move_to_pos(scene, sample):
    """
    Precondition for Coach to initiate a move:
    The Coach does not have ball possession, and a teammate currently possesses the ball.
    """
    return not HasBallPossession(args={'player': 'Coach'})(scene, sample) and \
           HasBallPossession(args={'player': 'teammate'})(scene, sample)

def λ_precondition_get_ball(scene, sample):
    """
    Precondition for Coach to attempt getting ball possession:
    The Coach is physically close to the ball, and does not already possess it.
    """
    return CloseTo(args={'obj': 'Coach', 'ref': 'ball', 'max': 1.0})(scene, sample) and \
           not HasBallPossession(args={'player': 'Coach'})(scene, sample)

def λ_precondition_shoot(scene, sample):
    """
    Precondition for Coach to shoot:
    The Coach has ball possession, and there is a clear, unobstructed path to the goal.
    """
    return HasBallPossession(args={'player': 'Coach'})(scene, sample) and \
           HasPathToPass(passer='Coach', receiver='goal', path_width={'avg': 1.5, 'std': 0.5})(scene, sample)

def λ_precondition_pass(scene, sample):
    """
    Precondition for Coach to pass:
    The Coach has ball possession, and there is a clear, unobstructed path to the teammate.
    """
    return HasBallPossession(args={'player': 'Coach'})(scene, sample) and \
           HasPathToPass(passer='Coach', receiver='teammate', path_width={'avg': 1.0, 'std': 0.2})(scene, sample)

def λ_precondition_wait_for_opening(scene, sample):
    """
    Precondition for Coach to wait:
    This condition is true if the teammate is currently marked by an opponent,
    or if an opponent is actively moving towards and pressing the Coach.
    """
    teammate_marked = CloseTo(args={'obj': 'teammate', 'ref': 'opponent', 'max': 3})(scene, sample)
    opponent_pressing_coach = MovingTowards(args={'obj': 'opponent', 'ref': 'Coach'})(scene, sample)
    return teammate_marked or opponent_pressing_coach

# λ_termination function for the overall scenario
def λ_termination_overall_behavior(scene, sample):
    """
    Defines when the entire scenario should terminate.
    The scenario ends once the ball is detected very close to the goal,
    indicating a shot has been attempted or scored.
    """
    return CloseTo(args={'obj': 'ball', 'ref': 'goal', 'max': 1.5})(scene, sample) or \
           CloseTo(args={'obj': 'ball', 'ref': 'goal_leftpost', 'max': 1.5})(scene, sample) or \
           CloseTo(args={'obj': 'ball', 'ref': 'goal_rightpost', 'max': 1.5})(scene, sample)

# Behavior definition for the Coach
behavior CoachBehavior():
    # Phase 1: Coach moves to a strategic position on the field.
    # This movement aims to create space or an angle for a future pass or shot.
    do Speak("Alright team, I'm moving into position to create an optimal angle for attack!")
    do MoveTo(λ_target_move_position)

    # Phase 2: Coach gets possession of the ball once in the chosen strategic position.
    # This assumes a teammate will pass the ball to the Coach or it becomes available.
    do Speak("Excellent, I'm in position! Now, I will get control of the ball!")
    do GetBallPossession()

    # Phase 3: Coach decides whether to shoot or pass based on the immediate situation.
    do Speak("Decision time! Assessing if I have an open shot or if passing is the better option.")
    if HasPathToPass(passer='Coach', receiver='goal', path_width={'avg': 1.5, 'std': 0.5})(self.scene, None):
        # If there's a clear, unobstructed path to the goal, the Coach takes the shot.
        do Speak("Yes! I have a clear path to the goal! Time to shoot and score!")
        do Shoot()
    else:
        # If there is no clear shot, the Coach considers waiting or passing.
        do Speak("No direct shot. I need to find a teammate or wait for an opening.")
        while λ_precondition_wait_for_opening(self.scene, None):
            # If the teammate is currently marked by an opponent or an opponent is pressing the Coach,
            # the Coach waits for a better opportunity to emerge.
            do Speak("Hold on, teammate is covered or opponent is pressing. Waiting for an opening!")
            do Wait() # Wait for one simulation step, re-evaluating the condition

        # Once conditions are favorable (teammate is open, or immediate pressure is relieved),
        # the Coach passes the ball to the teammate.
        do Speak("Perfect, my teammate is open! Passing the ball now for a scoring opportunity!")
        do Pass(teammate)

# Define objects used in the scenario.
# (Per prompt instructions, do not declare them explicitly with "new",
# assume they are already defined globally by the simulation environment.)
# Example setup if declarations were allowed:
# ball = new Ball
# Coach = new Coach with behavior CoachBehavior()
# teammate = new Teammate
# opponent = new Opponent
# goal = new Goal
# goal_leftpost = new Goal
# goal_rightpost = new Goal

# Overall scenario termination condition.
# This statement defines when the entire simulation run should stop.
terminate when λ_termination_overall_behavior(scene, sample)


def movesToward(player1, player2):
    dist1 = distance from player1.prevPosition to player2.prevPosition
    dist2 = distance from player1.position to player2.position
    return dist2 < dist1

behavior Follow(obj):
    while True:
        do MoveToBehavior(obj, distance = 3, status = f"Follow {obj.name}")

def pressure(player1, player2):
    """
    Returns True if player1 is pressuring player2, False otherwise.
    """
    behav = player1.gameObject.behavior.lower()
    name = player2.name.lower()
    print(f"player1: {player1.name}, player2: {player2.name}, behavior: {behav}")
    if 'follow' in behav and name in behav:
        return True
    return False

behavior opponent1Behavior():
    do Idle() until teammate.gameObject.ballPossession
    do Follow(ball) until ego.gameObject.ballPossession
    # do Uniform(Follow(ego), Follow(teammate))
    do Follow(teammate)
    # print("opponent follows ego")
    # do Follow(ego)

A = HasPath({'obj1': 'teammate', 'obj2': 'coach', 'path_width':{'avg': 2, 'std':1}})

behavior TeammateBehavior():
    passed = False
    try:
        do GetBallPossession(ball)
        do Idle()
    interrupt when (A.bool(simulation()) and not passed and self.gameObject.ballPossession):
        do Idle() for 2.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point) until MakePass({'player': 'coach'}).bool(simulation())
        do Idle() for 0.5 seconds
        do GetBallPossession(ball)
        do Shoot(goal)
        passed = True

def teammateHasBallPossession():
    for obj in simulation().objects:
        if isinstance(obj, Player) and obj.team == "blue" and obj.gameObject.ballPossession:
            return True
    return False

behavior GetBehindAndReceiveBall(player, zone): # similar logic as inzone
    do MoveToBehavior(point) until self.position.y > player.position.y + 2
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()

behavior ReceiveBall():
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()
 

teammate = new Player at (0,0), 
                with name "teammate",
                with team "blue",
                with behavior TeammateBehavior()

ball = new Ball ahead of teammate by 1

opponent = new Player ahead of teammate by 5,
                    facing toward teammate,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()

ego = new Coach behind opponent by 5, 
            facing toward teammate,
            with name "Coach",
            with team "blue",
            with behavior CoachBehavior()

goal = new Goal behind opponent by 10, facing away from ego

terminate when (ego.gameObject.stopButton)