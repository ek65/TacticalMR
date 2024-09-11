from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random



penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (4, 2, .1), position = (5, -1.5, 0))

behavior opponent3Behavior(pt):
    try:
        do InterceptBall(ball)
        do Idle() 

    interrupt when (self.gameObject.ballPossession):
        print("1st interrupt")
        do Idle() for 1 seconds
        do GroundPassFast(opponent1.position)
        do Idle() for 0.5 seconds
        abort

behavior opponent1Behavior(pt):
    try:
        
        do Idle() 

    interrupt when (self.gameObject.ballPossession):
        print("1st interrupt")
        do Idle() for 1 seconds
        do GroundPassFast(opponent2.position)
        do Idle() for 0.5 seconds
        abort
behavior coachBehavior():
    try:
        do Idle() 
    interrupt when (opponent3.gameObject.ballPossession):
        do Idle() for 1 seconds
        do MoveTo(opponent1.position)
        do Idle() for 0.5 seconds
        abort

behavior teammateBehavior():
    try:
        do Idle() 
    interrupt when (opponent1.gameObject.ballPossession):
        do Idle() for 1 seconds
        do MoveTo(opponent2.position)
        do Idle() for 0.5 seconds
        abort

ego = new Coach at (5, Range(0,0.1), 0), with behavior coachBehavior()

pt1 = new Point offset by (Range(3,5), Range(1,3))
pt2 = new Point offset by (Range(-5,-3), Range(1,3))
pt3 = new Point offset by (Range(5,6), Range(3,4))
pt4 = new Point offset by (Range(-4,0), Range(6,10))

op2_pos = Uniform(pt1, pt2)
op3_pos = Uniform(pt3, pt4)


goal = new Goal behind ego by Range(2.9,3), facing away from ego
pt = new Point offset by (Range(-3,3), Range(-1,0))


opponent1 = new Player offset by (3, 4, 0),
                facing toward ego,
                with behavior opponent1Behavior(pt),
                with name "opponent1"

opponent2 = new Player at (-1, 3, 0),
                    facing toward opponent1,
                    with name "opponent2"

defender1 = new Player at (2, Range(0,0.1), 0),
                facing toward opponent2,
                with team "blue",
                with name "defender1",
                with behavior teammateBehavior()

opponent3 = new Player offset by (Range(-4,0), Range(6,10)), 
                    facing toward opponent1,
                    with behavior opponent3Behavior(pt),
                    with name "opponent3"   
             

ball = new Ball ahead of opponent3 by Normal(2,1)


require (distance from op2_pos to pt) > 5
terminate when (ego.gameObject.stopButton)