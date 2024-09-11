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

behavior leftBackBehavior():
    try: 
        do Idle()
    interrupt when (hasBallPosession(self) and (ego.position.y <= -4)):
        do GroundPassFast(ego.position)
        do Idle()

behavior midfielder1Behavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do Idle() for 1 seconds
        do MoveTo(destPosMid)
        take LookAtAction(ball)
        do Idle()

behavior opponentAbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do SetSpeed(0.5)
        do MoveTo(oppAposition) 

behavior goalieBehavior():
    try: 
        do Idle() for 1 seconds
        do GroundPassFast(leftback.position)
        do Idle() 
    interrupt when hasBallPosession(leftback):
        do MoveTo(Vector(self.position.x - 2, self.position.y, self.position.z))
        do Idle() 

behavior blockSideLine(opp_A_dest):
    try:
        do Idle()
    interrupt when (hasBallPosession(leftback)):
        do MoveTo(opp_A_dest)
        take LookAtAction(ball)

teamGoal= new Goal at (0,-16,0), 
    with name "teamGoal",
    facing away from pt

ego = new Human at (4.5, 1.5, 0), 
        with name 'coach',
        facing teamGoal

leftback = new Player at (-7, -8.5, 0), 
        with name "leftBack",
        with team "blue",
        with behavior leftBackBehavior(),
        facing teamGoal

rightback = new Player at (7, -9, 0), 
        with name "ightBack",
        with team "blue",
        facing teamGoal

midfielderPos = new Point at (-3, 2, 0)
destPosMid = new Point at (-6, 2, 0)

midfielder1 = new Player at midfielderPos, 
        with name "Midfielder",
        with team "blue",
        with behavior midfielder1Behavior(),
        facing teamGoal

centerBack = new Player at (0, -11, 0), 
        with name "CenterBack",
        with team "blue",
        facing teamGoal

opponentGoal = new Goal at (0,16,0), 
    facing away from pt,
    with name "opponentGoal"

opp_A_dest = new Point at (-6.5, -7, 0)

opponent_A = new Player at (-5, -3.5),
        with name "opponent_A",
        facing teamGoal,
        with behavior blockSideLine(opp_A_dest)

opponent_B = new Player at (3, -4.5),
        with name "opponent_B",
        facing teamGoal

opponent_C = new Player at (-4.5, 4.5),
        with name "opponent_C",
        facing teamGoal

opponent_D = new Player at (4, 5),
        with name "opponent_D",
        facing teamGoal

opponent_E = new Player at (0, 4),
        with name "opponent_E",
        facing teamGoal

goalie = new Player at (0, -14, 0),
    facing 180 deg,
    with name "goalkeeper",
    with team "blue",
    with behavior goalieBehavior()

ball = new Ball ahead of goalie

terminate when (ego.gameObject.stopButton)