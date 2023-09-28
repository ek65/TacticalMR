from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion

# TODO: Right now the ball automatically gets recieved by AI when near ball, 
# instead make an action or paramter that enables this when we want

# TODO: check why try/interrupt statements arent working

# TODO: make penalty_box and goal_post models that can be spawned in the Unity simulation as well,
# temporarily trimesh box 3d mesh volume regions
# currently positions hardcoded to align with the objects in the Unity scene

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (13, 7, .1), position = (0, -44, 0))
goal_post = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (7.5, 2.5, .1), position = (0, -48.5, 0))

behavior opponent1Behavior():
    # try:
    #     do InterceptBall(ball)
    #     do Idle() for 1 seconds
    # interrupt when ((distance from ego to self) < 1):
    #     do GroundPassFast(opponent2.position)
    #     do RunTo(Point in penalty_box)

    # interrupt when ((self in penalty_box) and (self.gameObject.ballPossession)):
    #     take Shoot()

    while ((distance from ego to self) > 5):
        do InterceptBall(ball)

    do Idle() for 1 seconds
    do GroundPassFast(opponent2.position)
    do Idle() for 1 seconds

    while (not (self in penalty_box)) and (not (self.gameObject.ballPossession)):
        do MoveTo(pt)

    option = Uniform([1, 2, 3])
    if (option == 1):
        do ShootBall(Vector(0, 50, 0), "center-left")
    elif (option == 2):
        do ShootBall(Vector(0, 50, 0), "center-middle")
    elif (option == 3):
        do ShootBall(Vector(0, 50, 0), "center-right")
    
behavior opponent2Behavior():
    # try:
    #     do Idle()
    # interrupt when (self.gameObject.ballPossession):
    #     take PassTo(opponent1)
    while not self.gameObject.ballPossession and (distance from opponent1 to pt > 0.5):
        print(distance from opponent1 to pt)
        do Idle() for 3 seconds
    while not self.gameObject.ballPossession and not opponent1.gameObject.ballPossession:
        do InterceptBall(ball)
        
    do GroundPassFast(opponent1.position)


ego = new Human at (0, -40, 0)
ball = new Ball ahead of ego by 5
pt = new Point in penalty_box

opponent1 = new Player ahead of ego by Range(5, 10), 
                    facing toward ego,
                    with behavior opponent1Behavior()

opponent2 = new Player right of ego by Range(5, 10), 
                    facing toward opponent1,
                    with behavior opponent2Behavior

require (distance from ego to goal_post) < 10