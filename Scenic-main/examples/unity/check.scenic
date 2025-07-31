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
opponent_dist = Uniform(4, 7)         # distance behind coach
opponent_speed = 2        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    # Double checking gotBall to ensure the pass is triggered correctly
    # since MoveToBallAndGetPossession() might get interrupted
    gotBall = False
    try:
        do Idle() for 1.0 seconds  # Give coach time to start 
        do MoveToBallAndGetPossession()
        print("got ball")
        gotBall = True
        do Idle()
    interrupt when ego.gameObject.triggerPass and self.gameObject.ballPossession and gotBall:
        print("trigger pass")
        do Idle() for 1.0 seconds
        do Pass(ego.gameObject.xMark)
        # Idle after the pass happens
        do Idle() for 2.0 seconds
        
        # Wait to receive ball back from coach
        do Idle() until self.gameObject.ballPossession
        
        # When receiving ball back, move forward to opposite side of field
        if self.gameObject.ballPossession:
            # Determine which side coach and opponent are on
            coach_x = ego.position.x
            opponent_x = opponent.position.x
            
            # Calculate target position on opposite side
            # X-axis ranges from -10 to +10, with 0 at center
            # If coach and opponent are on positive side, go to negative side
            # If coach and opponent are on negative side, go to positive side
            if coach_x > 0 and opponent_x > 0:
                # Both on positive side (right), go to negative side (left)
                target_x = -6.0
            elif coach_x < 0 and opponent_x < 0:
                # Both on negative side (left), go to positive side (right)
                target_x = 6.0
            else:
                # Mixed positions, go to the side with more space
                # If coach is on left (negative), go right (positive)
                # If coach is on right (positive), go left (negative)
                target_x = 6.0 if coach_x < 0 else -6.0
            
            # Move forward to the target position (toward goal, so positive Y)
            target_position = Vector(target_x, 10.0, 0)
            do MoveToBehavior(target_position, distance=0.5)
            do Idle() for 1.0 seconds
    
    do Idle()

behavior OpponentFollowCoach():
    do Idle() for 1.0 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    
    # Track if we've already made a decision when ego received the ball
    decision_made = False
    go_to_coach = False
    
    while True:
        # Follow coach and maintain 5m distance from ego
        if distance from self to ego > 5.5:
            do MoveToBehavior(ego.position, distance=5)
        elif distance from self to ego < 4.5:
            do MoveToBehavior(ego.position, distance=5)
        else:
            do Idle() for 0.1 seconds
            
        # Check if ego has received the ball and we haven't made a decision yet
        if ego.gameObject.ballPossession and not decision_made:
            # Uniformly decide whether to go to coach or stay on radius
            decision = Uniform(0, 1)
            go_to_coach = (decision < 0.5)
            decision_made = True
            
            if True:
                do Idle() for .75 seconds
                # Go to coach (closer distance)
                print('Attack!')
                do MoveToBehavior(ego.position, distance=2)
                do InterceptBall()
            else:
                # Stay on current radius (3.5m from ego)
                do Idle() for 0.1 seconds
                # Continue maintaining 3.5m distance
                while ego.gameObject.ballPossession:
                    if distance from self to ego > 5:
                        do MoveToBehavior(ego.position, distance=5)
                    else:
                        do Idle() for 0.1 seconds
                        
        # If ego no longer has the ball, reset decision tracking
        if not ego.gameObject.ballPossession:
            decision_made = False
            go_to_coach = False
            
        # If opponent is close to coach and ball is nearby, intercept it
        if distance from self to ego <= 1.0 and distance from self to ball <= 1.0:
            do InterceptBall()

# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "blue", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Human ahead of teammate by coach_start_dist, with name "Coach", with team "blue"

# Place opponent ahead of coach (further from goal than coach)
opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)

line = new Line at (0, 10, 0)

terminate when (ego.gameObject.stopButton)
