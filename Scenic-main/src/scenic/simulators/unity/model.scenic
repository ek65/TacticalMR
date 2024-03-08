"""We need to define things here like positions, players, etc. 
This is how scenic actually grabs information.
"""
from scenic.simulators.unity.simulator import UnitySimulator
# from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.client import gameObject
from scenic.core.vectors import Orientation
param unity_map = None
param address = 'localhost'
param port = 5555
param timeout = 10
param timestep = .1
CATCH_RADIUS = 1.2
#TEAMMATE_SPEED = 2.5
#PLAYER_Y_OFFSET = 1.3
#goal_position = OrientedPoint at (25.24,-0.118,0.46)

simulator UnitySimulator(
    ip=globalParameters.address,
    port=int(globalParameters.port),
    timeout=int(globalParameters.timeout),
    render=True,
    timestep=float(globalParameters.timestep)
)
class UnityObject:
    position : (0,0,0)
    isUnityObject : True
    gameObjectType : ""
    yaw : 0 deg
    pitch : 0 deg
    roll : 0 deg
    gameObject : gameObject((0,0,0), Orientation.fromEuler(0,0,0))
    width : 0.0
    length : 0.0
    height : 0.0

class Ball(UnityObject):
    position : (0,0,1)
    gameObject : gameObject((0,0,1), Orientation.fromEuler(0,0,0))
    gameObjectType : "ball"
    width : 0.15
    length : 0.15
    height : 0.15

class Goal(UnityObject):
    gameObjectType: "goal"
    width: 1
    length : 1
    height : 1

class Player(UnityObject):
    gameObjectType : "player"
    width : 1.0
    length : 2.0
    height : 1.0
    team : "red"
    path : None
    ballPossession : False
    tacticalPosition : Vector(0, 0, 0)
    name : "Unknown"
    debug: True

class GoalKeeper(Player):
    gameObjectType : "goalkeeper"

#goalpost

class Human(UnityObject):
    gameObjectType : "human"
    width : 1.0
    length : 2.0
    height : 1.0
    team : "blue"
    path : None