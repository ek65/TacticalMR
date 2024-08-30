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

footed = DiscreteRange(-1, 1)

pressingDistance = 3.5 #Uniform(4, 5)
shootingDistance = Uniform(4, 8)
first_possession = False

behavior opponent1Behavior(pt):
    do Idle()


behavior teammateBehavior():
    try:
        do MoveTo(ball.position) 
        do Idle() for 2 seconds
        do GroundPassFast(ego.position)
        do Idle() for 3 seconds

    interrupt when hasBallPosession(self):
        do Idle() for 2 seconds 
        coachPos = ego.position
        do MoveTo(coachPos) for 1 seconds
        do GroundPassFast(coachPos)
        print("passing ball again")
        do Idle()
    
egoY = Range(4,4)
ego = new Human at (-2, egoY,0)

pt = new Point offset by (Range(-3,3), Range(-1,0))

oppY = Range(5,6)
opponent = new Player at (0,oppY,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_A"


opponent2 = new Player at (-3,oppY,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_B"

opponent3 = new Player at (3,oppY,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_C"


opponent4= new Player at (6,oppY,0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent_D"

teammate = new Player ahead of opponent2 by Range(4.5,5),
            facing toward ego,
            with behavior teammateBehavior(),
            with name "teammate",
            with team "blue"

ball = new Ball ahead of teammate 

terminate when (ego.gameObject.stopButton)