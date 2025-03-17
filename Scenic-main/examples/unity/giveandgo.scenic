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

behavior opponent1Behavior():
    do Idle() until ego.gameObject.ballPossession
    while True:
        do MoveTo(ball, distance = 4)

behavior TeammateBehavior():
    try:
        do Idle()
    interrupt when (ego.position.y > opponent.position.y):
        print("ego ahead of opponent")
        point = new Point at (0, 0, 11)
        do PassTo(point, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()

behavior GetBall():
    while not self.gameObject.ballPossession:
        take MoveToAction(ball.position)

def GetBehind(player): # similar logic as inzone
    point = new Point behind player by 5
    return point

def teammateHasBallPossession():
    for obj in simulation().objects:
        if isinstance(obj, Player) and obj.team == "blue" and obj.gameObject.ballPossession:
            return True
    return False

behavior ReceiveBall():
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()


behavior CoachBehavior():
    print("coach behavior")
    do GetBall() until hasBallPosession(ego)
    print("getBall")
    do Idle() until movesToward(opponent, ego)
    print("Idle")
    do PassTo(teammate)
    print("pass To teammate")
    do MoveTo(GetBehind(opponent))
    print("get behind opponent")
    do ReceiveBall()
    print("receiveball")


# test = False
# ego = new Human at (0, 0)
ego = new Coach at (0,0),
        with behavior CoachBehavior()

ball = new Ball ahead of ego by 1

opponent = new Player ahead of ego by 5,
                    facing toward ego,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()

goal = new Goal behind opponent by 5, facing away from ego

teammate = new Player offset by (Uniform(-5,5), 7), 
                with name "teammate",
                with team "blue",
                with behavior TeammateBehavior()

terminate when (ego.gameObject.stopButton)