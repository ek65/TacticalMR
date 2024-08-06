from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# TODO: Right now the ball automatically gets recieved by AI when near ball, 
# instead make an action or paramter that enables this when we want

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (4, 2, .1), position = (5, -1.5, 0))

behavior opponent1Behavior(pt):
    first_possession = False
    try:
        do InterceptBall(ball)
        do Idle() 

    interrupt when (not first_possession and self.gameObject.ballPossession and ego.gameObject.pause == False):
        print("1st interrupt")
        do Idle() for 1 seconds
        do GroundPassFast(opponent2.position)
        do Idle() for 0.5 seconds
        first_possession = True
        do ApproachGoal(pt)

    interrupt when (first_possession and self.gameObject.ballPossession and ego.gameObject.pause == False):
        print("2nd interrupt")
        do ShootBall(goal.position, "center-middle")
        abort
    
behavior opponent2Behavior(pt):
    try:
        do Idle()
    interrupt when (self.gameObject.ballPossession):
        print("opponent2 interrupt")
        do Idle() for 0.5 seconds
        do GroundPassFast(pt.position)
        do Idle() for 0.5 seconds
        abort



ego = new Human at (5, Range(0,0.1), 0)

goal = new Goal behind ego by Range(2.9,3), facing away from ego
pt = new Point offset by (Range(-3,3), Range(-1,0))

opponent1 = new Player offset by (Range(-4,0), Range(6,10)),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent1"

pt1 = new Point offset by (Range(3,5), Range(1,3))
pt2 = new Point offset by (Range(-5,-3), Range(1,3))
op2_pos = Uniform(pt1, pt2)

opponent2 = new Player at op2_pos, 
                    facing toward opponent1,
                    with behavior opponent2Behavior(pt),
                    with name "opponent2"

ball = new Ball ahead of opponent1 by Normal(2,1)

require (distance from op2_pos to pt) > 5
terminate when (ego.gameObject.stopButton)