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
midfielderPos = Vector(-1.7, -2, 0)
destPosMid = Vector(midfielderPos.x - 3, midfielderPos.y, midfielderPos.z)

behavior leftBackBehavior():
    try: 
        do Idle()
    interrupt when (hasBallPosession(self) and (distance from ego to leftback < 7)):
        do GroundPassFast(ego.position)
        do Idle()

behavior midfielder1Behavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do Idle() for 1 seconds
        do MoveTo(destPosMid)
        do Idle()

behavior opponentAbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do SetSpeed(0.5)
        do MoveTo(leftback.position) until (distance from self to leftback < 2)

behavior goalieBehavior():
    try: 
        do Idle() for 1 seconds
        do GroundPassFast(leftback.position)
        do Idle() 
    interrupt when hasBallPosession(leftback):
        do MoveTo(Vector(self.position.x - 2, self.position.y, self.position.z))
        do Idle() 

<<<<<<< HEAD
behavior blockSideLine():
    try:
        do Idle()
    interrupt when (hasBallPosession(leftback)):
        do MoveTo(new Point at (-4.5, -4.5))

teamGoal= new Goal at (0,-16,0), 
    with name "teamGoal",
=======

ego = new Human at (Range(0.4,1), Range(1.5,1.9),0), with name 'coach'

leftback = new Player at (Range(-5.5,-6), -9, 0), 
        with name "leftback",
        with team "blue",
        with behavior leftBackBehavior()

rightback = new Player at (Range(5.5, 6), -9, 0), 
        with name "rightback",
        with team "blue"

midfielder1 = new Player at midfielderPos, 
        with name "midfielder",
        with team "blue",
        with behavior midfielder1Behavior()

centerBack = new Player at (0, Range(-9,-10), 0), 
        with name "centerBack",
        with team "blue"

goal= new Goal at (0,-16,0), 
    with name "goal",
>>>>>>> 4006d7c9499ff1ee1264de4581ccdd14af12a893
    facing away from pt

ego = new Human at (0.5, 1.7, 0), 
        with name 'Coach',
        facing teamGoal

leftback = new Player at (-5.5, -8.5, 0), 
        with name "LeftBack",
        with team "blue",
        with behavior leftBackBehavior(),
        facing teamGoal

rightback = new Player at (5.5, -9, 0), 
        with name "RightBack",
        with team "blue",
        facing teamGoal

midfielder1 = new Player at midfielderPos, 
        with name "Midfielder",
        with team "blue",
        with behavior midfielder1Behavior(),
        facing teamGoal

centerBack = new Player at (0, -9, 0), 
        with name "CenterBack",
        with team "blue",
        facing teamGoal

opponentGoal = new Goal at (0,16,0), 
    facing away from pt,
    with name "opponentGoal"

opponent_A = new Player at (-1.5, -3.5),
        with name "opponent_A",
        facing teamGoal,
        with behavior blockSideLine()

opponent_B = new Player at (3, -4.5),
        with name "opponent_B",
        facing teamGoal

opponent_C = new Player at (-4.5, 3.5),
        with name "opponent_C",
        facing goal

opponent_D = new Player at (4, 4),
        with name "opponent_D",
        facing goal

opponent_E = new Player at (2, 2),
        with name "opponent_E",
        facing goal

goalie = new Player behind goal by 0.5,
    facing pt,
    with name "goalkeeper",
    with team "blue",
    with behavior goalieBehavior()

ball = new Ball ahead of goalie

terminate when (ego.gameObject.stopButton)