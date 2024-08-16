from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# TODO: Right now the ball automatically gets recieved by AI when near ball, 
# instead make an action or paramter that enables this when we want

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (4, 2, .1), position = (5, -1.5, 0))
timestep = 0.1

footed = DiscreteRange(-1, 1)

pressingDistance = 3.5 #Uniform(4, 5)
shootingDistance = Uniform(4, 8)


behavior opponent1Behavior(pt):
    first_possession = False
    try:
        do InterceptBall(ball)
        do Idle() 

    interrupt when hasBallPosession(self):
        do SetPlayerSpeed(5.0)
        do MoveTo(goal.position) for 0.1 seconds
        opponent.prevPosition = opponent.position

    interrupt when self.gameObject.ballPossession and distance from self to ego < pressingDistance:
        do SetPlayerSpeed(10.0)
        if abs(opponent.position.x - ego.position.x) < 1:
            do MoveTo(ego.position + Vector(1.5 * footed, 1.5, 0)) for 0.1 seconds
            opponent.prevPosition = opponent.position
        else:
            do MoveTo(ego.position + Vector(2 * footed, -1, 0)) for 0.1 seconds
            opponent.prevPosition = opponent.position
    interrupt when self.gameObject.ballPossession and distance from self to ego < (pressingDistance + 2) and distance from self to ego > (pressingDistance):
        do SetPlayerSpeed(1.5)
        do MoveTo(self.position + Vector((self.position.x - ego.position.x) * 5, 0, 0)) for 0.1 seconds
        opponent.prevPosition = opponent.position


behavior teammateBehavior():
    try:
        do SetPlayerSpeed(1.0)
        do MoveTo(ball.position) for 3 seconds
        do SetPlayerSpeed(1.5)
        do Idle() 

    interrupt when hasBallPosession(opponent):
        coachPosition = ego.position
        do MoveTo(coachPosition) for 3 seconds
        do Idle()
    

ego = new Human at (5, Range(0,0.1), 0)

goal = new Goal behind ego by Range(2.9,3), facing away from ego
pt = new Point offset by (Range(-3,3), Range(-1,0))

opponent = new Player offset by (Range(-4,0), Range(6,10)),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent"

teammate = new Player ahead of ego by 1,
            facing toward opponent,
            with behavior teammateBehavior(),
            with name "teammate",
            with team "blue"

pt1 = new Point offset by (Range(3,5), Range(1,3))
pt2 = new Point offset by (Range(-5,-3), Range(1,3))
op2_pos = Uniform(pt1, pt2)


ball = new Ball ahead of opponent by 1

require (distance from op2_pos to pt) > 5
terminate when (ego.gameObject.stopButton)