from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior coachBehavior():
        pass

behavior humanBehavior():
        pass


ego = new Robot at (4.5, 1.5, 0), 
        with behavior robotBehavior()

human = new NPC at (-7, -8.5, 0), 
        with behavior humanBehavior()