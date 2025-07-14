from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
behavior CoachBehavior():
    do Speak("wait idly for about 1 second with no movement")
    do Idle() for 1 seconds
    do Speak("run to the ball so the coach gains ball possession")
    do GetBallPossession(ball)
    do Speak("pause until the coach’s horizontal offset relative to the teammate meets set criteria")
    do Idle() until λ_precondition_1(simulation(), None)
    do Speak("move toward a target position left/right of teammate using horizontal threshold")
    do MoveTo(λ_target1) until λ_termination1(simulation(), None)
    do Speak("wait until conditions allow a pass to the teammate")
    do Idle() until λ_precondition_2(simulation(), None)
    do Speak("run again to the ball to secure possession")
    do GetBallPossession(ball)
    do Speak("pause until either a clear shooting path to goal or an obstructed pass path is detected")
    do Idle() until λ_precondition_4_5(simulation(), None)
    do Speak("if a clear path to goal exists, then prepare to shoot")
    if λ_precondition4(simulation(), None):
        do Speak("shoot the ball toward the goal immediately")
        do Shoot(goal)
    else:
        do Speak("wait until passing pressure from opponents is relieved")
        do Idle() until λ_termination4(simulation(), None)
        do Speak("pause until a clear path to teammate emerges")
        do Idle() until λ_precondition_6(simulation(), None)
        do Speak("pass the ball to the teammate")
        do Pass(teammate)
        do Speak("wait until the teammate is not pressured and in valid distance")
        do Idle() until λ_precondition_7(simulation(), None)
        do Speak("regain ball possession after the pass")
        do GetBallPossession(ball)
        do Speak("wait until the coach is positioned within zone C5 on the field")
        do Idle() until λ_precondition_8(simulation(), None)
        do Speak("finally, shoot the ball toward the goal")
        do Shoot(goal)
A1termination_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': nan, 'std': nan}})
A2termination_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': 0.40049360700000003, 'std': 0.009865832999999963}})
A1target_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': nan, 'std': nan}})
A2target_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': 0.40049360700000003, 'std': 0.009865832999999963}})
A1termination_4 = HasPath({'obj1': 'teammate', 'obj2': 'goal', 'path_width': {'avg': 0.12739879122352266, 'std': 0.014703974570608527}})
A2termination_4 = Pressure({'player1': 'opponent', 'player2': 'teammate'})
A1precondition_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': nan, 'std': nan}})
A2precondition_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': 0.39062777400000004, 'std': 0.0}})
A1precondition_2 = MakePass({'player': 'teammate'})
A1precondition_3 = HasBallPossession({'player': 'Coach'})
A1precondition_4 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_5 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.008382096896738045, 'std': 0.0}})
A1precondition_6 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.045738412923325214, 'std': 0.03735631602658717}})
A1precondition_7 = MakePass({'player': 'Coach'})
A2precondition_7 = Pressure({'player1': 'opponent', 'player2': 'teammate'})
A3precondition_7 = DistanceTo({'from': 'goal', 'to': 'teammate', 'min': {'avg': 14.37173079141661, 'std': 0.0}, 'max': {'avg': 17.565448745064746, 'std': 0.0}, 'operator': 'within'})
A1precondition_8 = InZone({'obj': 'coach', 'zone': ['C5']})
A1precondition_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': nan, 'std': nan}})
A2precondition_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': 0.39062777400000004, 'std': 0.0}})
A1precondition_2 = MakePass({'player': 'teammate'})
A1precondition_3 = HasBallPossession({'player': 'Coach'})
A1precondition_4 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_5 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.008382096896738045, 'std': 0.0}})
A1precondition_6 = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 0.045738412923325214, 'std': 0.03735631602658717}})
A1precondition_7 = MakePass({'player': 'Coach'})
A2precondition_7 = Pressure({'player1': 'opponent', 'player2': 'teammate'})
A3precondition_7 = DistanceTo({'from': 'goal', 'to': 'teammate', 'min': {'avg': 14.37173079141661, 'std': 0.0}, 'max': {'avg': 17.565448745064746, 'std': 0.0}, 'operator': 'within'})
A1precondition_8 = InZone({'obj': 'coach', 'zone': ['C5']})
def λ_target1(scene, sample):
    cond = (A1target_1 | A2target_1)

    return cond.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return True

def λ_termination1(scene, sample):
    cond = (A1termination_1 | A2termination_1)

    return cond.bool(simulation())

def λ_termination3(scene, sample):
    return True

def λ_termination6(scene, sample):
    return True

def λ_termination4(scene, sample):
    cond = (A1termination_4 & ~(A2termination_4))

    return cond.bool(simulation())

def λ_termination5(scene, sample):
    return True

def λ_termination7(scene, sample):
    return True

def λ_termination6(scene, sample):
    return True

def λ_precondition1(scene, sample):
    cond = (A1precondition_1 | A2precondition_1)

    return cond.bool(simulation())

def λ_precondition_1(scene, sample):
    return λ_precondition1(simulation(), sample)

def λ_precondition2(scene, sample):
    cond = A1precondition_2

    return cond.bool(simulation())

def λ_precondition_2(scene, sample):
    return λ_precondition2(simulation(), sample)

def λ_precondition4(scene, sample):
    cond = A1precondition_4

    return cond.bool(simulation())

def λ_precondition5(scene, sample):
    cond = ~(A1precondition_5)

    return cond.bool(simulation())

def λ_precondition_4_5(scene, sample):
    return λ_precondition4(simulation(), sample) or λ_precondition5(simulation(), sample)

def λ_precondition6(scene, sample):
    cond = A1precondition_6

    return cond.bool(simulation())

def λ_precondition_6(scene, sample):
    return λ_precondition6(simulation(), sample)

def λ_precondition7(scene, sample):
    cond = A1precondition_7 & ~(A2precondition_7) & A3precondition_7

    return cond.bool(simulation())

def λ_precondition_7(scene, sample):
    return λ_precondition7(simulation(), sample)

def λ_precondition8(scene, sample):
    cond = A1precondition_8

    return cond.bool(simulation())

def λ_precondition_8(scene, sample):
    return λ_precondition8(simulation(), sample)




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
    interrupt when (A(simulation(), None) and not passed and self.gameObject.ballPossession):
        do Idle() for 2.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point) until MakePass({'player': 'coach'})(simulation(), None)
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