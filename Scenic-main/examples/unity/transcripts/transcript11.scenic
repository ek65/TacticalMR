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
midfielderPos = new Point at (-6.5, 0.5, 0)
rightBackFlag = False


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
        do MoveTo(new Point at (-3, 0.5, 0)) 

behavior opponentBbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do MoveTo(new Point at (0, -4, 0))

behavior leftBackBehavior():
    try: 
        do MoveTo(ball.position)
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle() for 3 seconds
        do PassTo(goalie)

behavior rightBackBehavior():
    try: 
        do Idle()
    interrupt when ego.position.y < -5:
        do MoveTo(new Point at (8, -7, 0))
        do LookAt(ball)

behavior goalieBehavior():
    do Idle() for 1 seconds
    do MoveTo(new Point at (self.position.x - 2, self.position.y, self.position.z))
    do Idle()

behavior centerBackBehavior():
    try: 
        do Idle()
    interrupt when isMovingTowards(ego, goal) and (distance from self to ego) < 8:
        do MoveTo(new Point at (3,-14, 0))
        do LookAt(ball)

behavior coachBehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(goalie):
        do MoveTo(new Point at (0, -11, 0))

leftback = new Player at (-7, -9, 0), 
        with name "leftback",
        with team "blue",
        with behavior leftBackBehavior()

ego = new Player at (-1,-4,0), 
        with name "coach",
        with team "blue",
        with behavior coachBehavior()

centerBack = new Player at (0,-11, 0),
        with name "centerBack",
        with team "blue",
        with behavior centerBackBehavior()

rightback = new Player at (7, -9, 0), 
        with name "rightback",
        with team "blue",
        with behavior rightBackBehavior()

midfielder1 = new Player at midfielderPos, 
        with name "midfielder1",
        with team "blue"

goal= new Goal at (0,-16,0), 
    with name "goal",
    facing away from pt

opponent_A = new Player at (-4.5, -5),
        with name "opponent_A",

opponent_B = new Player at (Range(2,4), Range(-4,-5)),
        with name "opponent_B",
        with behavior opponentBbehavior

opponent_C = new Player at (-4, 4, 0),
        with name "opponent_C",
        facing goal,
        with behavior opponentCbehavior()

opponent_D = new Player at (4, 5, 0),
        with name "opponent_D",
        facing goal

opponent_E = new Player at (0, 4, 0),
        with name "opponent_E",
        facing goal,
        with behavior opponentEbehavior()

goalie = new Player behind goal by 0.5,
    facing pt,
    with name "goalie",
    with team "blue",
    with behavior goalieBehavior()


ball = new Ball ahead of leftback

terminate when (ego.gameObject.stopButton)