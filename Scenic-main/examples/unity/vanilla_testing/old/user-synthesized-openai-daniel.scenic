from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

lambda_target_MoveWideRight = lambda self: InZone({'obj': 'Coach', 'zone': 'C2'})
lambda_target_MoveWideLeft = lambda self: InZone({'obj': 'Coach', 'zone': 'A2'})
lambda_target_MoveForwardForPass = lambda self: HeightRelation({'obj':'Coach', 'ref':'teammate', 'relation':'ahead', 'vertical_threshold':{'avg':1.0}})
lambda_target_MoveToGoal = lambda self: HeightRelation({'obj':'Coach', 'ref':'goal', 'relation':'ahead', 'vertical_threshold':{'avg':-2.0}})

lambda_termination_MoveWide = lambda self: InZone({'obj': 'Coach', 'zone': 'C2'})
lambda_termination_MoveWideLeft = lambda self: InZone({'obj': 'Coach', 'zone': 'A2'})
lambda_termination_MoveForwardForPass = lambda self: HeightRelation({'obj':'Coach', 'ref':'teammate', 'relation':'ahead', 'vertical_threshold':{'avg':1.0}})
lambda_termination_MoveToGoal = lambda self: HeightRelation({'obj':'Coach', 'ref':'goal', 'relation':'ahead', 'vertical_threshold':{'avg':-2.0}})

lambda_precondition_Wide = lambda self: True
lambda_precondition_Forward = lambda self: True

behavior CoachBehavior():
    # 1. Get available for pass when teammate is under pressure
    do Speak("Teammate is under pressure, move wide right for the pass.")
    do MoveTo(lambda_target_MoveWideRight) until lambda_termination_MoveWide(self)
    # 2. Wait for pass to Coach
    do Speak("Hold position and wait for the pass from your teammate.")
    do Wait()
    # 3. Receive the ball; get possession
    do Speak("Gain possession after receiving the ball.")
    do GetBallPossession()
    # 4. Move forward to get away from opponent and prepare for through pass
    do Speak("Opponent is pressing, move ahead to be available for a through pass.")
    do MoveTo(lambda_target_MoveForwardForPass) until lambda_termination_MoveForwardForPass(self)
    # 5. Pass to the teammate making the run
    do Speak("Make a through pass to the teammate.")
    do Pass('teammate')
    # Optional: Wait momentarily after pass
    do Speak("Wait a bit for the next phase.")
    do Wait()

class MoveWideRightConstraint(Constraint):
    def __call__(self, scene, sample):
        zone_constraint = InZone({'obj': 'Coach', 'zone': 'C2'})
        return zone_constraint(scene, sample)

lambda_target = lambda self: MoveWideRightConstraint({})

lambda_termination = lambda self: InZone({'obj': 'Coach', 'zone': 'C2'})

lambda_precondition = lambda self: True



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