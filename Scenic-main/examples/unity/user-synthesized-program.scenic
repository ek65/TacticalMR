from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
behavior CoachBehavior():
    take SpeakAction("Coach moves sideways left/right by over 5 meters until a teammate pass event occurs.")
    do MoveTo(λ_target0) until λ_termination0(simulation(), None)
    
    take SpeakAction("Coach idles until both a teammate pass signal and ball possession are confirmed.")
    do Idle() until λ_precondition_0(simulation(), None)
    
    take SpeakAction("Coach moves to the ball with intent to gain possession.")
    do GetBallPossession(ball)
    
    take SpeakAction("Coach idles waiting until either opponent pressure or teammate near goal is detected.")
    do Idle() until λ_precondition_1_4(simulation(), None)
    
    take SpeakAction("If opponent pressure is high, Coach prepares to approach goal for a shot.")
    if λ_precondition1(simulation(), None):
        take SpeakAction("Coach advances toward goal until within about 4.94 meters proximity.")
        do MoveTo(λ_target2) until λ_termination2(simulation(), None)
        
        take SpeakAction("Coach idles once near goal, waiting for proper shot alignment.")
        do Idle() until λ_precondition_2(simulation(), None)
        
        take SpeakAction("Coach shoots the ball aiming accurately at the goal.")
        do Shoot(goal)
    else:
        take SpeakAction("Without acute pressure, Coach opts to pass the ball to a teammate.")
        do Pass(teammate)
A1termination_0 = MakePass({'player': 'teammate'})
A1target_0 = HorizontalRelation({'obj': 'Coach', 'ref': None, 'relation': 'left', 'horizontal_threshold': {'avg': 5, 'std': 0.0}})
A2target_0 = HorizontalRelation({'obj': 'Coach', 'ref': None, 'relation': 'right', 'horizontal_threshold': {'avg': 5, 'std': 0.0}})
A1termination_2 = CloseTo({'obj': 'Coach', 'ref': 'goal', 'max': {'avg': 4.937129569846508, 'std': 0.0}})
A1target_2 = CloseTo({'obj': 'Coach', 'ref': 'goal', 'max': {'avg': 4.937129569846508, 'std': 0.16371462664283953}})
A1precondition_0 = MakePass({'player': 'teammate'})
A2precondition_0 = HasBallPossession({'player': 'Coach'})
A1precondition_1 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_2 = CloseTo({'obj': 'Coach', 'ref': 'goal', 'max': {'avg': 4.937129569846508, 'std': 0.0}})
A1precondition_4 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A2precondition_4 = CloseTo({'obj': 'teammate', 'ref': 'goal', 'max': 10})
A1precondition_0 = MakePass({'player': 'teammate'})
A2precondition_0 = HasBallPossession({'player': 'Coach'})
A1precondition_1 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_2 = CloseTo({'obj': 'Coach', 'ref': 'goal', 'max': {'avg': 4.937129569846508, 'std': 0.0}})
A1precondition_4 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A2precondition_4 = CloseTo({'obj': 'teammate', 'ref': 'goal', 'max': 10})
def λ_target0(scene, sample):
    return A1target_0(simulation(), sample) or A2target_0(simulation(), sample)

def λ_target2(scene, sample):
    return A1target_2(simulation(), sample)

def λ_termination0(scene, sample):
    return A1termination_0(simulation(), sample)

def λ_termination1(scene, sample):
    return True

def λ_termination2(scene, sample):
    return A1termination_2(simulation(), sample)

def λ_termination3(scene, sample):
    return True

def λ_termination4(scene, sample):
    return True

def λ_precondition0(scene, sample):
    return (A1precondition_0(simulation(), sample) and A2precondition_0(simulation(), sample))

def λ_precondition_0(scene, sample):
    return λ_precondition0(simulation(), sample)

def λ_precondition1(scene, sample):
    return A1precondition_1(simulation(), sample)

def λ_precondition4(scene, sample):
    return A1precondition_4(simulation(), sample) and A2precondition_4(simulation(), sample)

def λ_precondition_1_4(scene, sample):
    return λ_precondition1(simulation(), sample) or λ_precondition4(simulation(), sample)

def λ_precondition2(scene, sample):
    return A1precondition_2(simulation(), sample)

def λ_precondition_2(scene, sample):
    return λ_precondition2(simulation(), sample)




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