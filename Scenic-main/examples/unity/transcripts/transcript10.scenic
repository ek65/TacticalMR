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
midfielderPos = Vector(Range(-1.5,-2), 1, 0)
destPosMid = Vector(midfielderPos.x - 3, midfielderPos.y, midfielderPos.z)


behavior midfielder1Behavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(ego):
        do MoveTo(destPosMid)
        do Idle()

behavior midfielder2Behavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(ego):
        do MoveTo(mfTwoPos.position)
        do Idle()
    interrupt when hasBallPosession(self):
        do GroundPassFast(midfielder1.position)
        do Idle()


behavior opponentAbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(ego):
        do SetSpeed(0.5)
        do MoveTo(ego.position) until (distance from self to ego < 2)

behavior goalieBehavior():
    try: 
        do Idle() for 1 seconds
        do GroundPassFast(ego.position)
        do Idle() 
    interrupt when hasBallPosession(ego):
        do MoveTo(Vector(self.position.x - 2, self.position.y, self.position.z))
        do Idle() 

ego = new Human at (Range(-5.5,-6), -9, 0), with name 'leftback'


midfielder2 = new Player at (Range(0.4,1), Range(1.5,1.9),0), 
        with name "midfielder2",
        with team "blue",
        with behavior midfielder2Behavior()

rightback = new Player at (Range(5.5, 6), -9, 0), 
        with name "rightback",
        with team "blue"

midfielder1 = new Player at midfielderPos, 
        with name "midfielder1",
        with team "blue",
        with behavior midfielder1Behavior()

centerBack = new Player at (0, Range(-9,-10), 0), 
        with name "centerBack",
        with team "blue"

mfTwoPos = new OrientedPoint ahead of centerBack by Range(1.5,2)

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
        with name "opponent_d",
        facing teamGoal


goalie = new Player behind teamGoal by 0.5,
    facing pt,
    with name "goalie",
    with team "blue",
    with behavior goalieBehavior()


ball = new Ball ahead of goalie

terminate when (ego.gameObject.stopButton)