from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

def movesToward(player1, player2):
    dist1 = distance from player1.prevPosition to player2.prevPosition
    dist2 = distance from player1.position to player2.position
    return dist2 < dist1

behavior Follow(obj):
    while True:
        do MoveToBehavior(obj, distance = 2, status = f"Follow {obj.name}")

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
    # do Follow(teammate)
    print("opponent follows ego")
    do Follow(ego)

A = HasPathToPass({'passer': 'teammate', 'receiver': 'coach', 'path_width':{'avg': 5, 'std':1}})

behavior TeammateBehavior():
    passed = False
    try:
        do GetBall()
        do Idle()
    interrupt when (A(simulation(), None) and not passed and self.gameObject.ballPossession):
        do Idle() for 2.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point)
        passed = True

behavior GetBall():
    while not self.gameObject.ballPossession:
        take MoveToAction(ball.position)

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

behavior CoachBehavior():
    do MoveTo(λ_target0) until λ_termination0(simulation(), None)
    do Idle() until λ_precondition_0(simulation(), None)
    do GetBallPossession()
    do Idle() until λ_precondition_1_3(simulation(), None)
    if λ_precondition1(simulation(), None):
        do MoveTo(λ_target2) until λ_termination2(simulation(), None)
        do Idle() until λ_precondition_2(simulation(), None)
        do Shoot(goal)
    else:
        do passTo({'obj': 'teammate', 'through': False, 'info': 'Coach passes the ball to an open teammate to shift the play forward and advance the attack.'})
A1termination_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': {'avg': -0.047132188766666666, 'std': 0.046622037131346904}, 'std': 0.0}})
A2termination_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': {'avg': -0.047132188766666666, 'std': 0.046622037131346904}, 'std': 0.0}})
A1target_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': -0.047132188766666666, 'std': 0.046622037131346904}})
A2target_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': -0.047132188766666666, 'std': 0.046622037131346904}})
A1termination_2 = HeightRelation({'obj': 'Coach', 'ref': 'goal', 'relation': 'ahead', 'height_threshold': {'avg': {'avg': -4.492499550000001, 'std': 0.14449954999999992}, 'std': 0.0}})
A2termination_2 = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': {'avg': 8.534579252827735, 'std': 0.5416486850561646}, 'std': 0.0}})
A1target_2 = HeightRelation({'obj': 'Coach', 'ref': 'goal', 'relation': 'ahead', 'height_threshold': {'avg': -4.492499550000001, 'std': 0.14449954999999992}})
A2target_2 = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': 8.534579252827735, 'std': 0.5416486850561646}})
A1precondition_0 = HasBallPossession({'player': 'Coach'})
A2precondition_0 = CloseTo({'obj': 'teammate', 'ref': 'opponent', 'max': {'avg': 4.134530787556577, 'std': 0.30574283264607205}})
A1precondition_1 = CloseTo({'obj': 'opponent', 'ref': 'teammate', 'max': {'avg': {'avg': 24.5, 'std': 0.5}, 'std': 0.0}})
A1precondition_2 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 10.0, 'std': 0.0}, 'operator': 'less_than'})
A1precondition_3 = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': {'avg': 8.534579252827735, 'std': 0.5416486850561646}, 'std': 0.0}})
def λ_target0(scene, sample):
    return (A1target_0(simulation(), None) or A2target_0(simulation(), None))

def λ_target2(scene, sample):
    return (A1target_2(simulation(), None) and A2target_2(simulation(), None))

def λ_termination0(scene, sample):
    return A1termination_0(simulation(), None) or A2termination_0(simulation(), None)

def λ_termination1(scene, sample):
    return True

def λ_termination2(scene, sample):
    return A1termination_2(simulation(), None) and A2termination_2(simulation(), None)

def λ_termination3(scene, sample):
    return True

def λ_termination4(scene, sample):
    return True

def λ_precondition0(scene, sample):
    return (A1precondition_0(simulation(), None) and A2precondition_0(simulation(), None))

def λ_precondition1(scene, sample):
    return A1precondition_1(simulation(), None)

def λ_precondition3(scene, sample):
    return A1precondition_3(simulation(), None)

def λ_precondition_1_3(scene, sample):
    return λ_precondition1(simulation(), None) or λ_precondition3(simulation(), None)

def λ_precondition2(scene, sample):
    return A1precondition_2(simulation(), None)

# behavior IdleSpecial():
#     while True:
#         take IdleAction()
#         # print(f"teammate: {teammate.gameObject.behavior}")
#         print(f"opponent: {opponent.gameObject.behavior}")
#         print(f"ego: {ego.name}")
#         # print(f"opp.x: {self.position.x}, opp.y: {self.position.y}, opp.z: {self.position.z}")

# teammate2 = new Player at (10,0), 
#                 with name "teammate2",
#                 with team "blue",
#                 with behavior IdleSpecial()

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