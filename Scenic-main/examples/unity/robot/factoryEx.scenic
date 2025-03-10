from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior worker1Behavior():
        do Idle() until (distance from self to ego < 2)
        do Idle() for 3 seconds
        take PickUpAction()
        do Idle() for 4 seconds
        do MoveToRobot(Vector(10.67, -65, 0))
        do Idle() for 0.5 seconds
        take PutDownAction(Vector(10.67, -65, 1.5))
        do Idle() for 6 seconds

behavior humanBehavior():
        pass


ego = new RobotCoach at (18.122, -50.207, 0), with name "Coach"

worker1 = new Robot at (9.9, -58, 0),
        facing ego, with name "worker1",
        with behavior worker1Behavior()

worker2 = new Robot at (9, -67, 0),
        facing ego, with name "worker2"

