from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Wait to receive the ball from your teammate.")
    do Idle() until λ_precondition_receive_teammate(simulation(), None)
    do MoveTo(λ_target_move_side()) until λ_termination_side(simulation(), None)
    do Speak("Get the ball possession from the teammate’s pass.")
    do GetBallPossession(ball)
    if λ_precondition_pressure(simulation(), None):
        do Speak("Move into space away from the opponent for a pass option.")
        do MoveTo(λ_target_space()) until λ_termination_space(simulation(), None)
        do Speak("Wait for the teammate to move towards goal.")
        do Idle() until λ_precondition_teammate_runs(simulation(), None)
        do Speak("Pass through to the open teammate running for goal.")
        do Pass(teammate)
    elif λ_precondition_no_pressure(simulation(), None):
        do Speak("Move towards the goal for a shooting opportunity.")
        do MoveTo(λ_target_goal()) until λ_termination_goal(simulation(), None)
        do Speak("Wait until close to goal and with ball possession.")
        do Idle() until λ_precondition_shoot(simulation(), None)
        do Speak("Take a shot towards the goal.")
        do Shoot(goal)
    else:
        do Speak("Wait briefly, looking for clear options.")
        do Idle()

A1precondition_receive_teammate = MakePass({'player': 'teammate'})
A1precondition_pressure = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_no_pressure = ~(Pressure({'player1': 'opponent', 'player2': 'Coach'}))
A1target_space = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.25, 'std': 0.25}, 'max': None, 'operator': 'greater_than'})
A1termination_space = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.25, 'std': 0.25}, 'max': None, 'operator': 'greater_than'})
A1precondition_teammate_runs = MovingTowards({'obj': 'teammate', 'ref': 'goal'})
A1target_goal = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.1, 'std': 0.1}, 'operator': 'less_than'})
A1termination_goal = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.1, 'std': 0.1}, 'operator': 'less_than'})
A1precondition_shoot = (HasBallPossession({'player': 'Coach'}) and DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.2, 'std': 0.2}, 'operator': 'less_than'}))

A1target_move_side = HorizontalRelation({'obj': 'Coach', 'relation': 'right', 'ref': 'teammate', 'horizontal_threshold': {'avg': 5.0, 'std': 0.2}})
A1termination_side = HorizontalRelation({'obj': 'Coach', 'relation': 'right', 'ref': 'teammate', 'horizontal_threshold': {'avg': 5.0, 'std': 0.2}})


def λ_target_move_side():
    return A1target_move_side.dist(simulation(), ego=True)

def λ_termination_side(scene, sample):
    return A1termination_side.bool(simulation())
    
def λ_precondition_receive_teammate(scene, sample):
    return A1precondition_receive_teammate.bool(simulation())

def λ_precondition_pressure(scene, sample):
    return A1precondition_pressure.bool(simulation())

def λ_precondition_no_pressure(scene, sample):
    return A1precondition_no_pressure.bool(simulation())

def λ_target_space():
    return A1target_space.dist(simulation(), ego=True)

def λ_termination_space(scene, sample):
    return A1termination_space.bool(simulation())

def λ_precondition_teammate_runs(scene, sample):
    return A1precondition_teammate_runs.bool(simulation())

def λ_target_goal():
    return A1target_goal.dist(simulation(), ego=True)

def λ_termination_goal(scene, sample):
    return A1termination_goal.bool(simulation())

def λ_precondition_shoot(scene, sample):
    return A1precondition_shoot


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