from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
behavior CoachBehavior():
    do Speak("Coach waits silently for 1 second with no movement or action.")
    do Idle() for 1 seconds
    do Speak("Coach moves toward zone D3 until he reaches that area and gains ball possession.")
    do MoveAs(λ_target0()) until λ_termination0()
    do Speak("Coach idles until he is in the designated zone and currently does not have the ball.")
    do Idle() until λ_precondition_3()
    do Speak("Coach proceeds to the ball in order to take possession of it.")
    do GetBallPossession(ball)
    do Speak("Coach idles until either he has ball possession with a clear pass to goal or opponent pressure subsides.")
    do Idle() until λ_precondition_5_6()
    do Speak("If coach has possession and a clear path to goal, shoot; otherwise, pass to the teammate.")
    if λ_precondition5():
        do Shoot(goal)
    else:
        do Pass(teammate)
A1termination_0 = InZone({'obj': 'coach', 'zone': ['D3', 'D3', 'D3']})
A2termination_0 = HasBallPossession({'player': 'Coach'})
A1target_0 = InZone({'obj': 'coach', 'zone': ['D3', 'D3', 'D3']})
A1precondition_3 = InZone({'obj': 'coach', 'zone': []})
A2precondition_3 = HasBallPossession({'player': 'Coach'})
A1precondition_5 = HasBallPossession({'player': 'Coach'})
A2precondition_5 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 1.0}})
A1precondition_6 = Pressure({'player1': 'opponent', 'player2': 'teammate'})
A1precondition_3 = InZone({'obj': 'coach', 'zone': []})
A2precondition_3 = HasBallPossession({'player': 'Coach'})
A1precondition_5 = HasBallPossession({'player': 'Coach'})
A2precondition_5 = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 1.0}})
A1precondition_6 = Pressure({'player1': 'opponent', 'player2': 'teammate'})
def λ_target0():
    cond = A1target_0
    return cond.dist(simulation(), ego=True)

def λ_termination0():
    cond = (A1termination_0 & A2termination_0)

    return cond.bool(simulation())

def λ_termination4():
    return True

def λ_termination1():
    return True

def λ_termination3():
    return True

def λ_precondition3():
    cond = A1precondition_3 & ~(A2precondition_3)

    return cond.bool(simulation())

def λ_precondition_3():
    return λ_precondition3()

def λ_precondition5():
    cond = A1precondition_5 & A2precondition_5

    return cond.bool(simulation())

def λ_precondition6():
    cond = ~(A1precondition_6)

    return cond.bool(simulation())

def λ_precondition_5_6():
    return λ_precondition5() or λ_precondition6()




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