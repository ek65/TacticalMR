from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# TODO: Right now the ball automatically gets recieved by AI when near ball, 
# instead make an action or paramter that enables this when we want

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (10, 2, .1), position = (0, -1.5, 0))

behavior opponent1Behavior():
    try:
        do InterceptBall(ball)
        do Idle() 
    interrupt when ((distance from ego to self) < 3 and self.gameObject.ballPossession and distance from self to pt > 0.1):
        do Idle() for 0.5 seconds
        do GroundPassFast(opponent2.position)
        do Idle() for 1 seconds
        do MoveTo(pt)
    interrupt when (distance from self to pt < 0.5 and self.gameObject.ballPossession):
        do Idle() for 1 seconds
        option = Uniform(1, 2, 3)
        # The finishing shot will be skewed left, center, or right
        if (option == 1):
            do ShootBall(goal.position, "left-middle")
        elif (option == 2):
            do ShootBall(goal.position, "center-middle")
        elif (option == 3):
            do ShootBall(goal.position, "right-middle")
    
behavior opponent2Behavior():
    try:
        do Idle()
    interrupt when (distance from opponent1 to pt < 0.5):
        do GroundPassFast(opponent1.position)
        abort


spawn_range = Range(0,0.1)
ego = new Human at (spawn_range, spawn_range, 0)
ball = new Ball at ego offset by Range(-1, 1) @ Range(3, 3.5)
pt = new Point in penalty_box
goal = new Goal behind ego by Range(2.9,3), facing away from ego

opponent1 = new Player at ball offset by Range(-1, 1) @ Range(1, 2),
                    facing toward ego,
                    with behavior opponent1Behavior()

opp2spawn = random.randint(1,2)
# opp2spawn = Unifrom(1,2)
print(opp2spawn)

if (opp2spawn == 1):
    opponent2 = new Player right of ego by Range(4, 5), 
                    facing toward opponent1,
                    with behavior opponent2Behavior()
elif (opp2spawn == 2):
    opponent2 = new Player left of ego by Range(4, 5), 
                    facing toward opponent1,
                    with behavior opponent2Behavior()

terminate when (ego.gameObject.stopButton)