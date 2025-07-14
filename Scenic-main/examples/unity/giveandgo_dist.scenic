import random
import numpy as np
from scenic.simulators.unity.actions import *
from scenic.simulators.unity.constraints import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

behavior opponentBehavior():
    do Idle() until termination(E2)
    do Idle() for 1 seconds
    do MoveAs(target(T6))
    do Idle()

behavior teammateBehavior():
    do GetBallPossession(ball)
    do Idle() until termination(T1)
    do Idle() for 1 seconds
    do Pass(ego)
    do Idle()

behavior coachBehavior():
    do Idle() until termination(E1)
    do MoveAs(target(T1 & T2 & T3))
    do Idle() until termination(E2)
    do MoveAs(target(T4 & T5))
    do Shoot(goal)
    do Idle()

ego = new Coach at (0, 11, 0),
        facing 180 deg,
        with behavior coachBehavior()

teammate = new Player at (0, 0, 0),
        with name 'teammate',
        with team 'blue',
        with behavior teammateBehavior()

ball = new Ball ahead of teammate by 1

opponent = new Player ahead of teammate by 3,
        facing toward teammate,
        with name 'opponent',
        with team "red",
        with behavior opponentBehavior()

goal = new Goal behind ego by 2,
        facing toward ego

terminate when (ego.gameObject.stopButton)

# Constraints

E1 = HasBallPossession({
    'player': 'teammate'
})

E2 = HasBallPossession({
    'player': 'coach'
})

T1 = HasPath({
    'obj1': 'coach',
    'obj2': 'teammate',
    'path_width': {
        'avg': 1.0,
        'std': 1.0
    }
})

T2 = HeightRelation({
    'obj': 'coach',
    'ref': 'teammate',
    'relation': 'ahead',
    'height_threshold': {
        'avg': 2.0,
        'std': 1.0
    }
})

T3 = CloseTo({
    'obj': 'coach',
    'ref': 'teammate',
    'max': 4
})

T4 = CloseTo({
    'obj': 'coach',
    'ref': 'goal',
    'max': 3
})

T5 = HeightRelation({
    'obj': 'coach',
    'ref': 'goal',
    'relation': 'below',
    'height_threshold': {
        'avg': 2.5,
        'std': 1.0
    }
})

T6 = CloseTo({
    'obj': 'opponent',
    'ref': 'coach',
    'max': 2
})

termination = lambda x: x.bool(simulation())
target = lambda x: x.dist(simulation(), ego=True)




