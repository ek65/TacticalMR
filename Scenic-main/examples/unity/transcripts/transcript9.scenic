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
midfielderPos = Vector(Range(-1.5,-2), -2, 0)
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

opponentGoal = new Goal at (0,16,0), 
    facing away from pt,
    with name "opponentGoal"

teamGoal= new Goal at (0,-16,0), 
    with name "teamGoal",
    facing away from pt

opponent_A = new Player at (Range(-4,-5), Range(-4,-5)),
        with name "opponent_A",

opponent_B = new Player at (Range(2,4), Range(-4,-5)),
        with name "opponent_B"

opponent_C = new Player at (Range(-4,-5), Range(3,4)),
        with name "opponent_C",
        facing teamGoal

opponent_D = new Player at (Range(2,4), Range(3.5,4.5)),
        with name "opponent_D",
        facing teamGoal

opponent_E = new Player at (Range(0,2), Range(0,2)),
        with name "opponent_E",
        facing teamGoal

goalie = new Player behind teamGoal by 0.5,
    facing pt,
    with name "goalkeeper",
    with team "blue",
    with behavior goalieBehavior()


ball = new Ball ahead of goalie

terminate when (ego.gameObject.stopButton)