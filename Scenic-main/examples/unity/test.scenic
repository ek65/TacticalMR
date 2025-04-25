from scenic.simulators.unity.actions import *
from scenic.simulators.unity.constraints import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

import numpy as np

def movesToward(player1, player2):
    dist1 = distance from player1.prevPosition to player2.prevPosition
    dist2 = distance from player1.position to player2.position
    return dist2 < dist1

behavior opponent1Behavior():
    do Idle() until ego.gameObject.ballPossession
    while True:
        do MoveToBehavior(ball, distance = 3)

behavior TeammateBehavior():
    try:
        do Idle()
    interrupt when (ego.position.y > opponent.position.y):
        print("ego ahead of opponent")
        do PassTo(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()

behavior GetBall():
    while not self.gameObject.ballPossession:
        take MoveToAction(ball.position)

def GetBehind(player): # similar logic as inzone
    point = new Point behind player by 5
    return point

def teammateHasBallPossession():
    for obj in simulation().objects:
        if isinstance(obj, Player) and obj.team == "blue" and obj.gameObject.ballPossession:
            return True
    return False

behavior ReceiveBall():
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()

behavior CoachBehavior():
    scene = simulation()
    scene.egoObject = ego
    do Idle() for 1 seconds

    do MoveAs(target()) until termination()

ego = new Coach at (0,-3,0),
        with behavior CoachBehavior()

ball = new Ball ahead of ego by 5

opponent = new Player ahead of ego by 10,
                    facing toward ego,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()

goal = new Goal behind opponent by 5, facing away from ego

teammate = new Player offset by (Uniform(-5,5), 7), 
                with name "teammate",
                with team "blue",
                with behavior TeammateBehavior()

terminate when (ego.gameObject.stopButton)

# Constraints

T1 = _CloseTo({
    'obj': 'coach',
    'ref': 'teammate',
    'max': 1
})

E1 = _CloseTo({
    'obj': 'coach',
    'ref': 'teammate',
    'max': 2
})

T2 = _HeightRelation({
    'obj': 'coach',
    'relation': 'above',
    'height_threshold': {
        'avg': 10.0,
        'std': 2.0
    }
})

T3 = _SideRelation({
    'obj': 'coach',
    'relation': 'left',
    'horizontal_threshold': {
        'avg': 7.0,
        'std': 2.0
    }
})

T4 = _ClearLine({
    'obj1': 'coach',
    'obj2': 'teammate',
    'path_width': {
        'avg': 5.0,
        'std': 1.0
    }
})

T5 = _DistanceTo({
    'from': 'coach',
    'to': 'opponent',
    'min': {
        'avg': 2.0,
        'std': 1.0
    },
    'max': {
        'avg': 4.0,
        'std': 1.0
    },
    'operator': 'within'
})

T6 = _InZone({
    'obj': 'coach',
    'zone': 'A1'
})

T7 = _InZone({
    'obj': 'coach',
    'zone': 'D2'
})

def target():
    cond = T1
    return cond.dist(simulation(), ego=True)

def termination():
    cond = E1
    return cond.bool(simulation())




