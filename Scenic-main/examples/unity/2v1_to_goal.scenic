from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

from scenic.core.regions import MeshVolumeRegion

behavior OpponentBehavior():
    while True:
        if hasBallPosession(ego):
            do MoveToBehavior(ego.position)
        elif hasBallPosession(teammate):
            do MoveToBehavior(teammate.position)
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
