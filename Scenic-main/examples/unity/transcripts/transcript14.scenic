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

# TEAM
behavior rightBackBehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

behavior leftBackBehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()


behavior centerBackBehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

behavior midfielder1Behavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

behavior midfielder2Behavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

behavior goalieBehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()


# OPPONENTS

behavior opponentAbehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

behavior opponentBbehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

behavior opponentCbehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

behavior opponentDbehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

behavior opponentEbehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()
 

behavior leftBackBehavior():
    try:
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

ego = new Human at (-50,-50,0)

rightBack = new Player at (Range(5.5, 6), Range(-12.5,-11.7), 0), 
    with name 'rightBack',
    with team "blue",
    with behavior rightBackBehavior()

leftBack = new Player at (Range(-5.5,-4.2), Range(-10.5,-12), 0), 
        with name "leftback",
        with team "blue",
        with behavior leftBackBehavior()

centerBack = new Player at (Range(-1,-1.7), Range(-13.5,-12), 0),
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

goal= new Goal at (0,-16,0), 
    with name "goal",
    facing away from pt

opponent_A = new Player at (Range(-4,-5), Range(-4,-5)),
        with name "opponent_A",
        with behavior opponentAbehavior()

opponent_B = new Player at (Range(2,4), Range(-4,-5)),
        with name "opponent_B",
        with behavior opponentBbehavior()

opponent_C = new Player at (Range(-4,-5), Range(3,4)),
        with name "opponent_C",
        facing goal,
        with behavior opponentCbehavior()

opponent_D = new Player at (Range(0,1.5), Range(2,3)),
        with name "opponent_D",
        facing goal,
        with behavior opponentDbehavior()

opponent_E = new Player at (Range(0,0.5), Range(0,0.5)),
        with name "opponent_E",
        facing goal,
        with behavior opponentEbehavior()


goalie = new Player behind goal by 0.5,
    facing pt,
    with name "goalie",
    with team "blue",
    with behavior goalieBehavior()


ball = new Ball ahead of goalie by 0.1

terminate when (ego.gameObject.stopButton)