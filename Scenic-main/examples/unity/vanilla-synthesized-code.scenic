from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A0 = HasBallPossession({'player': 'Coach'})
P0 = HasPathToPass({'passer': 'Coach', 'receiver': 'teammate', 'path_width': {'avg': 1.0, 'std': 0.1}})
M0 = MovingTowards({'obj': 'opponent', 'ref': 'teammate'})

def λ_goal(scene, sample):
    # destination is the goal
    goals = findObj('goal', scene.objects)
    if not goals:
        return False
    goal = goals[0]
    dx = sample[0] - goal.position.x
    dy = sample[1] - goal.position.y
    return dx*dx + dy*dy <= 0.25

def λ_safe(scene, sample):
    # destination far from the opponent
    opps = findObj('opponent', scene.objects)
    if not opps:
        return False
    opp = opps[0]
    dx = sample[0] - opp.position.x
    dy = sample[1] - opp.position.y
    return dx*dx + dy*dy >= 25

behavior CoachBehavior():
    # Loop until we shoot
    while True:
        # If we don't have the ball, get possession
        if not A0:
            do Speak("Get possession of the ball")
            take GetBallPossession()
        # If opponent is pressuring the teammate, go shoot
        elif A0 and M0:
            do Speak("Run toward goal to take a shot")
            take MoveTo(λ_goal)
            do Speak("Shoot the ball now")
            take Shoot()
            break
        # If we have a clear pass, pass to teammate
        elif A0 and P0:
            do Speak("Pass the ball to your teammate")
            take Pass("teammate")
            do Speak("Wait for the next play")
            take Wait()
        # Otherwise create space for a safe pass
        else:
            do Speak("Move away from opponent for a clear pass")
            take MoveTo(λ_safe)


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