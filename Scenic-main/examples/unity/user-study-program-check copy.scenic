# COMMENTING OUT FILE FOR NOW FOR FACTORY SETTING
# ####Environment Behavior START####
# # Parameters for variance
# coach_start_dist = Range(5, 6)  # initial distance from teammate
# opponent_dist = Range(4, 6)         # distance behind coach

# # Behaviors
# behavior TeammatePass():
#     # Double checking gotBall to ensure the pass is triggered correctly
#     # since MoveToBallAndGetPossession() might get interrupted
#     gotBall = False
#     try:
#         do Idle() for 1.0 seconds  # Give coach time to start 
#         do MoveToBallAndGetPossession()
#         print("got ball")
#         gotBall = True
#         do Idle()
#     interrupt when ego.triggerPass and self.gameObject.ballPossession and gotBall:
#         ego.triggerPass = False
#         print("trigger pass")
#         do Idle() for 1.0 seconds
#         do Pass(ego.xMark)
#         # Idle after the pass happens
#         do Idle() for 1.0 seconds
        
#         # move forward to opposite side of field
#         # Determine which side coach and opponent are on
#         coach_x = ego.position.x
#         opponent_x = opponent.position.x
        
#         # Calculate target position on opposite side
#         # X-axis ranges from -10 to +10, with 0 at center
#         # If coach and opponent are on positive side, go to negative side
#         # If coach and opponent are on negative side, go to positive side
#         if coach_x > 0 and opponent_x > 0:
#             # Both on positive side (right), go to negative side (left)
#             target_x = -6.0
#         elif coach_x < 0 and opponent_x < 0:
#             # Both on negative side (left), go to positive side (right)
#             target_x = 6.0
#         else:
#             # Mixed positions, go to the side with more space
#             # If coach is on left (negative), go right (positive)
#             # If coach is on right (positive), go left (negative)
#             target_x = 6.0 if coach_x < 0 else -6.0
        
#         # Move forward to the target position (toward goal, so positive Y)
#         target_position = Vector(target_x, ego.position.y, 0)
#         do MoveToBehavior(target_position, distance=0.5)
#         do Idle() for 1.0 seconds

#         do Idle() until self.gameObject.ballPossession
#         do Shoot(goal)
#         do Idle() for 1.0 seconds
#         do Shoot(goal)

#     do Idle()

# behavior OpponentFollowCoach():

#     do Idle() until ego.gameObject.ballPossession
    
#     # Set opponent speed
#     do SetPlayerSpeed(4.0)
    
#     while distance from ego to self > 4:
#         # Follow coach only until coach receives the ball
#         do MoveToBehavior(ego.position, distance=4)
            
    





# # Place teammate (AI) at origin
# teammate = new Player at (0, 0, 0), with name "teammate", with team "blue", with behavior TeammatePass()

# # Place coach (human) in front of teammate
# ego = new Coach ahead of teammate by coach_start_dist, 
#     with name "Coach", 
#     with team "blue", 
#     with behavior CoachBehavior(),
#     with xMark Vector(0, 0, 0),  # Set initial xMark position
#     with triggerPass False  # Initialize triggerPass to False

# # Place opponent ahead of coach (closer to goal than coach)
# opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# # Ball at teammate's feet
# ball = new Ball ahead of teammate by 0.5

# goal = new Goal at (0, 17, 0)

# terminate when (ego.gameObject.stopButton)