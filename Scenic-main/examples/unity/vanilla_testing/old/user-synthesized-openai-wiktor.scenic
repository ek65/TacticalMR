from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

param λ_target_coach_open_right = {
    return HasPathToPass({
        "passer": "teammate",
        "receiver": "Coach",
        "path_width": {"avg": 1.5, "std": 0.2}
    })(scene, sample) and \
    HorizontalRelation({
        "obj": "Coach",
        "ref": "teammate",
        "relation": "right",
        "horizontal_threshold": {"avg": 1.5, "std": 0.2}
    })(scene, sample)
}

param λ_target_coach_open_left = {
    return HasPathToPass({
        "passer": "teammate",
        "receiver": "Coach",
        "path_width": {"avg": 1.5, "std": 0.2}
    })(scene, sample) and \
    HorizontalRelation({
        "obj": "Coach",
        "ref": "teammate",
        "relation": "left",
        "horizontal_threshold": {"avg": 1.5, "std": 0.2}
    })(scene, sample)
}

param λ_target_wait_receive = {
    return HasBallPossession({"player": "teammate"})(scene, sample) and \
           HasPathToPass({
                "passer": "teammate",
                "receiver": "Coach",
                "path_width": {"avg": 1.5, "std": 0.2}
           })(scene, sample)
}

param λ_target_goal = {
    # "Coach" is close enough to goal to shoot (in advanced position).
    return CloseTo({"obj": "Coach", "ref": "goal", "max": 2.5})(scene, sample)
}

param λ_termination = {
    # Terminate if Coach or teammate scores/pass completes, or if ball out of play
    return HasBallPossession({"player": "goal"})(scene, sample)
        or HasBallPossession({"player": "teammate"})(scene, sample)
        or HasBallPossession({"player": "Coach"})(scene, sample)
}

param λ_precondition_gain_possession = {
    # Only run GetBallPossession if Coach does not have the ball
    return not HasBallPossession({"player": "Coach"})(scene, sample)
}

param λ_precondition_pass_to_coach = {
    # teammate must have ball
    return HasBallPossession({"player": "teammate"})(scene, sample)
}

param λ_precondition_coach_has_ball = {
    return HasBallPossession({"player": "Coach"})(scene, sample)
}

param λ_precondition_pass_to_teammate = {
    return HasBallPossession({"player": "Coach"})(scene, sample)
}

param λ_precondition_go_for_goal = {
    # Coach has ball and path to goal open
    return HasBallPossession({"player": "Coach"})(scene, sample)
}

behavior CoachBehavior():
    while True:
        if HasBallPossession({"player": "teammate"})(scene, sample):
            do Speak("Teammate has ball. Let's get open for a pass.")
            if HorizontalRelation({
                    "obj": "Coach",
                    "ref": "teammate",
                    "relation": "right",
                    "horizontal_threshold": {"avg": 1.5, "std": 0.2}
                })(scene, sample) == False:
                do Speak("Moving right to create passing angle.")
                do MoveTo(λ_target_coach_open_right)
            elif HorizontalRelation({
                    "obj": "Coach",
                    "ref": "teammate",
                    "relation": "left",
                    "horizontal_threshold": {"avg": 1.5, "std": 0.2}
                })(scene, sample) == False:
                do Speak("Moving left to create passing angle.")
                do MoveTo(λ_target_coach_open_left)
            do Speak("Waiting for teammate to pass.")
            do Wait()
        elif HasBallPossession({"player": "Coach"})(scene, sample):
            # Decision: Pass to teammate or go for goal based on opponent position
            if CloseTo({"obj": "opponent", "ref": "Coach", "max": 2.0})(scene, sample):
                do Speak("Opponent is pressuring me, pass to teammate!")
                do Pass("teammate")
            elif CloseTo({"obj": "opponent", "ref": "teammate", "max": 2.0})(scene, sample):
                do Speak("Opponent is staying with teammate, I'm going for goal!")
                do MoveTo(λ_target_goal)
                do Speak("Now, shoot to score!")
                do Shoot()
            else:
                do Speak("Evaluating options: pass or shoot.")
                do Wait()
        else:
            do Speak("Ready for ball, moving to get possession if it's free.")
            do GetBallPossession()
        # Check for termination
        if λ_termination(scene, sample):
            break



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