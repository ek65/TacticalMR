from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    # λ_precondition: Must have possession before making a pass or shooting
    λ_precondition = lambda scene: HasBallPossession({'player': 'Coach'})(scene, None)

    # λ_termination: Terminate after shooting at the open goal
    λ_termination = lambda scene: False

    # Helper lambda for creating an angle for a pass (to side of ball/opponent/teammate)
    def λ_create_passing_angle(scene):
        # Move to the right or left such that from Coach's new position, angle to teammate is maximized,
        # and there’s a viable passing path clear of the opponent, with line of pass to teammate and coach not in same line as opponent/ball
        return (
            not HasBallPossession({'player': 'Coach'})(scene, None)
        )

    # Helper: Teammate has ball
    def λ_teammate_has_ball(scene):
        return HasBallPossession({'player': 'teammate'})(scene, None)

    # Helper: Coach has ball
    def λ_coach_has_ball(scene):
        return HasBallPossession({'player': 'Coach'})(scene, None)

    # Helper: Path from Coach to goal is clear
    def λ_open_goal(scene):
        return HasPathToPass({'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 1.5, 'std': 0.2}})(scene, None)

    # Helper: Path from Coach to teammate is clear
    def λ_open_pass_teammate(scene):
        return HasPathToPass({'passer': 'Coach', 'receiver': 'teammate', 'path_width': {'avg': 1.0, 'std': 0.2}})(scene, None)

    # Helper: Teammate in attacking position
    def λ_teammate_open_for_shot(scene):
        # Teammate has path to goal
        return HasPathToPass({'passer': 'teammate', 'receiver': 'goal', 'path_width': {'avg': 1.2, 'std': 0.2}})(scene, None)

    # COACH BEHAVIOR SEQUENCE
    while True:
        # 1. Move to an open space/side to create a good passing angle if teammate has ball.
        if λ_teammate_has_ball(scene):
            do Speak("Move to side to create a good angle for pass.")
            do MoveTo(λ_create_passing_angle)
            do Wait()
        
        # 2. Wait for pass from teammate and get the ball
        do Speak("Wait for the ball from your teammate.")
        do Wait()
        if not λ_coach_has_ball(scene):
            do Speak("Get possession of the ball after receiving pass.")
            do GetBallPossession()
        
        # 3. Decision - if open goal, shoot; else, pass to teammate
        if λ_coach_has_ball(scene):
            if λ_open_goal(scene):
                do Speak("Now I have an open goal, will shoot!")
                do Shoot()
                break
            elif λ_open_pass_teammate(scene):
                do Speak("Passing ball to my teammate for a better shot.")
                do Pass("teammate")
                do Wait()
            else:
                # Otherwise, wait to reassess
                do Speak("No good pass or shot, will wait for opportunity.")
                do Wait()
                continue

        # 4. After passing, if teammate has ball near goal, encourage the teammate to shoot
        if λ_teammate_has_ball(scene) and λ_teammate_open_for_shot(scene):
            do Speak("Teammate, shoot at the open goal!")
            do Wait()

    # Required function definitions for Scenic (returned only in code; not executed inline)
    λ_target = λ_create_passing_angle
    λ_precondition = λ_precondition
    λ_termination = λ_termination


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