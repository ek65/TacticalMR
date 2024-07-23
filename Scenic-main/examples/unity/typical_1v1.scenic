from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
from scenic.core.regions import MeshVolumeRegion
import trimesh
import random

def isOpponentFacingWithBall() -> bool:
    opponent = closest(Player, coach)  # Assuming 'coach' is the reference object to find the closest player
    if opponent and hasBallPosession(opponent):
        return isFacingAt(opponent, coach)
    return False
def sprintAndGetSet():
    player = Player()
    target = opponent
    target_coordinate = Coordinate(CoordinateInit.RELATIVE, [target])
    speed = Speed(SpeedInit.MAGNITUDE)
    
    # Sprint towards the opponent
    moveTo(player, target_coordinate, MovingStyle.SPRINT, speed)
    
    # Determine the angle based on foot preference
    foot_preference = footed(player)
    angle = 15 if foot_preference == 'right' else -15
    
    # Face towards the opponent with a slight angle
    faceTowards(player, target, angle)

def isNextToOpponent() -> bool:
    opponent = closest(Object, player, max=1.0)  # Assuming 1.0 is the distance threshold for being 'next to'
    return opponent is not None
def intercept_the_ball():
    if hasBallPosession(opponent):
        tackle(player, opponent)

def opponentTriesToTakeBallAway() -> bool:
    player = closest(Player, ball)
    opponent = closest(Player, player)
    return opponent is not None and not hasBallPosession(opponent) and isFacingAt(opponent, ball)
def headTowardsGoal():
    player = Player()
    goal = Object()
    target_coordinate = Coordinate(CoordinateInit.RELATIVE, [goal])
    speed = Speed(SpeedInit.MAGNITUDE)
    moveTo(player, target_coordinate, MovingStyle.RUN, speed)


behavior coachBehavior():
    try:
        do Idle()
    interrupt when isOpponentFacingWithBall():
        do sprintAndGetSet()
    interrupt when isNextToOpponent():
        do intercept_the_ball()
    interrupt when opponentTriesToTakeBallAway():
        do headTowardsGoal()

ego = new Player at (0, 0, 0), with behavior coachBehavior()


footed = DiscreteRange(-1, 1)

pressingDistance = 2.5 #Uniform(4, 5)
shootingDistance = Uniform (4, 8)

behavior opponentBehavior():
    try:
        do InterceptBall(ball)
    interrupt when self.gameObject.ballPossession:
        do SetPlayerSpeed(5.0)
        do MoveTo(goal.position) for 0.1 seconds
    interrupt when self.gameObject.ballPossession and distance from self to ego < pressingDistance:
        do SetPlayerSpeed(10.0)
        if abs(opponent.position.x - ego.position.x) < 1:
            do MoveTo(ego.position + Vector(1.5 * footed, 1.5, 0)) for 0.1 seconds
        else:
            do MoveTo(ego.position + Vector(2 * footed, -1, 0)) for 0.1 seconds
    interrupt when self.gameObject.ballPossession and distance from self to ego < (pressingDistance + 2) and distance from self to ego > (pressingDistance):
        do SetPlayerSpeed(1.5)
        do MoveTo(self.position + Vector((self.position.x - ego.position.x) * 5, 0, 0)) for 0.1 seconds
    interrupt when distance from self to goal < distance from ego to goal: # ahead of defendant
            try:
                do SetPlayerSpeed(5.0)
                do MoveTo(goal.position + Vector(0, 4, 0)) for 0.1 seconds
            interrupt when distance from self to goal < shootingDistance:
                do ShootBall(goal.position, "center-middle")
                do Idle()
                
# opponent_A = new Player ahead of ego by Uniform(5, 6),
#                 facing directly toward ego,
#                 with name "opponent_A", 
#                 with behavior opponentBehavior

opponent = new Player ahead of ego by Uniform(3, 4),
                facing directly toward ego,
                with name "opponent",
                with behavior opponentBehavior()

ball = new Ball ahead of opponent by 0.5
goal = new Goal behind ego by 3, facing away from ego
        
terminate when (ego.gameObject.stopButton)