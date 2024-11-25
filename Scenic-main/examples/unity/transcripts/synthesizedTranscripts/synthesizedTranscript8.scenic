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

behavior coachBehavior():
    scene = simulation()
    do MoveToWrapper(λ_dest)
    do Idle() until λ_precondition
    do PassTo(midfielder)

behavior teammateBehavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(ego):
        do MoveTo(Vector(self.position.x + 1.5, self.position.y - 1, 0)) 
        do Idle()

behavior opponent1Behavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(ego):
        do MoveTo(Vector(self.position.x - 0.5, self.position.y - 0.5, 0))
        do Idle() 

behavior opponent2Behavior():
    try: 
        do Idle()
    interrupt when hasBallPosession(ego):
        do MoveTo(Vector(self.position.x - 1.8 , self.position.y + 2, 0)) for 2 seconds 
        do Idle()


oppGoal= new Goal at (0,-16,0), 
    with name "oppGoal",
    facing away from pt

teammate = new Player at (-6.5, -9, 0),
          with team 'blue',
          with name 'teammate',
          with behavior teammateBehavior()


opponent1 = new Player at (-4, -6, 0),
        with name 'opponent1',
        with behavior opponent1Behavior()

opponent2 = new Player at (4, -12, 0),
        with name 'opponent2',
        with behavior opponent2Behavior()

ego = new Player at (3.5, 2, 0), 
        with name 'coach',
        facing oppGoal,
        with coachBehavior()

A = InZone({'zone': 'B2'})
B = HasAngle({'ref': 'leftback', 'radius': {'avg': 3.3851227232864622, 'std': 0.0}})
C = HasBallPossession({'ref': 'leftback'})

def λ_dest(scene, sample):
    return A(scene, sample) and B(scene, sample) and C(scene, sample)

D = HasBallPossession({'ref': 'coach'})
E = HasAngle({'ref': 'midfielder', 'radius': {'avg': 2.8538201912442296, 'std': 0.0}})

def λ_precondition(scene, sample):
    return D(scene, sample) and E(scene, sample)
        
ball = new Ball ahead of ego

def sample_target(prev_target, scene, λ_dest) -> Vector: 
    global target
    i = 0
    sample = [prev_target.x, prev_target.y]
    
    while not λ_dest(sample, scene):

        x = Range(-FIELD_WIDTH / 2, FIELD_WIDTH / 2)
        y = Range(-FIELD_HEIGHT / 2, FIELD_HEIGHT / 2)
        sample = [x,y]

        if i > 1000:
            raise Exception("Maximum sample depth exceeded.")
        i += 1

    target = Vector(sample[0], sample[1])
    return target

behavior MoveToWrapper(λ_dest):
    scene = simulation()
    target = Vector(0, 0)
    target = sample_target(target, scene, λ_dest)
    while (distance from self to target > 0.5):
        do MoveTo(target) for timestep seconds
        target = sample_target(target, scene, λ_dest)
    do Idle() for 1 seconds

terminate when (ego.gameObject.stopButton)