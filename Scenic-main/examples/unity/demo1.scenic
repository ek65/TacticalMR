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

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (10, 3, .1), position = (0, -1.5, 0))
# goal_post = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (7.5, 2.5, .1), position = (0, -48.5, 0))

behavior opponent1Behavior():
    try:
        do InterceptBall(ball)
        do Idle() 
    interrupt when ((distance from ego to self) < 3.5):
        do Idle() for 0.5 seconds
        do GroundPassFast(opponent2.position)
        do Idle() for 1 seconds
        abort
        
    
    while (distance from self to pt > 0.5):
        do MoveTo(pt)
    
    while (distance from self to ball > 0.5):
        do Idle() for 1 seconds

    option = Uniform(1, 2, 3)
    print(option)
    if (option == 1):
        do ShootBall(Vector(0, -11.5, 0), "left-middle")
    elif (option == 2):
        do ShootBall(Vector(0, -11.5, 0), "center-middle")
    elif (option == 3):
        do ShootBall(Vector(0, -11.5, 0), "right-middle")
    
behavior opponent2Behavior():
    try:
        do Idle()
    interrupt when (distance from opponent1 to pt < 0.5):
        do GroundPassFast(opponent1.position)
        abort


test = Range(0,0.1)
ego = new Human at (test, test, 0)
ball = new Ball ahead of ego by Range(3.5, 4)
pt = new Point in penalty_box
goal = new Goal behind ego by Range(3.9,4), facing away from ego

opponent1 = new Player ahead of ego by Range(5, 7), 
                    facing toward ego,
                    with behavior opponent1Behavior()

opponent2 = new Player right of ego by Range(4, 7), 
                    facing toward opponent1,
                    with behavior opponent2Behavior()

# require (distance from ego to goal_post) < 10

terminate when (ego.gameObject.stopButton)