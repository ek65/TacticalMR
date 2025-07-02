from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

from scenic.core.regions import MeshVolumeRegion

import trimesh
import random
import math

# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do GetBallPossession(ball)
    print("got ball")
    do Idle() for 1.0 seconds
    do Pass(coach)
    do Idle()

behavior OpponentFollowCoach():
    do Idle() for 0.5 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    while True:
        do MoveToBehavior(coach.position)
        do Idle() for 0.1 seconds

# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "green", with behavior TeammatePass()

# Place coach (human) in front of teammate
coach = new Human ahead of teammate by coach_start_dist, with name "coach", with team "blue"

# Place opponent ahead of coach (further from goal than coach)
opponent = new Player ahead of coach by opponent_dist, facing toward coach, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 10, 0)

