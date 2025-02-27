from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior robotBehavior():
        pass

behavior humanBehavior():
        pass


ego = new Robot at (17.122, -50.207, 0)

human = new Robot at (17.122, -54.1, 0)