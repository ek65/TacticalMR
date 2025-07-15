from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

from scenic.core.regions import MeshVolumeRegion

behavior OpponentBehavior():
    while True:
        if hasBallPosession(ego):
            if distance from self to ego > 2.0:
                do MoveToBehavior(ego.position, distance=2.0)
            else:
                do Idle() for 0.1 seconds 
        elif hasBallPosession(teammate):
            if distance from self to teammate > 2.0:
                do MoveToBehavior(teammate.position, distance=2.0)
            else:
                do Idle() for 0.1 seconds 
        else:
            do Idle() until hasBallPosession(ego) or hasBallPosession(teammate)

behavior TeammateBehavior():
    while True:
        do Idle() until hasBallPosession(self)
        dist = distance from self to opponent
        has_path = HasPath({'obj1': 'Teammate', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.5}})
        if dist < 3 or not has_path:
            do Pass(ego)
        else:
            do Shoot(goal)
    

ego = new Human at (-4, 10, 0), with name "Coach"

ball = new Ball ahead of ego by 0.25

teammate = new Player at (4, 10, 0), with name "Teammate", with behavior TeammateBehavior

opponent = new Player at (Uniform(-3, 3), Uniform(11, 13), 0), with name "Opponent", with behavior OpponentBehavior

goal = new Goal at (0, 17, 0)

terminate when (ego.gameObject.stopButton)