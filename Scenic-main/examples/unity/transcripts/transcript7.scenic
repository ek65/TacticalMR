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
footed = DiscreteRange(-1, 1)


behavior opponent1Behavior(pt):
    do Idle()

behavior teammateBehavior():
    try:
        do Idle()

    interrupt when hasBallPosession(self):
        #pos = inBetween(opponent, opponent2)
        do Idle() for 1 seconds
        do PassTo(ego.position)
        do Idle() for 0.5 seconds
        do MoveTo(Vector(teammate.position.x + 2, teammate.position.y, teammate.position.z)) for 1 seconds
        do MoveTo(Vector(3.3,7,0))
        do Idle()
    
# egoY = Range(4,4)
ego = new Human at (0,0,0)

pt = new Point at (0,-10,0)

opponent = new Player at (-4.5,6,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_A"

opponent2 = new Player at (-0.5,6,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_B"

opponent3 = new Player at (3.5,6,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_C"

opponent4= new Player at (7.5,6,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_D"

teammate = new Player at (-4.2,3.5,0),
            facing toward ego,
            with behavior teammateBehavior(),
            with name "teammate",
            with team "blue"

ball = new Ball ahead of ego 

goal= new Goal at (0,-16,0), 
    with name "goal",
    facing away from pt

terminate when (ego.gameObject.stopButton)


    
    

            
