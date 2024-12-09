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
footed = DiscreteRange(-1, 1)

FIELD_WIDTH, FIELD_HEIGHT = 20, 34
NUM_ZONES_X, NUM_ZONES_Y = 4, 5
ZONE_WIDTH = FIELD_WIDTH / NUM_ZONES_X
ZONE_HEIGHT = FIELD_HEIGHT / NUM_ZONES_Y

sample = None

### inserted
behavior coachBehavior():
    scene = simulation()
    do Idle() for 1 seconds
    do Idle() until λ_precondition1(scene, sample)
    do PassTo("teammate")
    do Idle() until λ_precondition2(scene, sample)
    do PassTo("teammate")
###

behavior opponent1Behavior(pt):
    do Idle()

behavior teammateBehavior():
    try:
        do Idle()

    interrupt when hasBallPosession(self):
        #pos = inBetween(opponent, opponent2)
        do Idle() for 1 seconds
        do PassTo(ego.position)
        do Idle() for 0.5 seconds
        do MoveTo(Vector(teammate.position.x + 2, teammate.position.y, teammate.position.z)) for 1 seconds
        do MoveTo(Vector(3.3,7,0))
        do Idle()
    
ego = new Player at (0,0,0), 
        with name 'coach',
        with behavior coachBehavior(),
        with team "blue"

pt = new Point at (0,-10,0)

opponent = new Player at (-4.5,6,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_A"

opponent2 = new Player at (-0.5,6,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_B"

opponent3 = new Player at (3.5,6,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_C"

opponent4= new Player at (7.5,6,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_D"

teammate = new Player at (-4.2,3.5,0),
            facing toward ego,
            with behavior teammateBehavior(),
            with name "teammate",
            with team "blue"

ball = new Ball ahead of ego 

goal= new Goal at (0,-16,0), 
    with name "goal",
    facing away from pt

### inserted
A = HasAngleOfPass({'ref': 'teammate', 'radius': {'avg': 2.5179357573229906, 'std': 0.0}})
B = AheadOfLine({'obj': 'teammate', 'height': {'avg': 3.50000119, 'std': 0.0}})

def λ_precondition1(scene, sample):
    return A(scene, sample) and B(scene, sample)

C = HasBallPossession({'ref': 'coach'})
D = AheadOfLine({'obj': 'teammate', 'height': {'avg': 6.50001025, 'std': 0.0}})

def λ_precondition2(scene, sample):
    return C(scene, sample) and D(scene, sample)
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


    
    

            
