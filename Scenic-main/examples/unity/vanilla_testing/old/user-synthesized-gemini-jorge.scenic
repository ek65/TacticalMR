from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

model scenic.domains.soccer.model

behavior CoachBehavior():
    # Phase 1: Wait for Teammate to have ball
    do Speak("Waiting for teammate to gain possession of the ball.")
    # λ_precondition: none, always waiting
    λ_termination_Wait1 = lambda: HasBallPossession({'player': 'teammate'})(self.current_scene, None)
    wait until λ_termination_Wait1()

    # Phase 2: Move to an open position to receive pass
    # Using fixed coordinates from Demo 0 for a representative open spot.
    open_spot_pos = Point(-6.4, 5.2) 
    do Speak("Teammate is pressured, find open space to receive the pass!")
    λ_target_MoveTo1 = lambda pos: CloseTo({'obj': 'Coach', 'ref': open_spot_pos, 'max': 0.5})(self.current_scene, pos)
    λ_precondition_MoveTo1 = lambda: True # Assumes movement is always possible
    do MoveTo(λ_target_MoveTo1)
    # λ_termination for MoveTo is implicit based on the λ_dest and 0.5m threshold.

    # Phase 3: Get Ball Possession
    do Speak("Secure the ball once it's near to you!")
    λ_precondition_GetBallPossession = lambda: CloseTo({'obj': 'Coach', 'ref': 'ball', 'max': 0.5})(self.current_scene, None)
    λ_termination_GetBallPossession = lambda: HasBallPossession({'player': 'Coach'})(self.current_scene, None)
    do GetBallPossession()

    # Phase 4: Decision - Pass or Shoot
    # Conditions for shooting: Coach is not pressured AND Opponent is chasing teammate
    # 'min': {'avg': 4.0} indicates distance greater than 4 units
    is_coach_free = lambda: DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 4.0}})(self.current_scene, None)
    is_opponent_chasing_teammate = lambda: MovingTowards({'obj': 'opponent', 'ref': 'teammate'})(self.current_scene, None)

    if is_coach_free() and is_opponent_chasing_teammate():
        # Option 1: Shoot (demos 1, 2)
        do Speak("You are clear, advance towards the goal for a shot!")
        # Move towards goal area (y > 12.0)
        λ_target_MoveTo2 = lambda pos: HeightRelation({'obj': 'Coach', 'relation': 'ahead', 'vertical_threshold': {'avg': 12.0, 'std': 1.0}})(self.current_scene, pos)
        λ_precondition_MoveTo2 = lambda: HasBallPossession({'player': 'Coach'})(self.current_scene, None)
        do MoveTo(λ_target_MoveTo2)

        do Speak("Take the shot! Aim for the goal!")
        λ_precondition_Shoot = lambda: HasBallPossession({'player': 'Coach'})(self.current_scene, None) and CloseTo({'obj': 'Coach', 'ref': 'goal', 'max': 5.0})(self.current_scene, None)
        λ_termination_Shoot = lambda: not HasBallPossession({'player': 'Coach'})(self.current_scene, None)
        do Shoot()
    else:
        # Option 2: Pass (demo 0)
        do Speak("Pass the ball to your teammate for a through ball opportunity!")
        # 'path_width': {'avg': 1.0} indicates a clear path of at least 1 unit wide
        λ_precondition_Pass = lambda: HasBallPossession({'player': 'Coach'})(self.current_scene, None) and HasPathToPass({'passer': 'Coach', 'receiver': 'teammate', 'path_width': {'avg': 1.0}})(self.current_scene, None)
        λ_termination_Pass = lambda: not HasBallPossession({'player': 'Coach'})(self.current_scene, None) and HasBallPossession({'player': 'teammate'})(self.current_scene, None)
        do Pass(teammate)

    # After main action, wait for a short period before ending or repeating
    do Speak("Good job, await the next play!")
    # λ_precondition: none, always possible
    # λ_termination: none, fixed duration
    do Wait(2) # Wait for 2 simulation steps

scenario CoachScenario():
    Coach.behavior = CoachBehavior()
    # Objects Ball, Coach, Goal, Goal_leftpost, Goal_rightpost, Opponent, Teammate are assumed to be
    # defined and loaded by the soccer domain model or the simulation environment.


def movesToward(player1, player2):
    dist1 = distance from player1.prevPosition to player2.prevPosition
    dist2 = distance from player1.position to player2.position
    return dist2 < dist1

behavior Follow(obj):
    while True:
        do MoveToBehavior(obj, distance = 3, status = f"Follow {obj.name}")

def pressure(player1, player2):
    """
    Returns True if player1 is pressuring player2, False otherwise.
    """
    behav = player1.gameObject.behavior.lower()
    name = player2.name.lower()
    print(f"player1: {player1.name}, player2: {player2.name}, behavior: {behav}")
    if 'follow' in behav and name in behav:
        return True
    return False

behavior opponent1Behavior():
    do Idle() until teammate.gameObject.ballPossession
    do Follow(ball) until ego.gameObject.ballPossession
    # do Uniform(Follow(ego), Follow(teammate))
    do Follow(teammate)
    # print("opponent follows ego")
    # do Follow(ego)

A = HasPath({'obj1': 'teammate', 'obj2': 'coach', 'path_width':{'avg': 2, 'std':1}})

behavior TeammateBehavior():
    passed = False
    try:
        do GetBallPossession(ball)
        do Idle()
    interrupt when (A.bool(simulation()) and not passed and self.gameObject.ballPossession):
        do Idle() for 2.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point) until MakePass({'player': 'coach'}).bool(simulation())
        do Idle() for 0.5 seconds
        do GetBallPossession(ball)
        do Shoot(goal)
        passed = True

def teammateHasBallPossession():
    for obj in simulation().objects:
        if isinstance(obj, Player) and obj.team == "blue" and obj.gameObject.ballPossession:
            return True
    return False

behavior GetBehindAndReceiveBall(player, zone): # similar logic as inzone
    do MoveToBehavior(point) until self.position.y > player.position.y + 2
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()

behavior ReceiveBall():
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()
 

teammate = new Player at (0,0), 
                with name "teammate",
                with team "blue",
                with behavior TeammateBehavior()

ball = new Ball ahead of teammate by 1

opponent = new Player ahead of teammate by 5,
                    facing toward teammate,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()

ego = new Coach behind opponent by 5, 
            facing toward teammate,
            with name "Coach",
            with team "blue",
            with behavior CoachBehavior()

goal = new Goal behind opponent by 10, facing away from ego

terminate when (ego.gameObject.stopButton)