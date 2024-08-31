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
pt = new OrientedPoint at (0,0,0)
midfielderPos = Vector(Range(-1.5,-2) - 3, -2, 0)
rightBackFlag = False


behavior midfielder1Behavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do Idle()

behavior midfielder2Behavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(goalie):
        do MoveTo(centerBack.position) until (distance from self to centerBack < 1)
        do Idle()

behavior opponentCbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do MoveTo(midfielder1.position) until (distance from self to midfielder1 < 1)
        do Idle()

behavior opponentEbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do MoveTo(leftback.position) for 1.5 seconds
        do Idle()

behavior opponentBbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do MoveTo(midfielder2.position) until (distance from self to midfielder2Ahead < 1)
        do Idle()

behavior leftBackBehavior():
    try: 
        do MoveTo(ball.position)
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle() for 2 seconds
        do GroundPassFast(goalie.position)
        do Idle()

behavior centerBackBehavior():
    try: 
        do Idle()
    interrupt when distance from self to midfielder2 < 1.5:
        do Idle() for 4 seconds
        do MoveTo(Vector(self.position.x + 3, self.position.y - 2.5, self.position.z))
        do Idle()



behavior goalieBehavior():
    try: 
        do Idle() for 1 seconds
        do MoveTo(Vector(self.position.x - 2, self.position.y, self.position.z))
        do Idle() 
    interrupt when hasBallPosession(opponent_A):
        do Idle() 

ego = new Human at (Range(5.5, 6), -9, 0), with name 'rightback'

leftback = new Player at (Range(-5.5,-6), -9, 0), 
        with name "leftback",
        with team "blue",
        with behavior leftBackBehavior()

centerBack = new Player at (0, Range(-10,-11), 0),
        with name "centerBack",
        with team "blue",
        with behavior centerBackBehavior()

midfielder2 = new Player ahead of centerBack by 2, 
        with name "midfielder2",
        with team "blue",
        with behavior midfielder2Behavior()

midfielder1 = new Player at midfielderPos, 
        with name "midfielder1",
        with team "blue",
        with behavior midfielder1Behavior()

midfielder2Ahead = new OrientedPoint ahead of midfielder2 by 0.2

opponentGoal = new Goal at (0,16,0), 
    facing away from pt,
    with name "opponentGoal"

teamGoal= new Goal at (0,-16,0), 
    with name "teamGoal",
    facing away from pt

opponent_A = new Player at (Range(-4,-5), Range(-4,-5)),
        with name "opponent_A",

opponent_B = new Player at (Range(2,4), Range(-4,-5)),
        with name "opponent_B",
        with behavior opponentBbehavior

opponent_C = new Player at (Range(-4,-5), Range(3,4)),
        with name "opponent_C",
        facing teamGoal,
        with behavior opponentCbehavior()

opponent_D = new Player at (Range(2,4), Range(3.5,4.5)),
        with name "opponent_D",
        facing teamGoal

opponent_E = new Player at (Range(0,2), Range(0,2)),
        with name "opponent_E",
        facing teamGoal,
        with behavior opponentEbehavior()


goalie = new Player behind teamGoal by 0.5,
    facing pt,
    with name "goalie",
    with team "blue",
    with behavior goalieBehavior()


ball = new Ball ahead of leftback

terminate when (ego.gameObject.stopButton)