from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

import numpy as np
import scenic.core.dynamics as _dynamics
from scenic.core.distributions import RejectionException
from scenic.core.geometry import hypot
from scenic.core.vectors import Vector
from scenic.core.utils import shortPrint
from scenic.core.errors import RuntimeParseError, InvalidScenarioError
from scenic.core.errors import InvalidScenarioError

# Objects (pre-declared as per instructions)
# Ball (ID: ball)
# Coach (ID: Coach)
# Goal (ID: goal)
# Goal (ID: goal_leftpost)
# Goal (ID: goal_rightpost)
# Opponent (ID: opponent)
# Teammate (ID: teammate)

# Constraint API (as provided)
def findObj(id, objects):
    if isinstance(id, str):
        key_lower = id.lower()
        return [obj for obj in objects if key_lower in obj.name.lower()]
    return []

def isEgo(id, scene):
    return id.lower() == scene.egoObject.name.lower()
    
class Constraint:
    def __init__(self, args):
        self.args = args

    def __call__(self, sample, scene):
        pass

class HasBallPossession(Constraint):
    def __init__(self, args):
        self.playerID = args.get('player', None)
    def __call__(self, scene, sample):
        if not self.playerID: return False
        player_objs = findObj(self.playerID, scene.objects)
        if not player_objs: return False
        player_obj = player_objs[0]
        ball_obj = findObj('ball', scene.objects)
        if not ball_obj: return False
        ball_obj = ball_obj[0]
        # Assuming ball possession means player is very close to the ball
        return hypot(player_obj.position.x - ball_obj.position.x, player_obj.position.y - ball_obj.position.y) < 1.0 # arbitrary threshold

class InZone(Constraint):
    FIELD_WIDTH, FIELD_HEIGHT = 20, 34
    NUM_ZONES_X, NUM_ZONES_Y = 4, 5
    ZONE_WIDTH = FIELD_WIDTH / NUM_ZONES_X
    ZONE_HEIGHT = FIELD_HEIGHT / NUM_ZONES_Y

    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.zone = args.get('zone', None)
    def __call__(self, scene, sample):
        if not self.objID or not self.zone: return False
        obj = findObj(self.objID, scene.objects)
        if not obj: return False
        obj = obj[0]
        
        # Calculate zone boundaries based on 'AX' format
        col_char = self.zone[0].upper()
        row_num = int(self.zone[1:])
        col_index = ord(col_char) - ord('A')
        
        min_x = col_index * self.ZONE_WIDTH - self.FIELD_WIDTH / 2
        max_x = (col_index + 1) * self.ZONE_WIDTH - self.FIELD_WIDTH / 2
        min_y = (row_num - 1) * self.ZONE_HEIGHT - self.FIELD_HEIGHT / 2
        max_y = row_num * self.ZONE_HEIGHT - self.FIELD_HEIGHT / 2
        
        return min_x <= obj.position.x < max_x and min_y <= obj.position.y < max_y
        
class MovingTowards(Constraint):
    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
    def __call__(self, scene, sample):
        if not self.objID or not self.refID: return False
        obj = findObj(self.objID, scene.objects)
        ref = findObj(self.refID, scene.objects)
        if not obj or not ref: return False
        obj = obj[0]
        ref = ref[0]
        
        # Crude approximation: checking if obj's velocity points towards ref
        # This requires dynamic properties like velocity which might not be directly in scenic objects
        # For simplicity, let's assume `velocity` attribute exists on objects in simulator
        if not hasattr(obj, 'velocity') or not hasattr(ref, 'position'):
             # If velocity not available, assume not moving towards or always true for demo purposes
            return False # Or True if we want to default to `True` for `MovingTowards` based on demo context
        
        # Vector from obj to ref
        vec_to_ref = Vector(ref.position.x - obj.position.x, ref.position.y - obj.position.y)
        # Dot product of obj's velocity and vector to ref
        dot_product = obj.velocity.dot(vec_to_ref)
        # If dot product is positive, obj is generally moving towards ref
        return dot_product > 0.1 # Small positive value to avoid floating point issues

class HasPathToPass(Constraint):
    def __init__(self, args={}):
        self.passerID = args.get('passer', None)
        self.receiverID = args.get('receiver', None)
        self.radius_dict = args.get('path_width', None)
        self.radiusAvg = self.radius_dict.get('avg', 0.0) if self.radius_dict else 0.0
        self.radiusStd = self.radius_dict.get('std', 1.0) if self.radius_dict else 1.0

    def __call__(self, scene, sample):
        if not self.passerID or not self.receiverID: return False
        passer = findObj(self.passerID, scene.objects)
        receiver = findObj(self.receiverID, scene.objects)
        opponents = findObj('opponent', scene.objects)
        if not passer or not receiver: return False
        passer = passer[0]
        receiver = receiver[0]
        
        pass_vector = Vector(receiver.position.x - passer.position.x, receiver.position.y - passer.position.y)
        pass_length_sq = pass_vector.norm_sq
        
        radius_sample = np.random.normal(self.radiusAvg, self.radiusStd)
        
        for opp in opponents:
            # Project opponent onto the pass line
            opp_vector = Vector(opp.position.x - passer.position.x, opp.position.y - passer.position.y)
            
            t = opp_vector.dot(pass_vector) / pass_length_sq
            
            # Check if projection is within the segment (0,1)
            if 0 < t < 1:
                # Calculate the shortest distance from opponent to the line segment
                closest_point_on_line = passer.position + pass_vector * t
                dist_to_line = hypot(opp.position.x - closest_point_on_line.x, opp.position.y - closest_point_on_line.y)
                if dist_to_line < radius_sample:
                    return False # Opponent obstructs path
            else:
                # Check distance to endpoints if not within segment
                dist_to_passer = hypot(opp.position.x - passer.position.x, opp.position.y - passer.position.y)
                dist_to_receiver = hypot(opp.position.x - receiver.position.x, opp.position.y - receiver.position.y)
                if dist_to_passer < radius_sample or dist_to_receiver < radius_sample:
                    return False
        return True

class DistanceTo(Constraint):
    def __init__(self, args):
        self.fromID = args.get('from', None)
        self.toID = args.get('to', None)
        self.min_dict = args.get('min', None)
        self.max_dict = args.get('max', None)
        self.operator = args.get('operator', None)
        
        self.minAvg = self.min_dict.get('avg', None) if self.min_dict else None
        self.maxAvg = self.max_dict.get('avg', None) if self.max_dict else None

    def __call__(self, scene, sample):
        if not self.fromID or not self.toID: return False
        from_obj = findObj(self.fromID, scene.objects)
        to_obj = findObj(self.toID, scene.objects)
        if not from_obj or not to_obj: return False
        from_obj = from_obj[0]
        to_obj = to_obj[0]
        
        distance = hypot(from_obj.position.x - to_obj.position.x, from_obj.position.y - to_obj.position.y)
        
        if self.operator == 'less_than':
            return distance < self.maxAvg
        elif self.operator == 'greater_than':
            return distance > self.minAvg
        elif self.operator == 'within':
            return self.minAvg <= distance <= self.maxAvg
        return False

class HeightRelation(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.relation = args.get('relation', None)
        self.threshold = args.get('vertical_threshold', None)
        self.threshold_avg = self.threshold.get('avg') if self.threshold else None

    def __call__(self, scene, sample):
        if not self.objID: return False
        player_objs = findObj(self.objID, scene.objects)
        if not player_objs: return False
        player_obj = player_objs[0]
        player_y = player_obj.position.y

        value = player_y
        if self.refID:
            ref_objs = findObj(self.refID, scene.objects)
            if not ref_objs: return False
            ref_obj = ref_objs[0]
            ref_y = ref_obj.position.y
            value = player_y - ref_y

        if self.threshold_avg is None: return False
        
        if self.relation == 'behind':
            return value < -self.threshold_avg # Corrected for 'behind' (smaller y means behind if positive y is 'ahead')
        elif self.relation == 'ahead':
            return value > self.threshold_avg
        return False

class HorizontalRelation(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.relation = args.get('relation', None)
        self.horizontal_threshold = args.get('horizontal_threshold', None)
        self.threshold_avg = float(self.horizontal_threshold.get('avg')) if self.horizontal_threshold else None

    def __call__(self, scene, sample):
        if not self.objID: return False
        player_objs = findObj(self.objID, scene.objects)
        if not player_objs: return False
        player_obj = player_objs[0]
        player_x = player_obj.position.x

        value = player_x
        if self.refID:
            ref_objs = findObj(self.refID, scene.objects)
            if not ref_objs: return False
            ref_obj = ref_objs[0]
            ref_x = ref_obj.position.x
            value = player_x - ref_x

        if self.threshold_avg is None: return False
        
        if self.relation == 'left':
            return value < -self.threshold_avg
        elif self.relation == 'right':
            return value > self.threshold_avg
        return False

behavior CoachBehavior():
    # Wait for the ball to be passed to Coach.
    do Speak("Alright team, let's get ready! Wait for the ball, stay alert!")
    do Wait() until (lambda self, scene: HasBallPossession(player='Coach')(scene, self.current_scenario.simulator.current_sample))

    # Once Coach has the ball, confirm possession.
    do Speak("You've got the ball! Now, assess the situation quickly!")
    do GetBallPossession() with
        lambda_precondition: (lambda self, scene: HasBallPossession(player='Coach')(scene, self.current_scenario.simulator.current_sample)),
        lambda_termination: (lambda self, scene: HasBallPossession(player='Coach')(scene, self.current_scenario.simulator.current_sample))

    # Decision logic: Shoot if opponent is distracted by teammate, otherwise pass.
    try:
        # Scenario 2/3 (Shoot): Opponent is moving towards teammate (Coach is free).
        do Speak("Excellent! Opponent is focused on your teammate. Drive to the goal!")
        do MoveTo(lambda pos: pos in goal) with
            invariant: (lambda self, scene: MovingTowards(obj='opponent', ref='teammate')(scene, self.current_scenario.simulator.current_sample)),
            lambda_precondition: (lambda self, scene: HasBallPossession(player='Coach')(scene, self.current_scenario.simulator.current_sample)),
            lambda_termination: (lambda self, scene: DistanceTo(from='Coach', to='goal', operator='less_than', max={'avg': 7.0, 'std': 1.0})(scene, self.current_scenario.simulator.current_sample))

        do Speak("You're in prime position! Take the shot, unleash a powerful strike!")
        do Shoot(target=goal) with
            lambda_precondition: (lambda self, scene: HasBallPossession(player='Coach')(scene, self.current_scenario.simulator.current_sample) and
                                 DistanceTo(from='Coach', to='goal', operator='less_than', max={'avg': 7.0, 'std': 1.0})(scene, self.current_scenario.simulator.current_sample)),
            lambda_termination: (lambda self, scene: not HasBallPossession(player='Coach')(scene, self.current_scenario.simulator.current_sample))

    except _dynamics.guards.InvariantViolation: # Scenario 0 (Pass): Coach is pressured or needs to create space.
        do Speak("Opponent is closing in! Get into a wide-open passing lane!")
        do MoveTo(lambda pos: pos.x > teammate.position.x + 5.0 or pos.x < teammate.position.x - 5.0) with
            lambda_precondition: (lambda self, scene: HasBallPossession(player='Coach')(scene, self.current_scenario.simulator.current_sample)),
            lambda_termination: (lambda self, scene:
                                 (abs(self.position.x - teammate.position.x) > 5.0) and # Is wide open
                                 HasPathToPass(passer='Coach', receiver='teammate', path_width={'avg': 1.5, 'std': 0.2})(scene, self.current_scenario.simulator.current_sample))

        do Speak("Fantastic! Pass it through to your teammate for a great scoring chance!")
        do Pass(target='teammate') with
            lambda_precondition: (lambda self, scene: HasBallPossession(player='Coach')(scene, self.current_scenario.simulator.current_sample) and
                                 HasPathToPass(passer='Coach', receiver='teammate', path_width={'avg': 1.5, 'std': 0.2})(scene, self.current_scenario.simulator.current_sample)),
            lambda_termination: (lambda self, scene: HasBallPossession(player='teammate')(scene, self.current_scenario.simulator.current_sample))


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