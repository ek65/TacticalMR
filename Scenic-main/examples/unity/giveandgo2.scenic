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
        do MoveToBehavior(obj, distance = 2)

behavior opponent1Behavior():
    do Idle() until teammate.gameObject.ballPossession
    do Follow(ball) until ego.gameObject.ballPossession
    do Uniform(Follow(ball), Follow(teammate))
    # do Follow(teammate)
    # do Follow(ball)
    

behavior TeammateBehavior():
    passed = False
    try:
        do GetBall()
        do Idle()
    interrupt when (abs(self.x - ego.x) > 3 and not passed):
        do Idle() for 1.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point)
        passed = True
        do Idle() until self.gameObject.ballPossession
        do Pass(goal)

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
    do GetBall() until hasBallPosession(ego)
    do Idle() until movesToward(opponent, ego)
    do Pass(teammate)
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

opponent = new Player ahead of teammate by 5,
                    facing toward teammate,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()

ego = new Human behind opponent by 5, 
            facing toward teammate,
            with name "Coach",
            with team "blue"

goal = new Goal behind opponent by 10, facing away from ego

terminate when (ego.gameObject.stopButton)