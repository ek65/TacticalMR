from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
behavior CoachBehavior():
    do Speak("Coach stands idle for one second without taking action.")
    do Idle() for 1 seconds

    do Speak("Coach moves to a target spot based on teammate and opponent positions.")
    do MoveTo(λ_target0) until λ_termination0(simulation(), None)

    do Speak("Coach waits until a teammate is ready for a pass opportunity.")
    do Idle() until λ_precondition_6(simulation(), None)

    do Speak("Coach goes to the ball to take possession.")
    do GetBallPossession(ball)

    do Speak("Coach idles until pressure or movement cues decide the next play.")
    do Idle() until λ_precondition_7_8(simulation(), None)

    do Speak("If opponent pressure exists, follow the passing strategy.")
    if λ_precondition7(simulation(), None):
        do Speak("Coach moves to a safer spot from the opponent for passing.")
        do MoveTo(λ_target2) until λ_termination2(simulation(), None)
        do Speak("Coach waits until a clear distance from opponent is reestablished.")
        do Idle() until λ_precondition_9(simulation(), None)
        do Speak("Coach then passes the ball to the teammate.")
        do Pass(teammate)
    else:
        do Speak("Coach moves toward goal area when pressure is low.")
        do MoveTo(λ_target3) until λ_termination3(simulation(), None)
        do Speak("Coach idles waiting for an unobstructed path to goal.")
        do Idle() until λ_precondition_10(simulation(), None)
        do Speak("Coach shoots the ball aiming at the goal.")
        do Shoot(goal)
A3termination_0 = HeightRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'below', 'height_threshold': {'avg': -2.0, 'std': 1.0}})
A1termination_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': -0.051703527400000004, 'std': 0.044703527400000005}})
A2termination_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': 0.013, 'std': 0.0}})
A4termination_0 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 5.921706745077121, 'std': 0.18424753231768948}, 'max': None, 'operator': 'greater_than'})
A1target_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'left', 'horizontal_threshold': {'avg': -0.051703527400000004, 'std': 0.044703527400000005}})
A2target_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'right', 'horizontal_threshold': {'avg': 0.013, 'std': 0.0}})
A3target_0 = HeightRelation({'obj': 'Coach', 'ref': 'teammate', 'relation': 'below', 'height_threshold': {'avg': -2.0, 'std': 1.0}})
A4target_0 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 5.921706745077121, 'std': 0.18424753231768948}, 'max': None, 'operator': 'greater_than'})
A1termination_2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.172259899611368, 'std': 0.0}, 'max': None, 'operator': 'greater_than'})
A1target_2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.172259899611368, 'std': 0.0}, 'max': None, 'operator': 'greater_than'})
A1termination_3 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.025909493366715, 'std': 0.015410097852564864}, 'operator': 'less_than'})
A1target_3 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.025909493366715, 'std': 0.015410097852564864}, 'operator': 'less_than'})
A1precondition_6 = MakePass({'player': 'teammate'})
A1precondition_7 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_8 = MovingTowards({'obj': 'opponent', 'ref': 'teammate'})
A2precondition_8 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_9 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.172259899611368, 'std': 0.0}, 'max': None, 'operator': 'greater_than'})
A1precondition_10 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_6 = MakePass({'player': 'teammate'})
A1precondition_7 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_8 = MovingTowards({'obj': 'opponent', 'ref': 'teammate'})
A2precondition_8 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_9 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.172259899611368, 'std': 0.0}, 'max': None, 'operator': 'greater_than'})
A1precondition_10 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
def λ_target0(scene, sample):
    return (A3target_0(simulation(), sample) and (A1target_0(simulation(), sample) or A2target_0(simulation(), sample)) and A4target_0(simulation(), sample))

def λ_target2(scene, sample):
    return A1target_2(simulation(), sample)

def λ_target3(scene, sample):
    return A1target_3(simulation(), sample)

def λ_termination0(scene, sample):
    return (A3termination_0(simulation(), sample) and (A1termination_0(simulation(), sample) or A2termination_0(simulation(), sample)) and A4termination_0(simulation(), sample))

def λ_termination1(scene, sample):
    return True

def λ_termination2(scene, sample):
    return A1termination_2(simulation(), sample)

def λ_termination4(scene, sample):
    return True

def λ_termination3(scene, sample):
    return A1termination_3(simulation(), sample)

def λ_termination5(scene, sample):
    return True

def λ_precondition6(scene, sample):
    return A1precondition_6(simulation(), sample)

def λ_precondition_6(scene, sample):
    return λ_precondition6(simulation(), sample)

def λ_precondition7(scene, sample):
    return A1precondition_7(simulation(), sample)

def λ_precondition8(scene, sample):
    return (A1precondition_8(simulation(), sample) and not(A2precondition_8(simulation(), sample)))

def λ_precondition_7_8(scene, sample):
    return λ_precondition7(simulation(), sample) or λ_precondition8(simulation(), sample)

def λ_precondition9(scene, sample):
    return A1precondition_9(simulation(), sample)

def λ_precondition_9(scene, sample):
    return λ_precondition9(simulation(), sample)

def λ_precondition10(scene, sample):
    return A1precondition_10(simulation(), sample)

def λ_precondition_10(scene, sample):
    return λ_precondition10(simulation(), sample)




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