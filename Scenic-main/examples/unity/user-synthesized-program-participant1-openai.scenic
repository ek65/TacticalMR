from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
behavior CoachBehavior():
    do Speak("Coach idles for 1 second with no action.") 
    do Idle() for 1 seconds
    do Speak("Coach moves toward the ball until horizontally aligned with it based on left/right criteria.")
    do MoveTo(λ_target0) until λ_termination0(simulation(), None)
    do Speak("Coach idles until he gains ball possession (verified by possession check).")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("Coach moves to obtain ball control by going to the ball.")
    do GetBallPossession(ball)
    do Speak("Coach idles until he confirms continued ball possession.")
    do Idle() until λ_precondition_1(simulation(), None)
    do Speak("Coach idles until either he reaches zone C5 or has an unobstructed path to goal.")
    do Idle() until λ_termination2(simulation(), None)
    do Speak("Coach idles until either zone or passing conditions with teammate or goal are met.")
    do Idle() until λ_precondition_2_3(simulation(), None)
    do Speak("If coach in zone with teammate pass option, then pass; otherwise, shoot at goal.")
    if λ_precondition2(simulation(), None):
        do Speak("Coach passes the ball to the teammate as conditions are met.")
        do Pass(teammate)
    else:
        do Speak("Coach shoots the ball toward the goal as alternative action.")
        do Shoot(goal)
A1termination_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'ball', 'relation': 'left', 'horizontal_threshold': {'avg': nan, 'std': nan}})
A2termination_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'ball', 'relation': 'right', 'horizontal_threshold': {'avg': 0.1923799165, 'std': 0.00562007149999999}})
A1target_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'ball', 'relation': 'left', 'horizontal_threshold': {'avg': nan, 'std': nan}})
A2target_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'ball', 'relation': 'right', 'horizontal_threshold': {'avg': 0.1923799165, 'std': 0.00562007149999999}})
A1termination_2 = InZone({'obj': 'coach', 'zone': ['C5', 'C5']})
A2termination_2 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_0 = HasBallPossession({'player': 'Coach'})
A1precondition_1 = HasBallPossession({'player': 'Coach'})
A1precondition_2 = InZone({'obj': 'coach', 'zone': ['C5', 'C5']})
A2precondition_2 = HasPath({'obj1': 'teammate', 'obj2': 'goal', 'path_width': {'avg': 0.12739879122352266, 'std': 0.014703974570608527}})
A1precondition_3 = HasBallPossession({'player': 'Coach'})
A2precondition_3 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1precondition_0 = HasBallPossession({'player': 'Coach'})
A1precondition_1 = HasBallPossession({'player': 'Coach'})
A1precondition_2 = InZone({'obj': 'coach', 'zone': ['C5', 'C5']})
A2precondition_2 = HasPath({'obj1': 'teammate', 'obj2': 'goal', 'path_width': {'avg': 0.12739879122352266, 'std': 0.014703974570608527}})
A1precondition_3 = HasBallPossession({'player': 'Coach'})
A2precondition_3 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
def λ_target0():
    return A1target_0.dist(simulation(), ego=True) | A2target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation()) | A2termination_0.bool(simulation())

def λ_termination1(scene, sample):
    return True

def λ_termination2(scene, sample):
    return A1termination_2.bool(simulation()) | A2termination_2.bool(simulation())

def λ_termination3(scene, sample):
    return True

def λ_termination4(scene, sample):
    return True

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_0(scene, sample):
    return λ_precondition0(simulation(), sample)

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition_1(scene, sample):
    return λ_precondition1(simulation(), sample)

def λ_precondition2(scene, sample):
    return A1precondition_2.bool(simulation()) & A2precondition_2.bool(simulation())

def λ_precondition3(scene, sample):
    return A1precondition_3.bool(simulation()) & A2precondition_3.bool(simulation())

def λ_precondition_2_3(scene, sample):
    return λ_precondition2(simulation(), sample) or λ_precondition3(simulation(), sample)




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