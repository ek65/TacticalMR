from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random


behavior opponent1Behavior():
    try:
        do InterceptBall(ball)
        do Idle() 
    interrupt when ((distance from ego to self) < 4 and self.gameObject.ballPossession and distance from self to pt > 0.1):
        do Idle() for 0.5 seconds
        ego.gameObject.pause = True
        test = True
        do Idle() for 2 seconds
        ego.gameObject.pause = False
        do Idle() for 2 seconds


# test = False
ego = new Human at (0, 0)
ball = new Ball at (3,4)
goal = new Goal behind ego by Range(1,2), facing away from ego


opponent1 = new Player at (1,9),
                    facing toward ego,
                    with behavior opponent1Behavior()


terminate when (ego.gameObject.stopButton)