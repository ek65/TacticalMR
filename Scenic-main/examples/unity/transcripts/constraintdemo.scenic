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

target = Vector(0, 0, 0)

behavior coachBehavior():
    try:
        scene = simulation()
        target = sample_target()
        do moveTo(target) for timestep seconds
    interrupt when (False):
        do Idle()

behavior teammateBehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()

behavior opponentBehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(self):
        do Idle()
       

teammate = new Player at (-6.5, -9, 0), 
        with name "teammate",
        with team "blue",
        with behavior teammateBehavior()

opponent_1 = new Player at (-4, -6, 0),
        with name "opponent 1"

opponent_2 = new Player at (4, -12, 0),
        with name "opponent 2"

goal= new Goal at (0,-16,0), 
    with name "goal",
    facing away from pt

ego = new Player at (3.5,2, 0),
            with name "coach",
            with behavior coachBehavior(),
            facing goal 

ball = new Ball ahead of ego


# [obj if obj.name.startswith(" ") else None for obj in scene.objects]


terminate when (ego.gameObject.stopButton)

A = InZone({'zone': 'C2'})
B = HasAngle({'ref': teammate, 'r': 2.3})

def sample_target() -> Vector:

    i = 0
    prev = [target.x, target.y]

    while not λ(prev):

        x = Range(-FIELD_WIDTH / 2, FIELD_WIDTH / 2)
        y = Range(-FIELD_HEIGHT / 2, FIELD_HEIGHT / 2)

        prev = Vector(x, y, 0)

        if i > 1000:
            raise Exception("Maximum sample depth exceeded.")
        i += 1

    return Vector(prev[0], prev[1], 0)

def λ(sample) -> bool:
    return A.verify(sample, scene) and B.verify(sample, scene)

