from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# TODO: Right now the ball automatically gets recieved by AI when near ball, 
# instead make an action or paramter that enables this when we want

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (4, 2, .1), position = (5, -1.5, 0))

behavior opponent1Behavior():
    try:
        do InterceptBall(ball)
        do Idle() 
    interrupt when (self.gameObject.ballPossession):
        print("first interrupt")
        do Idle() for 1 seconds
        do GroundPassFast(opponent2.position)
        do Idle() for 0.5 seconds
        do ApproachGoal(pt)
    interrupt when (distance from self to pt < 1 and self.gameObject.ballPossession):
        print("second interrupt")
        option = Uniform(1, 2, 3)
        # The finishing shot will be skewed left, center, or right
        if (option == 1):
            do ShootBall(goal.position, "left-middle")
        elif (option == 2):
            do ShootBall(goal.position, "center-middle")
        elif (option == 3):
            do ShootBall(goal.position, "right-middle")
        abort
    
behavior opponent2Behavior():
    try:
        do Idle()
    interrupt when (self.gameObject.ballPossession):
        do Idle() for 1 seconds
        do GroundPassFast(pt.position)
        abort


# behavior coachBehavior():
    
#     do moveTo(self, Coordinate(CoordinateInit.RELATIVE, ref = [opponent1, opponent2, goal]).weighted({opponent1: 1, opponent2: 0.6, goal: 0.6}), MovingStyle.RUN, Speed(SpeedInit.MAGNITUDE))

#     print("coach moveTo done")

#     do Idle()
    
    # try:
    #     print("coachBehavior started")
    #     do Idle()
    # interrupt when (hasBallPosession(opponent1) or hasBallPosession(opponent2)):
    #     print("coachBehavior moveTo called")
    #     do moveTo(self, Coordinate(CoordinateInit.RELATIVE, ref = [opponent1, opponent2, goal]).weighted({opponent1: 1, opponent2: 0.6, goal: 0.6}), MovingStyle.RUN, Speed(SpeedInit.MAGNITUDE))


test = False
spawn_range = Range(0,0.1)
ego = new Human at (5, spawn_range, 0)
ball = new Ball at ego offset by Range(-4, 4) @ Range(4, 4.5)
pt = new Point in penalty_box
goal = new Goal behind ego by Range(2.9,3), facing away from ego

opponent1 = new Player offset by (Range(-4,4), Range(4,6)),
                facing toward ego,
                with behavior opponent1Behavior(),
                with name "opponent1"

opponent2 = new Player left of opponent1 by Range(3,5), 
                    facing toward opponent1,
                    with behavior opponent2Behavior(),
                    with name "opponent2"

require(ego can see opponent1)
require(distance from ego to opponent1 < 10)
terminate when (ego.gameObject.stopButton)