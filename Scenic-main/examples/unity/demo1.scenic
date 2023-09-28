from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion

# TODO: Right now the ball automatically gets recieved by AI when near ball, 
# instead make an action or paramter that enables this when we want

# TODO: make penalty_box and goal_post models that can be spawned in the Unity simulation as well,
# temporarily trimesh box 3d mesh volume regions
# currently positions hardcoded to align with the objects in the Unity scene

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (13, 7, .1), position = (0, -44, 0))
goal_post = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (7.5, 2.5, .1), position = (0, -48.5, 0))

behavior opponent1Behavior():
    # TODO: check why try/interrupt statements arent working
    # try:
    #     do InterceptBall(ball)
    #     do Idle() for 1 seconds
    # interrupt when ((distance from ego to self) < 1):
    #     do GroundPassFast(opponent2.position)
    #     do RunTo(Point in penalty_box)

    # interrupt when ((self in penalty_box) and (self.gameObject.ballPossession)):
    #     take Shoot()

    while ((distance from ego to self) > 3):
        do InterceptBall(ball)

    do Idle() for 1 seconds
    do GroundPassFast(opponent2.position)
    do Idle() for 1 seconds

    # TODO: see why and statments cause while loops to break, using distance check for now

    # while (not (self in penalty_box)) and (not (self.gameObject.ballPossession)):
    #     do MoveTo(pt)
    
    while (distance from self to pt > 0.5):
        do MoveTo(pt)
    
    while (distance from self to ball > 0.5):
        do Idle() for 1 seconds

    option = Uniform(1, 2, 3)
    print(option)
    if (option == 1):
        do ShootBall(Vector(0, -50, 0), "left-middle")
    elif (option == 2):
        do ShootBall(Vector(0, -50, 0), "center-middle")
    elif (option == 3):
        do ShootBall(Vector(0, -50, 0), "right-middle")
    
behavior opponent2Behavior():
    # try:
    #     do Idle()
    # interrupt when (self.gameObject.ballPossession):
    #     take PassTo(opponent1)
    while (distance from opponent1 to pt > 0.5):
        print(distance from opponent1 to pt)
        do Idle() for 1 seconds
    do GroundPassFast(opponent1.position)


ego = new Human at (0, -45, 0)
ball = new Ball ahead of ego by 3
pt = new Point in penalty_box

opponent1 = new Player ahead of ego by Range(3, 5), 
                    facing toward ego,
                    with behavior opponent1Behavior()

opponent2 = new Player right of ego by Range(3, 5), 
                    facing toward opponent1,
                    with behavior opponent2Behavior

require (distance from ego to goal_post) < 5