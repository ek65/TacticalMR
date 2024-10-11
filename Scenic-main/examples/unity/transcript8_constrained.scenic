from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# TODO: Right now the ball automatically gets recieved by AI when near ball, 
# instead make an action or paramter that enables this when we want

timestep = 0.1
pt = new OrientedPoint at (0,0,0)

behavior leftBackBehavior():
    try: 
        do Idle()
    interrupt when (hasBallPosession(self) and (distance from ego to sampled_point < 0.5)):
        do PassTo(sampled_point.position)
        do Idle()

behavior midfielder1Behavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do Idle() for 1 seconds
        do MoveTo(destPosMid)
        do LookAt(ball)

behavior opponentAbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(leftback):
        do SetSpeed(0.5)
        do MoveTo(oppAposition) 

behavior goalkeeperBehavior():
    try: 
        do Idle() for 1 seconds
        do PassTo(leftback)
        do Idle() 
    interrupt when hasBallPosession(leftback):
        do MoveTo(Vector(self.position.x - 2, self.position.y, self.position.z))
        do Idle() 

behavior blockSideLine(opp_A_dest):
    try:
        do Idle()
    interrupt when (hasBallPosession(leftback)):
        do MoveTo(opp_A_dest)
        do LookAt(ball)

behavior coachBehavior():
    print("------------------")
    print(sampled_point.position)
    do Idle() for 3 seconds
    do MoveTo(sampled_point)

teamGoal= new Goal at (0,-16,0), 
    with name "teamGoal",
    facing away from pt

ego = new Coach at (4.5, 1.5, 0), 
        with name 'coach',
        facing teamGoal,
        with behavior coachBehavior()

leftback = new Player at (-7, -8.5, 0), 
        with name "leftBack",
        with team "blue",
        with behavior leftBackBehavior(),
        facing teamGoal

rightback = new Player at (7, -9, 0), 
        with name "rightBack",
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

goalkeeper = new Player at (0, -14, 0),
    facing 180 deg,
    with name "goalkeeper",
    with team "blue",
    with behavior goalkeeperBehavior()

ball = new Ball ahead of goalkeeper

# Check that the line from C to T does not intersect the opponents' circles
def no_intersection(C, T, Oa, Ob, radius, constrained_region):
    opponentA_region = CircularRegion(center=Oa, radius=radius)
    opponentB_region = CircularRegion(center=Ob, radius=radius)

    main_region = constrained_region

    safe_region = main_region.difference(opponentA_region).difference(opponentB_region)

    return safe_region

constrained_region = RectangularRegion((0,0), 0, 20, 34) # x = -10 to 10, y = -17 to 17
# Define the constants and parameters
y_threshold = 0
y_threshold_region = RectangularRegion((0,0), 0, 20, 34)
if (y_threshold == 0):
    y_threshold_region = RectangularRegion((0,-8.5), 0, 20, 17)

constrained_region = constrained_region.intersect(y_threshold_region)

C = ego.position
T = leftback.position
Oa = opponent_A.position
Ob = opponent_B.position
radius = 1.0

safe_intersection_region = no_intersection(C, T, Oa, Ob, radius, constrained_region)
constrained_region = constrained_region.intersect(safe_intersection_region)
sampled_point = new Point in constrained_region

terminate when (ego.gameObject.stopButton)