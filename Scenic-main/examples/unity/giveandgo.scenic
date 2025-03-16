from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random


behavior opponent1Behavior():
    do Idle() until ego.gameObject.ballPossession
    while True:
        do MoveTo(ball, distance = 4)

behavior TeammateBehavior():
    try:
        do Idle()
    interrupt when (ego.position.y > opponent.position.y):
        print("ego ahead of opponent")
        point = new Point ahead of goal by 3
        do PassTo(ego, slow=False)
        # take GroundPassSlowAction(goal)
        # do Idle() for 0.5 seconds
        # take StopAction()

# test = False
ego = new Human at (0, 0)
ball = new Ball ahead of ego by 1

opponent = new Player ahead of ego by 7,
                    facing toward ego,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()

goal = new Goal behind opponent by 5, facing away from ego

teammate = new Player offset by (Uniform(-5,5), 5), 
                with name "teammate",
                with team "blue",
                with behavior TeammateBehavior()

terminate when (ego.gameObject.stopButton)