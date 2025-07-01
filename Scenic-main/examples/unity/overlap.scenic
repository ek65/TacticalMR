from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

from scenic.core.regions import MeshVolumeRegion

import trimesh
import random

# Ego setup
pi = 3.1415

ego_y = 5  # a few units ahead of origin
ego = new Human at (0, ego_y, 0), facing (0, 1, 0), with name "ego"

# Ball at ego's feet
ball = new Ball at (0, ego_y + 1, 0)

# Sample a random side: -1 (left) or 1 (right)
side = DiscreteRange(-1, 1)

# Opponent: 5-7 units in front of ego, at angle 0-20 deg from y-axis, on chosen side
opp_dist = Uniform(5, 7)
opp_angle = Uniform(0, 20) * side
opp_x = opp_dist * sin(opp_angle * pi / 180)
opp_y = ego_y + opp_dist * cos(opp_angle * pi / 180)
opponent = new Player at (opp_x, opp_y, 0), facing (0, 1, 0), with team "red", with name "opponent"

# Teammate: 3-5 units behind ego, at angle 0-20 deg from y-axis, on same side
team_dist = Uniform(3, 5)
team_angle = Uniform(0, 20) * side
team_x = -team_dist * sin(team_angle * pi / 180)
team_y = ego_y - team_dist * cos(team_angle * pi / 180)

# Overlap run: 3-5 units in front of ego, 4-6 units to the same side
run_dist = Uniform(3, 5)
run_side = Uniform(4, 6) * side
run_x = run_side
run_y = ego_y + run_dist

behavior OverlapRun():
    do Idle() for 2 seconds
    do MoveToBehavior(Vector(run_x, run_y, 0))
    do Idle()

teammate = new Player at (team_x, team_y, 0), facing (0, 1, 0), with team "blue", with name "teammate", with behavior OverlapRun()

