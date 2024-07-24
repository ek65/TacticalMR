from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
from scenic.core.regions import MeshVolumeRegion
import trimesh
import random

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

# Coach Behavior
behavior coachBehavior():
    try:
        do Idle()
    interrupt when isOpponentFacingWithBall():
        do Invalid code()
    interrupt when Invalid code():
        do interceptBallFromOpponent()
    interrupt when opponent_tries_to_take_ball_away():
        do headTowardsGoal()



# Define Ego

ego = new Player at (0, 0, 0), with behavior coachBehavior()


opponent = new Player ahead of ego by Uniform(3, 4),
                facing directly toward ego,
                with name "opponent",
                with behavior opponentBehavior()
ball = new Ball ahead of opponent by 0.5
goal = new Goal behind ego by 3, facing away from ego

# Python
def isOpponentFacingWithBall() -> bool:
    opponent = closest(Player, coach)
    return hasBallPosession(opponent) and isFacingAt(opponent, coach)
def approachAndSet():
    player = Player()
    opponent = Object()
    
    # Move towards the opponent with sprinting style
    target = Coordinate(CoordinateInit.RELATIVE, [opponent])
    speed = Speed(SpeedInit.MAGNITUDE)
    moveTo(player, target, MovingStyle.SPRINT, speed)
    
    # Determine the foot preference
    foot_preference = footed(player)
    
    # Set the angle based on foot preference
    angle = 45 if foot_preference == 'right' else -45
    
    # Face towards the opponent with the specified angle
    faceTowards(player, opponent, angle)

def isNextToOpponent() -> bool:
    player = Player()
    opponent = closest(type=Object, ref=player)
    if opponent is None:
        return False
    distance_to_opponent = distance from player.position to opponent.position
    return distance_to_opponent < 1.0  # Assuming 1.0 units as the threshold for 'next to'
def interceptBallFromOpponent():
    if hasBallPosession(opponent):
        if isFacingAt(opponent, ball):
            tackle(player, opponent)
        else:
            faceTowards(player, ball)
            moveTo(player, Coordinate(CoordinateInit.RELATIVE, [ball]), MovingStyle.SPRINT, Speed(SpeedInit.MAGNITUDE))
            tackle(player, opponent)
def opponent_tries_to_take_ball_away() -> bool:
    player = closest(Player, ball, max=float('inf'))
    opponent = closest(Player, player, max=float('inf'))
    if opponent and movingTowards(opponent, ball):
        return True
    return False
def headTowardsGoal():
    player = Player()
    goal = Object()
    target_coordinate = Coordinate(CoordinateInit.RELATIVE, [goal])
    speed = Speed(SpeedInit.MAGNITUDE)
    moveTo(player, target_coordinate, MovingStyle.RUN, speed)





terminate when (ego.gameObject.stopButton)