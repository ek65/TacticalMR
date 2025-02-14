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
    do Idle() until λ_precondition1(scene, sample)
    do MoveToWrapper(λ_dest)
    do Idle() until λ_precondition2(scene, sample)
    do PassTo("midfielder")
###

behavior leftBackBehavior():
    try: 
        do Idle()
    interrupt when (hasBallPosession(self) and (ego.position.y <= -4)):
        do Idle() until ego.gameObject.speed < 0.1
        do PassTo(ego)
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

behavior goalieBehavior():
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

teamGoal= new Goal at (0,-16,0), 
    with name "teamGoal",
    facing away from pt

ego = new Player at (4.5, 1.5, 0), 
        with name 'coach',
        with behavior coachBehavior(),
        facing teamGoal,
        with team "blue"

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

goalie = new Player at (0, -14, 0),
    facing 180 deg,
    with name "goalkeeper",
    with team "blue",
    with behavior goalieBehavior()

ball = new Ball ahead of goalie

### inserted
### Constraints and Lambda Functions for Action 1: Idle
A = HasBallPossession({'ref': 'leftBack'})
B = DistanceToObject({'ref': 'goalkeeper', 'obj': 'leftBack', 'min_dist': {'avg': 8.012040391621941, 'std': 1.7780501826436534e-06}, 'max_dist': None, 'operator': 'greater_than'})

def λ_precondition1(scene, sample):
    return A(scene, sample) and B(scene, sample)

### Constraints and Lambda Functions for Action 2: MoveTo
C = InZone({'obj': 'Coach', 'zone': 'B2'})
D = HasAngleOfPass({'ref': 'leftBack', 'radius': {'avg': 1.0114652498742274, 'std': 0.004478846927278735}})

def λ_dest(scene, sample):
    return C(scene, sample) and D(scene, sample)

### Constraints and Lambda Functions for Action 3: Idle
E = HasBallPossession({'ref': 'coach'})

def λ_precondition2(scene, sample):
    return E(scene, sample)

### Constraints and Lambda Functions for Action 4: PassTo

###

def sample_target(scene, prev_target, λ_dest) -> Vector: 
    global sample
    i = 0
    target = [prev_target.x, prev_target.y]
    
    while not λ_dest(scene, target):

        x = Range(-FIELD_WIDTH / 2, FIELD_WIDTH / 2)
        y = Range(-FIELD_HEIGHT / 2, FIELD_HEIGHT / 2)
        target = [x,y]

        if i > 100000:
            raise Exception("Maximum sample depth exceeded.")
        i += 1

    sample = Vector(target[0], target[1])
    return sample

behavior MoveToWrapper(λ_dest):
    scene = simulation()
    sample = Vector(0, 0)
    sample = sample_target(scene, sample, λ_dest)
    while (distance from self to sample > 0.5):
        do MoveTo(sample) for timestep seconds
        sample = sample_target(scene, sample, λ_dest)
    do Idle() for 1 seconds

terminate when (ego.gameObject.stopButton)