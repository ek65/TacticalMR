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

FIELD_WIDTH, FIELD_HEIGHT = 20, 34
NUM_ZONES_X, NUM_ZONES_Y = 4, 5
ZONE_WIDTH = FIELD_WIDTH / NUM_ZONES_X
ZONE_HEIGHT = FIELD_HEIGHT / NUM_ZONES_Y

sample = None

### inserted
behavior coachBehavior():
    scene = simulation()
    print("coachBehavior1")
    do Idle() until λ_precondition(scene, sample)
    print("coachBehavior2")
    do PassTo("midfielder1")
###

behavior midfielder2Behavior():
    try: 
        do Idle()
    interrupt when (hasBallPosession(ego)):
        do MoveTo(new Point at (-1, -6))
        # do MoveTo(ball)
    interrupt when (hasBallPosession(self)):
        do PassTo(midfielder1)

behavior midfielder1Behavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(ego):
        do Idle() for 1 seconds
        do MoveTo(destPosMid)
        take LookAtAction(ball)
        do Idle()

behavior opponentAbehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(ego):
        do SetSpeed(0.5)
        do MoveTo(oppAposition) 

behavior goalieBehavior():
    try: 
        do Idle() for 1 seconds
        do PassTo(ego.position)
        do Idle() 
    interrupt when hasBallPosession(ego):
        do MoveTo(Vector(self.position.x - 2, self.position.y, self.position.z))
        do Idle() 

behavior blockSideLine(opp_A_dest):
    try:
        do Idle()
    interrupt when (hasBallPosession(ego)):
        do MoveTo(opp_A_dest)
        take LookAtAction(ball)

teamGoal= new Goal at (0,-16,0), 
    with name "teamGoal",
    facing away from pt

ego = new Player at (-7, -8.5, 0), 
        with name 'coach',
        with behavior coachBehavior(),
        facing teamGoal,
        with team "blue"

midfielder2 = new Player at (4.5, 1.5, 0), 
        with name "midfielder2",
        with team "blue",
        with behavior midfielder2Behavior(),
        facing teamGoal

rightback = new Player at (7, -9, 0), 
        with name "rightBack",
        with team "blue",
        facing teamGoal

midfielderPos = new Point at (-3, 2, 0)
destPosMid = new Point at (-6, 2, 0)

midfielder1 = new Player at midfielderPos, 
        with name "midfielder1",
        with team "blue",
        with behavior midfielder1Behavior(),
        facing teamGoal

centerBack = new Player at (0, -11, 0), 
        with name "centerBack",
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

### inserted
A = HasBallPossession({'ref': 'coach'})
B = InZone({'zone': 'A2'})
C = HasAngleOfPass({'ref': 'midfielder2', 'radius': {'avg': 2.460801632098414, 'std': 1.0}})

def λ_precondition(scene, sample):
    a = A(scene, sample)
    b = B(scene, sample)
    c = C(scene, sample)
    print(a, b, c)
    return a and b and c
###



terminate when (ego.gameObject.stopButton)