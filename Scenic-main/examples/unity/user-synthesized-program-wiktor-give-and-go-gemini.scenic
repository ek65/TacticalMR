from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
behavior CoachBehavior():
    do Speak("wait and do nothing for 1 second")
    do Idle() for 1 seconds
    do Speak("move left or right of teammate by unknown meters until teammate to coach relation is known")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("wait until ball possession or pass or specific position is achieved")
    do Idle() until λ_precondition_0_1(simulation(), None)
    do Speak("check conditions if not holding ball and not passed yet")
    if λ_precondition0(simulation(), None):
        do Speak("stop until teammate passes")
        do Idle() until λ_termination1(simulation(), None)
        do Speak("wait until teammate passes and coach has ball")
        do Idle() until λ_precondition_2(simulation(), None)
        do Speak("move to ball and get it")
        do GetBallPossession(ball)
        do Speak("stay idle until coach has ball possession")
        do Idle() until λ_precondition_3(simulation(), None)
        do Speak("stay idle until opponent pressures teammate at specified distance")
        do Idle() until λ_termination3(simulation(), None)
        do Speak("stay idle until pressure situation or other conditions validated")
        do Idle() until λ_precondition_4_5(simulation(), None)
        do Speak("decide to shoot unless pressure present")
        if λ_precondition4(simulation(), None):
            do Shoot(goal)
        else:
            do Pass(teammate)
    else:
        do Speak("without preconditions met, move to ball and claim it")
        do GetBallPossession(ball)
        do Speak("stay idle until coach has ball possession")
        do Idle() until λ_precondition_3(simulation(), None)
        do Speak("idle while monitoring until opponent and teammate in specified zone")
        do Idle() until λ_termination3(simulation(), None)
        do Speak("continue waiting until conditions of pressure or others validated")
        do Idle() until λ_precondition_4_5(simulation(), None)
        do Speak("attempt a score unless pressured by opponent")
        if λ_precondition4(simulation(), None):
            do Shoot(goal)
        else:
            do Pass(teammate)
A1termination_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': -0.0007528053527252602, 'std': 0.0007153030335595689}})
A2termination_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': -0.0007528053527252602, 'std': 0.0007153030335595689}})
A3termination_0 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 0.0003993368083368259, 'std': 0.0003799238375653692}})
A1target_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': -0.0007528053527252602, 'std': 0.0007153030335595689}})
A2target_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': -0.0007528053527252602, 'std': 0.0007153030335595689}})
A1termination_1 = MakePass({'player': 'teammate'})
A1termination_3 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A2termination_3 = CloseTo({'obj': 'opponent', 'ref': 'teammate', 'max': {'avg': 6.340679614533104, 'std': 0.0}})
A1precondition_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': -0.0006735415369667482, 'std': 0.0008106048542939802}})
A2precondition_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': -0.0006735415369667482, 'std': 0.0008106048542939802}})
A3precondition_0 = HasBallPossession({'player': 'Coach'})
A4precondition_0 = MakePass({'player': 'teammate'})
A5precondition_0 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 0.00035743694411417875, 'std': 0.00043062021393143826}})
A1precondition_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': -0.000990596800000796, 'std': 0.0}})
A2precondition_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': -0.000990596800000796, 'std': 0.0}})
A3precondition_1 = MakePass({'player': 'teammate'})
A1precondition_2 = MakePass({'player': 'teammate'})
A2precondition_2 = HasBallPossession({'player': 'Coach'})
A1precondition_3 = HasBallPossession({'player': 'Coach'})
A1precondition_4 = Pressure({'player1': 'Opponent', 'player2': 'Coach'})
A1precondition_5 = Pressure({'player1': 'Opponent', 'player2': 'Coach'})
A1precondition_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': -0.0006735415369667482, 'std': 0.0008106048542939802}})
A2precondition_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': -0.0006735415369667482, 'std': 0.0008106048542939802}})
A3precondition_0 = HasBallPossession({'player': 'Coach'})
A4precondition_0 = MakePass({'player': 'teammate'})
A5precondition_0 = HasPath({'obj1': 'teammate', 'obj2': 'Coach', 'path_width': {'avg': 0.00035743694411417875, 'std': 0.00043062021393143826}})
A1precondition_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': -0.000990596800000796, 'std': 0.0}})
A2precondition_1 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': -0.000990596800000796, 'std': 0.0}})
A3precondition_1 = MakePass({'player': 'teammate'})
A1precondition_2 = MakePass({'player': 'teammate'})
A2precondition_2 = HasBallPossession({'player': 'Coach'})
A1precondition_3 = HasBallPossession({'player': 'Coach'})
A1precondition_4 = Pressure({'player1': 'Opponent', 'player2': 'Coach'})
A1precondition_5 = Pressure({'player1': 'Opponent', 'player2': 'Coach'})
def λ_target0():
    return (A1target_0.dist(simulation(), ego=True) or A2target_0.dist(simulation(), ego=True))

def λ_termination0(scene, sample):
    return (A1termination_0.bool(simulation()) or A2termination_0.bool(simulation())) and A3termination_0.bool(simulation())

def λ_termination1(scene, sample):
    return A1termination_1.bool(simulation())

def λ_termination2(scene, sample):
    return 

def λ_termination3(scene, sample):
    return (A1termination_3.bool(simulation()) or A2termination_3.bool(simulation()))

def λ_termination4(scene, sample):
    return 

def λ_termination5(scene, sample):
    return 

def λ_termination2(scene, sample):
    return 

def λ_termination3(scene, sample):
    return (A1termination_3.bool(simulation()) or A2termination_3.bool(simulation()))

def λ_termination4(scene, sample):
    return 

def λ_termination5(scene, sample):
    return 

def λ_precondition0(scene, sample):
    return ( (A1precondition_0.bool(simulation()) or A2precondition_0.bool(simulation())) and ~(A3precondition_0.bool(simulation())) and ~(A4precondition_0.bool(simulation())) and A5precondition_0.bool(simulation()) )

def λ_precondition1(scene, sample):
    return (A1precondition_1.bool(simulation()) or A2precondition_1.bool(simulation())) and A3precondition_1.bool(simulation())

def λ_precondition_0_1(scene, sample):
    return λ_precondition0(simulation(), sample) or λ_precondition1(simulation(), sample)

def λ_precondition2(scene, sample):
    return (A1precondition_2.bool(simulation()) and A2precondition_2.bool(simulation()))

def λ_precondition_2(scene, sample):
    return λ_precondition2(simulation(), sample)

def λ_precondition3(scene, sample):
    return A1precondition_3.bool(simulation())

def λ_precondition_3(scene, sample):
    return λ_precondition3(simulation(), sample)

def λ_precondition4(scene, sample):
    return ~(A1precondition_4.bool(simulation()))

def λ_precondition5(scene, sample):
    return A1precondition_5.bool(simulation())

def λ_precondition_4_5(scene, sample):
    return λ_precondition4(simulation(), sample) or λ_precondition5(simulation(), sample)

def λ_precondition3(scene, sample):
    return A1precondition_3.bool(simulation())

def λ_precondition_3(scene, sample):
    return λ_precondition3(simulation(), sample)

def λ_precondition4(scene, sample):
    return ~(A1precondition_4.bool(simulation()))

def λ_precondition5(scene, sample):
    return A1precondition_5.bool(simulation())

def λ_precondition_4_5(scene, sample):
    return λ_precondition4(simulation(), sample) or λ_precondition5(simulation(), sample)




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