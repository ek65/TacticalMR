from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

from scenic.core.regions import MeshVolumeRegion

import trimesh
import random

# Parameters
pressingDistance = 2.5
shootingDistance = 6.0

# Ego behavior: dribble toward goal, shoot if close enough
behavior EgoDribbleAndShoot():
    while distance from self to goal > shootingDistance:
        do MoveTo(goal.position)
    do ShootBall(goal.position, "center-middle")
    do Idle()

# Opponent behavior: move to block ego
behavior BlockEgo():
    while True:
        do MoveTo(ego.position)

# Scene objects

ego = new Human at (0, 0, 0), facing toward goal, with behavior EgoDribbleAndShoot()
ball = new Ball ahead of ego by 1
opponent = new Player ahead of ego by 10, facing toward ego, with name "opponent", with team "red", with behavior BlockEgo()
goal = new Goal ahead of opponent by 20, facing away from ego

terminate when (goal.isScored or distance from opponent to ego < 1.0)

