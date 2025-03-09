from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# behavior robotBehavior():
#         do Idle() for 3 seconds
#         take PickUpAction()
#         do Idle() for 4 seconds
#         do MoveToRobot(Vector(9.92, -55.055, 0.77))
#         do Idle() for 0.5 seconds
#         take PutDownAction(Vector(9.92, -55.055, 0.77))
#         do Idle() for 6 seconds

behavior humanBehavior():
        pass


ego = new RobotCoach at (18.122, -50.207, 0)

worker1 = new Robot at (9, -58, 0),
        facing ego

worker2 = new Robot at (9, -67, 0),
        facing ego 

