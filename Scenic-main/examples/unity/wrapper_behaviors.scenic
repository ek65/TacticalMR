from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (4, 2, .1), position = (5, -1.5, 0))
timestep = 0.1
pt = new OrientedPoint at (0,0,0)

FIELD_WIDTH, FIELD_HEIGHT = 20, 34
NUM_ZONES_X, NUM_ZONES_Y = 4, 5
ZONE_WIDTH = FIELD_WIDTH / NUM_ZONES_X
ZONE_HEIGHT = FIELD_HEIGHT / NUM_ZONES_Y

def sample_target(prev_target : Vector, scene, λ_dest : list, λ_always = None : list) -> Vector:
    i = 0
    sample = [prev_target.x, prev_target.y]

    while not λ(sample, scene, λ_dest):

        x = Range(-FIELD_WIDTH / 2, FIELD_WIDTH / 2)
        y = Range(-FIELD_HEIGHT / 2, FIELD_HEIGHT / 2)
        sample = [x,y]

        if i > 1000:
            raise Exception("Maximum sample depth exceeded.")
        i += 1

    target = Vector(sample[0], sample[1], 0)
    return target

def λ(sample, scene, λ_dest : list) -> bool:
    for i in range(len(λ_dest)):
        if not λ_dest[i].verify(sample, scene):
            return False
    return True

behavior MoveTo(λ_dest : list):
    scene = simulation()
    target = Vector(0, 0, 0)
	target = sample_target(target, scene, λ_dest)
	while (distance from self to target > 0.5){
		do moveTo(target) for timestep seconds
		target = sample_target(target, scene, λ_dest)
	}
	do Idle()

behavior MoveTo(λ_dest : list, λ_always : list):
    scene = simulation()
    target = Vector(0, 0, 0)
	target = sample_target(target, scene, λ_dest, λ_always)
	while (distance from self to target > 0.5){
		do moveTo(target) for timestep seconds
		target = sample_target(target, scene, λ_dest, λ_always)
	}
	do Idle()


behavior ThroughPass(λ_dest : list):
    scene = simulation()
    target = Vector(0, 0, 0)
    target = sample_target(target, scene, λ_dest)
    do PassTo(target)
    do Idle()