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
param performAct = False

behavior midfielder1Behavior():
    try: 
        do Idle() 
    interrupt when (hasBallPosession(rightback)):
        do Idle() for 1 seconds
        do MoveTo(Vector(self.position.x + 1, self.position.y - 1, self.position.z))
        do Idle()
    interrupt when ((distance from opponent_E to pt1) <= 3):
        do Idle() for 2 seconds 
        do MoveTo(Vector(self.position.x - 3, self.position.y + 2, self.position.z))      
        do Idle()

behavior midfielder2Behavior():
    try: 
        do Idle() for 1 seconds
        do MoveTo(opponent_E.position)
    interrupt when (distance from self to opponent_E) <= 1.5:
        do Idle()
    interrupt when (hasBallPosession(rightback)):
        do MoveTo(Vector(self.position.x - 1, self.position.y - 7, self.position.z))
        performAct = True
        do Idle()

behavior opponentCbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do Idle()

behavior opponentEbehavior():
    try: 
        do Idle()
    interrupt when ((distance from midfielder2 to pt1) <= 3):
        do Idle() for 1 seconds
        do MoveTo(midfielder2.position)
        try:
            do Idle()
        interrupt when ((distance from self to midfielder2) <= 1.5):
            do Idle() for 1 seconds
            do MoveTo(rightback.position) until ((distance from self to rightback <= 2))
            do Idle()


behavior opponentBbehavior():
    try: 
        do Idle()
    interrupt when ((distance from opponent_E to pt1) <= 3):
        do Idle() for 1 seconds 
        do MoveTo(rightback.position) until ((distance from self to rightback <= 2))
        do Idle()

# behavior leftBackBehavior():
#     try: 
#         do Idle()
#     interrupt when ((distance from opponent_E to rightback) <= 4):
#         do MoveTo(Vector(self.position.x + 3, self.position.y + 8, self.position.z))
#         do Idle()

behavior rightBackBehavior():
    try:
        do Idle()
    interrupt when (hasBallPosession(self)):
        do Idle() for 5 seconds
        do MoveTo(Vector(self.position.x - 1, self.position.y + 4, self.position.z))
        do Idle()


behavior centerBackBehavior():
    try: 
        do Idle()
    interrupt when distance from self to midfielder2 < 1.5:
        do Idle() for 4 seconds



behavior goalieBehavior():
    try: 
        do Idle() for 1 seconds
        do MoveTo(ball.position)
    interrupt when (hasBallPosession(self)):
        do Idle() for 2 seconds
        do GroundPassFast(rightback.position)
        do Idle() for 1 seconds
        do MoveTo(Vector(self.position.x + 2, self.position.y, self.position.z))
        do Idle()

ego = new Human at (Range(-7,-8), -9, 0), with name 'leftback'
leftback = ego

rightback = new Player at (Range(5.5, 6), -9, 0), 
        with name "rightback",
        with team "blue",
        with behavior rightBackBehavior()

centerBack = new Player at (-2, Range(-12,-13), 0),
        with name "centerBack",
        with team "blue",
        with behavior centerBackBehavior()

midfielder2 = new Player ahead of centerBack by 5, 
        with name "midfielder2",
        with team "blue",
        with behavior midfielder2Behavior()

midfielder1 = new Player at (8,1.5,0), 
        with name "midfielder1",
        with team "blue",
        with behavior midfielder1Behavior()

midfielder2Ahead = new OrientedPoint ahead of midfielder2 by 0.2

goal= new Goal at (0,-16,0), 
    with name "goal",
    facing away from pt

opponent_A = new Player at (Range(-4.7,-5), Range(-6.8,-7), 0),
        with name "opponent_A",

opponent_B = new Player ahead of midfielder1 by 0.1,
        with name "opponent_B",
        with behavior opponentBbehavior()

opponent_C = new Player at (Range(-6,-6.5), Range(1,1.5)),
        with name "opponent_C",
        facing goal,
        with behavior opponentCbehavior()

opponent_D = new Player at (Range(0,0.5), 10),
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