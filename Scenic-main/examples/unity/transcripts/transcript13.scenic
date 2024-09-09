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
        do Idle()

behavior opponentCbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do Idle()

behavior opponentEbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do Idle()

behavior opponentBbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do Idle()

behavior leftBackBehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

behavior centerBackBehavior():
    try: 
        do Idle()
    interrupt when distance from self to midfielder2 < 1.5:
        do Idle() for 4 seconds



behavior goalieBehavior():
    try: 
        do Idle() for 1 seconds
    interrupt when hasBallPosession(opponent_A):
        do Idle() 

ego = new Human at (Range(5.5, 6), -9, 0), with name 'rightback'

leftback = new Player at (Range(-7,-8), -9, 0), 
        with name "leftback",
        with team "blue",
        with behavior leftBackBehavior()

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
        with behavior opponentBbehavior

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


goalie = new Player behind goal by 0.5,
    facing pt,
    with name "goalie",
    with team "blue",
    with behavior goalieBehavior()


ball = new Ball ahead of goalie

terminate when (ego.gameObject.stopButton)