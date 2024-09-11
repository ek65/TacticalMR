from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random


penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (4, 2, .1), position = (5, -1.5, 0))

pt1 = new Point at (0,1,0)

pt2 = new Point at (0,5,0)

pt3 = new Point at (3,8,0)

pt4 = new Point at (-3,8,0)

pt5 = new Point at (0, 3.5 ,0)

pt6 = new Point at (2, 7 ,0)

behavior opponent1Behavior():
    try:
        print("hello")
        do InterceptBall(ball)
        do Idle() 

    interrupt when (self.gameObject.ballPossession):
        print("1st interrupt")
        do Idle() for 1 seconds
        do GroundPassFast(player2.position)
        do Idle() 
        abort


ego = new Player at pt1, with behavior opponent1Behavior()
player2 = new Player at pt2
player3 = new Player at pt3
player4 = new Player at pt4


ball = new Ball ahead of ego
