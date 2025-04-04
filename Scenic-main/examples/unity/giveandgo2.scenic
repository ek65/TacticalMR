from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

def movesToward(player1, player2):
    dist1 = distance from player1.prevPosition to player2.prevPosition
    dist2 = distance from player1.position to player2.position
    return dist2 < dist1

behavior Follow(obj):
    while True:
        do MoveToBehavior(obj, distance = 2, status = f"Follow {obj.name}")

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
    # do Follow(teammate)
    print("opponent follows ego")
    do Follow(ego)

A = HasPathToPass({'passer': 'teammate', 'receiver': 'coach', 'path_width':{'avg': 5, 'std':1}})

behavior TeammateBehavior():
    passed = False
    try:
        do GetBall()
        do Idle()
    interrupt when (A(simulation(), None) and not passed and self.gameObject.ballPossession):
        do Idle() for 2.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point)
        passed = True

behavior GetBall():
    while not self.gameObject.ballPossession:
        take MoveToAction(ball.position)

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

behavior CoachBehavior():
    point = new Point left of teammate by Uniform(-4, 4)
    do MoveToBehavior(point.position, distance=0.5)
    do Idle() until self.gameObject.ballPossession
    do Idle() for 1.5 seconds
    do Idle() until pressure(opponent, ego) or pressure(opponent, teammate)
    print("pressure detected")
    if pressure(opponent, ego):
        do Pass(teammate)
    else:
        point = new Point at (Range(-5, 5), Range(13, 17), 0)
        do MoveToBehavior(point, distance=0.5)
        do Pass(goal)

    # do GetBehindAndReceiveBall(opponent)
    # do MoveToAndReceiveBall(InZone(['A1','A2','A3']))

behavior IdleSpecial():
    while True:
        take IdleAction()
        # print(f"teammate: {teammate.gameObject.behavior}")
        print(f"opponent: {opponent.gameObject.behavior}")
        print(f"ego: {ego.name}")
        # print(f"opp.x: {self.position.x}, opp.y: {self.position.y}, opp.z: {self.position.z}")

teammate2 = new Player at (10,0), 
                with name "teammate2",
                with team "blue",
                with behavior IdleSpecial()

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