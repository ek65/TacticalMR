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
pt1 = new OrientedPoint at (0,-10,0)

behavior midfielder1Behavior():
    try: 
        do Idle() 
    interrupt when (hasBallPosession(ego)):
        do MoveTo(Vector(9, 0, 0), ball)
    interrupt when (movesDownTheMidLine(opponent_B)):
        do MoveTo(Vector(7, 3, 0)) 

behavior midfielder2Behavior():
    try: 
        do Idle()
    interrupt when (hasBallPosession(ego)):
        do MoveTo(Vector(-2, -7, 0), ball)

behavior opponentCbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do Idle()

behavior opponentEbehavior():
    try: 
        do Idle()
    interrupt when ((distance from self to midfielder2) > 0.5):
        do MoveTo(midfielder2, ball, 1)
    interrupt when (isMovingUpTheField(ego)):
        do MoveTo(ego, distance = 4)

behavior opponentBbehavior():
    try: 
        do Idle()
    interrupt when ((distance from self to midfielder1) > 0.5):
        do MoveTo(midfielder1, ball, 1)
    interrupt when (isMovingUpTheField(ego)):
        do MoveTo(ego, distance = 4)
    
behavior leftBackBehavior():
    try: 
        do Idle()
    interrupt when (isMovingUpTheField(ego)):
        do MoveTo(Vector(-3,0,0))

behavior goalieBehavior():
    do MoveTo(ball.position)
    do Idle() for 1 seconds
    do PassTo(ego)
    do Idle() for 1 seconds
    do MoveTo(Vector(2, -13, 0))
    do Idle()

behavior coachBehavior():
    try:
        do Idle()
    interrupt when (hasBallPosession(self)):
        do MoveTo(Vector(3,-5,0), ball)
    interrupt when (distance from self to opponent_E < 3 or distance from self to opponent_B < 3):
        do Idle() for 0.5 seconds
        do PassTo(leftback)

# ego = new Human at (7, -12, 0), 
#         with name 'coach'
ego = new Player at (7, -12, 0),
        with name "coach",
        with team "blue",
        with behavior coachBehavior()

leftback = new Player at (-7.5, -9, 0), 
        with name "leftback",
        with team "blue",
        with behavior leftBackBehavior()

centerBack = new Player at (-2, -12.5, 0),
        with name "centerBack",
        with team "blue"

midfielder2 = new Player at (0, -2, 0), 
        with name "midfielder2",
        with team "blue",
        with behavior midfielder2Behavior()

midfielder1 = new Player at (8,1.5,0), 
        with name "midfielder1",
        with team "blue",
        with behavior midfielder1Behavior()

goal= new Goal at (0,-16,0), 
    with name "goal",
    facing away from pt

opponent_A = new Player at (-5, -7, 0),
        with name "opponent_A",

opponent_B = new Player ahead of midfielder1 by 0.1,
        with name "opponent_B",
        with behavior opponentBbehavior()

opponent_C = new Player at (-6.5, 1.5),
        with name "opponent_C",
        facing goal,
        with behavior opponentCbehavior()

opponent_D = new Player at (0, 10),
        with name "opponent_D",
        facing goal

opponent_E = new Player at (0,0),
        with name "opponent_E",
        facing goal,
        with behavior opponentEbehavior()


goalie = new Player behind goal by 0.2,
    facing pt,
    with name "goalie",
    with team "blue",
    with behavior goalieBehavior()


ball = new Ball ahead of goalie by 0.2

terminate when (ego.gameObject.stopButton)