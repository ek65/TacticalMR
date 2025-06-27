from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    precondition:
        # Coach starts when the teammate has possession.
        HasBallPossession({'player': 'teammate'})
    while True:
        do Speak("Stay ready for a pass from your teammate.")
        # Wait until Coach is free and available for a pass (not closely marked by opponent).
        wait until HasBallPossession({'player': 'teammate'})
        do Speak("Move to a wide open spot to receive the ball.")
        # Move wide (either left or right) to be available for pass from teammate and away from opponent.
        do MoveTo(λ_moveWide)
        do Speak("Hold your position and get ready. The pass may come soon.")
        wait
        # Wait for the pass.
        do Speak("Watch the teammate. Look for the incoming pass.")
        wait until HasBallPossession({'player': 'Coach'})
        do Speak("You have the ball! Assess your options quickly.")
        # If the opponent is close, and teammate is running towards the goal, pass through to teammate.
        if OpponentIsClose() and TeammateIsAhead():
            do Speak("Opponent is pressing. Make a through pass to your teammate.")
            do Pass('teammate')
        else:
            # Otherwise, approach the goal and shoot.
            do Speak("No pressure. Attack the goal directly and go for a shot.")
            do MoveTo(λ_attackGoal)
            do Speak("Take a shot when you see the opportunity.")
            do Shoot()
        # End sequence for one play. Repeat for demonstration.
        wait

# λ_target, λ_termination, λ_precondition functions (all defined explicitly):

def λ_moveWide(self):
    # Move to a location at least 3 meters away from the opponent and in a side zone relative to the ball.
    def pred(sample, scene):
        wide_zones = ['A2', 'A3', 'D2', 'D3']  # Example: wide left/right zones
        return any(InZone({'obj': 'Coach', 'zone': z})(scene, sample) for z in wide_zones) and \
               DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 3}})(scene, sample)
    return pred

def λ_attackGoal(self):
    # Move until Coach is within shooting distance of the center of the goal.
    def pred(sample, scene):
        # Within 7 meters of the goal and in a central lane.
        return DistanceTo({'from': 'Coach', 'to': 'goal', 'max': {'avg': 7}})(scene, sample) and \
               InZone({'obj': 'Coach', 'zone': 'B5'})(scene, sample)
    return pred

def λ_termination(self):
    # Terminate behavior if Coach has shot the ball OR lost possession.
    def term(sample, scene):
        # If Coach lost the ball or took a shot, terminate this behavior sequence.
        return not HasBallPossession({'player': 'Coach'})(scene, sample)
    return term

def λ_precondition(self):
    # Ensure that teammate starts with the ball
    def precond(sample, scene):
        return HasBallPossession({'player': 'teammate'})(scene, sample)
    return precond

# Helper functions used in the behavior

def OpponentIsClose():
    def pred(sample, scene):
        # Opponent within 2.5 meters of Coach
        return DistanceTo({'from': 'Coach', 'to': 'opponent', 'max': {'avg': 2.5}})(scene, sample)
    return pred

def TeammateIsAhead():
    def pred(sample, scene):
        # Teammate's y position is ahead of Coach's (i.e., closer to opponent goal), by at least 2 meters
        return HeightRelation({'obj': 'teammate', 'ref': 'Coach', 'relation': 'ahead', 'vertical_threshold': {'avg': 2.0}})(scene, sample)
    return pred


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