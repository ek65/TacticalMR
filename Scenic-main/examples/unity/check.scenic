from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

from scenic.core.regions import MeshVolumeRegion

import trimesh
import random
import math

# Parameters for variance
teammate_start_dist = Uniform(5, 8)  # initial distance from ego
teammate_check_dist = Uniform(4, 6)   # how much closer teammate checks
teammate_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind teammate
opponent_speed = Uniform(5, 7)        # opponent's movement speed

def teammate_check_point():
    # Calculate the check point for the teammate
    return teammate.position + ( -teammate_check_dist @ teammate_check_angle )

# Behaviors
behavior EgoPass():
    pi = 3.1415
    check_dx = -teammate_check_dist * cos(teammate_check_angle * pi / 180)
    check_dy = -teammate_check_dist * sin(teammate_check_angle * pi / 180)
    check_point = Vector(teammate.position.x + check_dx, teammate.position.y + check_dy, teammate.position.z)
    do Idle() until distance from teammate.position to check_point < 0.5  # Give teammate time to start moving
    do Pass(teammate)
    do Idle()


behavior TeammateCheckAndReceive():
    do Idle() for 1.0 seconds
    pi = 3.1415
    # Compute direction from teammate to ego
    dir_x = ego.position.x - self.position.x
    dir_y = ego.position.y - self.position.y
    norm = math.sqrt(dir_x**2 + dir_y**2)
    dir_x /= norm
    dir_y /= norm
    # Rotate by check angle
    theta = teammate_check_angle * pi / 180
    rot_x = dir_x * cos(theta) - dir_y * sin(theta)
    rot_y = dir_x * sin(theta) + dir_y * cos(theta)
    # Move by check distance
    check_dx = teammate_check_dist * rot_x
    check_dy = teammate_check_dist * rot_y
    check_point = Vector(self.position.x + check_dx,
                         self.position.y + check_dy,
                         self.position.z)
    self.last_check_dx = check_dx
    self.last_check_dy = check_dy
    do MoveToBehavior(check_point)
    do Idle() for 2.0 seconds
    if distance from self to opponent < 3:
        do Pass(ego)
    else:
        # Move forward by 2 units in the check direction
        dir_norm = math.sqrt(check_dx**2 + check_dy**2)
        if dir_norm == 0:
            dribble_dx = 2
            dribble_dy = 0
        else:
            dribble_dx = (check_dx / dir_norm) * 2
            dribble_dy = (check_dy / dir_norm) * 2
        dribble_to_point = Vector(self.position.x, self.position.y + 2, self.position.z)
        do DribbleTo(dribble_to_point)
    do Idle()

behavior OpponentFollow():
    do Idle() for 0.5 seconds  # Wait for teammate to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    # Wait until teammate has set last_check_dx
    while not hasattr(teammate, 'last_check_dx'):
        do Idle() for 0.1 seconds
    opp_dx = teammate.last_check_dx
    opp_dy = teammate.last_check_dy
    opponent_check_point = Vector(self.position.x + opp_dx, self.position.y + opp_dy, self.position.z)
    do MoveToBehavior(opponent_check_point)
    do Idle()

# Place ego at origin
ego = new Human at (0, 0, 0), with behavior EgoPass()

# Place teammate directly in front of ego (no angle)
teammate = new Player ahead of ego by teammate_start_dist, with name "teammate", with team "green", with behavior TeammateCheckAndReceive()

# Place opponent behind teammate (further from goal than teammate)
opponent = new Player ahead of teammate by opponent_dist, facing toward teammate, with name "opponent", with team "red", with behavior OpponentFollow()

# Ball at ego's feet
ball = new Ball ahead of ego by 0.5

goal = new Goal at (0, 15, 0) 

terminate after 10 seconds