from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

from scenic.core.regions import MeshVolumeRegion

import trimesh
import random

# Parameters for variance
teammate_start_dist = Uniform(9, 12)  # initial distance from ego
teammate_check_dist = Uniform(3, 5)   # how much closer teammate checks
teammate_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(2, 5)         # distance behind teammate
opponent_speed = Uniform(3, 7)        # opponent's movement speed

# Behaviors
behavior EgoPass():
    do Idle() until distance from teammate.position to check_point < 0.5  # Give teammate time to start moving
    do PassTo(teammate)
    do Idle()


behavior TeammateCheckAndReceive():
    do Idle() for 1.0 seconds
    pi = 3.1415
    check_dx = -teammate_check_dist * cos(teammate_check_angle * pi / 180)
    check_dy = -teammate_check_dist * sin(teammate_check_angle * pi / 180)
    check_point = Vector(self.position.x + check_dx, self.position.y + check_dy, self.position.z)
    do MoveTo(check_point)
    do Idle() until self.ballPossession  # Wait for pass
    do Idle() for 1.0 seconds
    if distance from self to opponent < 3:
        do PassTo(ego)
    else:
        do LookAt(ego.heading) # heading may or may not exist
        do DribbleTo(ego.heading) for 2.0 seconds
    do Idle()

behavior OpponentFollow():
    do Idle() for 1.0 seconds  # Wait for teammate to start checking
    do SetPlayerSpeed(opponent_speed)
    # Follow the teammate at the same angle, staying directly behind
    pi = 3.1415
    opp_dx = -teammate_check_dist * cos(teammate_check_angle * pi / 180)
    opp_dy = -teammate_check_dist * sin(teammate_check_angle * pi / 180)
    opponent_check_point = Vector(self.position.x + opp_dx, self.position.y + opp_dy, self.position.z)
    do MoveTo(opponent_check_point)
    do Idle()

# Place ego at origin
ego = new Human at (0, 0, 0), with behavior EgoPass()

# Place teammate directly in front of ego (no angle)
teammate = new Player ahead of ego by teammate_start_dist, with name "teammate", with team "green", with behavior TeammateCheckAndReceive()

# Place opponent directly behind teammate (relative to ego)
opponent = new Player behind teammate by opponent_dist, facing toward teammate, with name "opponent", with team "red", with behavior OpponentFollow()

# Ball at ego's feet
ball = new Ball ahead of ego by 1

def teammate_check_point():
    # Calculate the check point for the teammate
    return teammate.position + ( -teammate_check_dist @ teammate_check_angle )

terminate when (
    (teammate.hasPassedBack and ego.gameObject.ballPossession)
    or
    (teammate.hasDribbledForward)
)
