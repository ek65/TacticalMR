from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior Follow(obj):
    while True:
        do MoveToBehavior(obj, distance = 3, status = f"Follow {obj.name}")

behavior opponent1Behavior():
    do Idle() until teammate.gameObject.ballPossession
    do Follow(ball) until ego.gameObject.ballPossession
    do Uniform(Follow(ego), Follow(teammate))
    # do Follow(teammate)
    # print("opponent follows ego")
    # do Follow(ego)

A = HasPath({'obj1': 'teammate', 'obj2': 'coach', 'path_width':{'avg': 2, 'std':1}})

behavior TeammateBehavior():
    passed = False
    try:
        do MoveToBallAndGetPossession(ball)
        do Idle()
    interrupt when (not passed and self.gameObject.ballPossession):
        do Idle() for 2.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        # do MoveToBehavior(point) until MakePass({'player': 'coach'})(simulation(), None)
        # do Idle() for 1 seconds
        # do MoveToBallAndGetPossession(ball)
        # do Shoot(goal)
        passed = True

teammate = new Player at (0,0), 
                with name "teammate",
                with team "blue",
                with behavior TeammateBehavior()

ball = new Ball ahead of teammate by 1

opponent = new Player ahead of teammate by 5,
                    facing toward teammate,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()

ego = new Human behind opponent by 5, 
            facing toward teammate,
            with name "Coach",
            with team "blue"

goal = new Goal behind opponent by 10, facing away from ego

terminate when (ego.gameObject.stopButton)