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
        do MoveTo(ball, distance = 3)

behavior TeammateBehavior():
    passed = False
    passed2 = True
    try:
        do GetBall()
        do Idle() for 1 seconds
        do PassTo(ego, slow=False)
        passed = True
        do Idle()
    interrupt when (self.gameObject.ballPossession and passed):
        do Idle() for 1 seconds
        if (self.position.x - ego.position.x) > 0:
            point = new Point at (-5,10,0)
            do PassTo(point, slow=False)
        else:
            point = new Point at (5,10,0)
            do PassTo(point, slow=False)

behavior GetBall():
    while not self.gameObject.ballPossession:
        take MoveToAction(ball.position)

def teammateHasBallPossession():
    for obj in simulation().objects:
        if isinstance(obj, Player) and obj.team == "blue" and obj.gameObject.ballPossession:
            return True
    return False

behavior GetBehindAndReceiveBall(player, zone): # similar logic as inzone
    
    do MoveTo(point) until self.position.y > player.position.y + 2
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()

behavior ReceiveBall():
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()

behavior CoachBehavior():
    do GetBall() until hasBallPosession(ego)
    do Idle() until movesToward(opponent, ego)
    do PassTo(teammate)
    # do GetBehindAndReceiveBall(opponent)
    do MoveToAndReceiveBall(InZone(['A1','A2','A3']))

# ego = new Human at (0,0)
# ego = new Coach at (0,0),
#         with behavior CoachBehavior()


teammate = new Player at (0,0), 
                with name "teammate",
                with team "blue",
                with behavior TeammateBehavior()

ball = new Ball ahead of teammate by 1

ego = new Human ahead of teammate by 4, 
            facing toward teammate,
            with name "Coach",
            with team "blue"

opponent = new Player behind ego by 5,
                    facing toward teammate,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()



goal = new Goal behind opponent by 10, facing away from ego

terminate when (ego.gameObject.stopButton)