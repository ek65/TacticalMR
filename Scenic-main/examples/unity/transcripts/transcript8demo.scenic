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

ego = new Human at (3.5, 2, 0), 
        with name 'coach',
        facing oppGoal
        
        
ball = new Ball ahead of ego

terminate when (ego.gameObject.stopButton)