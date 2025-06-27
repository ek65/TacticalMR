from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

import numpy as np

def findObj(id, objects):
    if isinstance(id, str):
        key_lower = id.lower()
        return [obj for obj in objects if key_lower in obj.name.lower()]

def isEgo(id, scene):
    return id.lower() == scene.egoObject.name.lower()
    
# MARK: Constraints
class Constraint:
    def __init__(self, args):
        self.args = args

    def __call__(self, sample, scene):
        pass

# MARK: HasBallPossession 
class HasBallPossession(Constraint):

    def __init__(self, args):
        self.playerID = args.get('player', None)

    def __call__(self, scene, sample):
        player_objs = findObj(self.playerID, scene.objects)
        if not player_objs:
            return False
        player_obj = player_objs[0]
        ball_obj_list = findObj('ball', scene.objects)
        if not ball_obj_list:
            return False
        ball_obj = ball_obj_list[0]
        # Assuming possession means being very close to the ball
        return player_obj.distanceTo(ball_obj) < 1.0 
    
# MARK: InZone

FIELD_WIDTH, FIELD_HEIGHT = 20, 34
NUM_ZONES_X, NUM_ZONES_Y = 4, 5
ZONE_WIDTH = FIELD_WIDTH / NUM_ZONES_X
ZONE_HEIGHT = FIELD_HEIGHT / NUM_ZONES_Y

class InZone(Constraint):

    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.zone = args.get('zone', None)

    def __call__(self, scene, sample):
        obj_list = findObj(self.objID, scene.objects)
        if not obj_list:
            return False
        obj = obj_list[0]
        
        col_char = self.zone[0].upper()
        row_num = int(self.zone[1:])
        
        col_idx = ord(col_char) - ord('A')
        row_idx = row_num - 1
        
        min_x = -FIELD_WIDTH / 2 + col_idx * ZONE_WIDTH
        max_x = min_x + ZONE_WIDTH
        min_y = row_idx * ZONE_HEIGHT
        max_y = min_y + ZONE_HEIGHT
        
        return min_x <= obj.position.x < max_x and min_y <= obj.position.y < max_y
        
# MARK: MovingTowards
class MovingTowards(Constraint):

    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)

    def __call__(self, scene, sample):
        obj_list = findObj(self.objID, scene.objects)
        ref_list = findObj(self.refID, scene.objects)

        if not obj_list or not ref_list:
            return False

        obj = obj_list[0]
        ref = ref_list[0]
        
        if hasattr(obj, 'heading') and hasattr(obj, 'position') and hasattr(ref, 'position'):
            vec_to_ref = ref.position - obj.position
            if np.linalg.norm(vec_to_ref) == 0:
                return False 
            obj_heading_vec = np.array([np.cos(obj.heading), np.sin(obj.heading)])
            vec_to_ref_norm = vec_to_ref / np.linalg.norm(vec_to_ref)
            
            dot_product = np.dot(obj_heading_vec, vec_to_ref_norm)
            
            return dot_product > 0.8 # Threshold for "moving towards"
        return False

# MARK: HasPathToPass

class HasPathToPass(Constraint):

    def __init__(self, args={}):
        self.passerID = args.get('passer', None)
        self.receiverID = args.get('receiver', None)
        self.radius = args.get('path_width', None)
        self.radiusAvg = self.radius.get('avg', 0.0) if self.radius else 0.0
        self.radiusStd = self.radius.get('std', 1.0) if self.radius else 1.0

    def __call__(self, scene, sample):
        passer_objs = findObj(self.passerID, scene.objects)
        receiver_objs = findObj(self.receiverID, scene.objects)
        opponent_objs = findObj('opponent', scene.objects)

        if not passer_objs or not receiver_objs:
            return False
        
        passer = passer_objs[0]
        receiver = receiver_objs[0]

        p1 = passer.position
        p2 = receiver.position

        for opponent in opponent_objs:
            line_vec = p2 - p1
            l2 = np.dot(line_vec, line_vec)
            if l2 == 0.0:
                dist_to_line = opponent.distanceTo(passer)
            else:
                t = np.dot(opponent.position - p1, line_vec) / l2
                t = max(0, min(1, t))
                projection = p1 + t * line_vec
                dist_to_line = opponent.position.distanceTo(projection)
            
            if dist_to_line < self.radiusAvg:
                return False
        
        return True
    
# MARK: CloseTo
class CloseTo(Constraint):
    def __init__(self, args):
        self.obj = args.get('obj', None)
        self.ref = args.get('ref', None)
        self.max = args.get('max', None)
        self.maxAvg = self.max.get('avg', None) if self.max else None

    def __call__(self, scene, sample):
        obj_list = findObj(self.obj, scene.objects)
        ref_list = findObj(self.ref, scene.objects)

        if not obj_list or not ref_list:
            return False

        obj = obj_list[0]
        ref = ref_list[0]
        
        dist = obj.distanceTo(ref)
        
        if self.maxAvg is not None:
            return dist < self.maxAvg
        else:
            return dist < 3.0 # Default close distance

# MARK: DistanceTo
class DistanceTo(Constraint):
    def __init__(self, args):
        self.fromID = args.get('from', None)
        self.toID = args.get('to', None)
        self.min = args.get('min', None)
        self.max = args.get('max', None)
        self.operator = args.get('operator', None)

        self.minAvg = self.min.get('avg', None) if self.min else None
        self.maxAvg = self.max.get('avg', None) if self.max else None

    def __call__(self, scene, sample):
        obj_from_list = findObj(self.fromID, scene.objects)
        obj_to_list = findObj(self.toID, scene.objects)

        if not obj_from_list or not obj_to_list:
            return False

        obj_from = obj_from_list[0]
        obj_to = obj_to_list[0]
        
        dist = obj_from.distanceTo(obj_to)

        if self.operator == 'less_than':
            return dist < self.maxAvg if self.maxAvg is not None else False
        elif self.operator == 'greater_than':
            return dist > self.minAvg if self.minAvg is not None else False
        elif self.operator == 'within':
            return (self.minAvg is None or dist >= self.minAvg) and \
                   (self.maxAvg is None or dist <= self.maxAvg)
        else:
            return (self.minAvg is None or dist >= self.minAvg) and \
                   (self.maxAvg is None or dist <= self.maxAvg)

# MARK: HeightRelation
        
class HeightRelation(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.relation = args.get('relation', None)
        self.threshold = args.get('vertical_threshold', None)
        self.threshold_avg = self.threshold.get('avg') if self.threshold else None

    def __call__(self, scene, sample):
        if sample and isEgo(self.objID, scene):
            player_y = sample[1]
        else:
            player_objs = findObj(self.objID, scene.objects)
            if not player_objs:
                return False
            player_obj = player_objs[0]
            player_y = player_obj.position.y

        if self.refID:
            ref_objs = findObj(self.refID, scene.objects)
            if not ref_objs:
                return False
            ref_obj = ref_objs[0]
            ref_y = ref_obj.position.y
            value = player_y - ref_y
        else:
            value = player_y

        if self.threshold_avg is None:
            return False
        
        if self.relation == 'behind':
            return value < self.threshold_avg
        elif self.relation == 'ahead':
            return value > self.threshold_avg
        else:
            return False

class HorizontalRelation(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.relation = args.get('relation', None)
        self.horizontal_threshold = args.get('horizontal_threshold', None)
        self.threshold_avg = float(self.horizontal_threshold.get('avg')) if self.horizontal_threshold else None

    def __call__(self, scene, sample):
        if sample and isEgo(self.objID, scene):
            player_x = sample[0]
        else:
            player_objs = findObj(self.objID, scene.objects)
            if not player_objs:
                return False
            player_obj = player_objs[0]
            player_x = player_obj.position.x

        if self.refID:
            ref_objs = findObj(self.refID, scene.objects)
            if not ref_objs:
                return False
            ref_obj = ref_objs[0]
            ref_x = ref_obj.position.x
            value = player_x - ref_x
        else:
            value = player_x

        if self.threshold_avg is None:
            return False
        
        if self.relation == 'left':
            return value < -abs(self.threshold_avg)
        elif self.relation == 'right':
            return value > abs(self.threshold_avg)
        else:
            return False

# Custom Constraint (based on example interpretation)
class Pressure(Constraint):
    def __init__(self, args):
        self.player1ID = args.get('player1', None)
        self.player2ID = args.get('player2', None)

    def __call__(self, scene, sample):
        obj_list = findObj(self.player1ID, scene.objects)
        ref_list = findObj(self.player2ID, scene.objects)

        if not obj_list or not ref_list:
            return False

        obj = obj_list[0]
        ref = ref_list[0]
        
        # Check if obj's heading aligns with vector to ref within a tolerance.
        if hasattr(obj, 'heading') and hasattr(obj, 'position') and hasattr(ref, 'position'):
            vec_to_ref = ref.position - obj.position
            if np.linalg.norm(vec_to_ref) == 0:
                return False 
            obj_heading_vec = np.array([np.cos(obj.heading), np.sin(obj.heading)])
            vec_to_ref_norm = vec_to_ref / np.linalg.norm(vec_to_ref)
            
            dot_product = np.dot(obj_heading_vec, vec_to_ref_norm)
            
            return dot_product > 0.8 
        return False


A1termination_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 6.399477695297064, 'std': 0.8416729364595561}, 'max': None, 'operator': 'greater_than'})
A2termination_0 = HasPathToPass({'passer': 'Teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.043342957402023236, 'std': 0.04454568506693753}})
A1target_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 6.399477695297064, 'std': 0.8416729364595561}, 'max': None, 'operator': 'greater_than'})
A1termination_2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.172259899611368, 'std': 0.0}, 'max': None, 'operator': 'greater_than'})
A1target_2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.172259899611368, 'std': 0.0}, 'max': None, 'operator': 'greater_than'})
A1termination_5 = HasBallPossession({'player': 'Coach'})
A2termination_5 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.025909493366715, 'std': 0.015410097852564864}, 'operator': 'less_than'})
A3termination_5 = HasPathToPass({'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})
A1target_5 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 5.025909493366715, 'std': 0.015410097852564864}, 'operator': 'less_than'})
A2target_5 = CloseTo({'obj': 'Coach', 'ref': 'ball', 'max': {'avg': 11.941602839093648, 'std': 0.01539784416917822}})
A1precondition_0 = HasPathToPass({'passer': 'Teammate', 'receiver': 'Coach', 'path_width': {'avg': 0.043342957402023236, 'std': 0.04454568506693753}}) # Replaced MakePass
A1precondition_1 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A2precondition_1 = CloseTo({'obj': 'Coach', 'ref': 'opponent', 'max': {'avg': 7.551545991065808, 'std': 0.0}})
A1precondition_2 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A2precondition_2 = HasPathToPass({'passer': 'Coach', 'receiver': 'teammate', 'path_width': {'avg': 0.11392247846714056, 'std': 0.0}})
A1precondition_3 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_4 = MovingTowards({'obj': 'Teammate', 'ref': 'goal'})
A1precondition_5 = HasBallPossession({'player': 'Coach'})
A2precondition_5 = DistanceTo({'from': 'goal', 'to': 'Coach', 'min': None, 'max': {'avg': 5.025909493366715, 'std': 0.015410097852564864}, 'operator': 'less_than'})
A3precondition_5 = HasPathToPass({'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 2.0, 'std': 0.0}})

def λ_target0():
    return A1target_0.bool(simulation())

def λ_target2():
    return A1target_2.bool(simulation())

def λ_target5():
    return (A1target_5.bool(simulation()) and A2target_5.bool(simulation()))

def λ_termination0(scene, sample):
    return (A1termination_0.bool(simulation()) and A2termination_0.bool(simulation()))

def λ_termination2(scene, sample):
    return A1termination_2.bool(simulation())

def λ_termination5(scene, sample):
    return (A1termination_5.bool(simulation()) and A2termination_5.bool(simulation()) and A3termination_5.bool(simulation()))

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_0(scene, sample):
    return λ_precondition0(simulation(), sample)

def λ_precondition1(scene, sample):
    return (A1precondition_1.bool(simulation()) and ~(A2precondition_1.bool(simulation())))

def λ_precondition2(scene, sample):
    return (A1precondition_2.bool(simulation()) and A2precondition_2.bool(simulation()))

def λ_precondition3(scene, sample):
    return ~(A1precondition_3.bool(simulation()))

def λ_precondition_1_2_3(scene, sample):
    return λ_precondition1(simulation(), sample) or λ_precondition2(simulation(), sample) or λ_precondition3(simulation(), sample)

def λ_precondition4(scene, sample):
    return A1precondition_4.bool(simulation())

def λ_precondition_4(scene, sample):
    return λ_precondition4(simulation(), sample)

def λ_precondition5(scene, sample):
    return (A1precondition_5.bool(simulation()) and A2precondition_5.bool(simulation()) and A3precondition_5.bool(simulation()))

def λ_precondition_5(scene, sample):
    return λ_precondition5(simulation(), sample)

behavior CoachBehavior():
    do Speak("move to an open position to receive the pass")
    do MoveTo(λ_target0) until λ_termination0(simulation(), None)
    do Speak("wait for teammate to pass the ball")
    do Idle() until λ_precondition_0(simulation(), None)
    do Speak("get possession of the ball")
    do GetBallPossession(ball)
    do Speak("wait for the opponent to make a decision")
    do Idle() until λ_precondition_1_2_3(simulation(), None)
    if λ_precondition1(simulation(), None):
        do Speak("move away from the opponent to create space")
        do MoveTo(λ_target2) until λ_termination2(simulation(), None)
        do Speak("wait for teammate to move into a scoring position")
        do Idle() until λ_precondition_4(simulation(), None)
        do Speak("pass the ball to teammate for a shot")
        do Pass(teammate)
    elif λ_precondition2(simulation(), None):
        do Speak("pass the ball quickly to teammate")
        do Pass(teammate)
    else:
        do Speak("approach the goal for a clear shooting opportunity")
        do MoveTo(λ_target5) until λ_termination5(simulation(), None)
        do Speak("wait for a good shooting position near goal")
        do Idle() until λ_precondition_5(simulation(), None)
        do Speak("take a powerful shot at the goal")
        do Shoot(goal)


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