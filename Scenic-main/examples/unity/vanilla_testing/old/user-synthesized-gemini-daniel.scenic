from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

import numpy as np

# Utility functions (assuming these are available in the Scenic environment)
def findObj(id, objects):
    if isinstance(id, str):
        key_lower = id.lower()
        return [obj for obj in objects if key_lower in obj.name.lower()]
    return []

def isEgo(id, scene):
    return id.lower() == scene.egoObject.name.lower()

# Constraint Class Definitions (as provided in the Scenic documentation/API)
class Constraint:
    def __init__(self, args):
        self.args = args

    def __call__(self, scene, sample):
        pass

class HasBallPossession(Constraint):
    def __init__(self, args):
        self.playerID = args.get('player', None)

    def __call__(self, scene, sample=None):
        player_objs = findObj(self.playerID, scene.objects)
        if player_objs:
            return player_objs[0].hasBall
        return False

class InZone(Constraint):
    FIELD_WIDTH, FIELD_HEIGHT = 20, 34
    NUM_ZONES_X, NUM_ZONES_Y = 4, 5
    ZONE_WIDTH = FIELD_WIDTH / NUM_ZONES_X
    ZONE_HEIGHT = FIELD_HEIGHT / NUM_ZONES_Y

    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.zone = args.get('zone', None)

    def __call__(self, scene, sample=None):
        obj_list = findObj(self.objID, scene.objects)
        if not obj_list: return False
        obj = obj_list[0]
        
        col_char = self.zone[0].upper()
        row_num = int(self.zone[1:]) - 1 
        
        col_idx = ord(col_char) - ord('A')
        
        min_x = -InZone.FIELD_WIDTH / 2 + col_idx * InZone.ZONE_WIDTH
        max_x = min_x + InZone.ZONE_WIDTH
        min_y = row_num * InZone.ZONE_HEIGHT
        max_y = min_y + InZone.ZONE_HEIGHT
        
        return (obj.position.x >= min_x and obj.position.x < max_x and
                obj.position.y >= min_y and obj.position.y < max_y)

class MovingTowards(Constraint):
    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)

    def __call__(self, scene, sample=None):
        obj_list = findObj(self.objID, scene.objects)
        ref_list = findObj(self.refID, scene.objects)
        if not obj_list or not ref_list: return False
        obj = obj_list[0]
        ref = ref_list[0]
        
        if obj.velocity is None: return False
        
        vec_to_ref = ref.position - obj.position
        
        if np.linalg.norm(obj.velocity.toVector()) == 0: return False
        if np.linalg.norm(vec_to_ref.toVector()) == 0: return False
        
        unit_vel = obj.velocity.toVector() / np.linalg.norm(obj.velocity.toVector())
        unit_to_ref = vec_to_ref.toVector() / np.linalg.norm(vec_to_ref.toVector())
        
        return np.dot(unit_vel, unit_to_ref) > 0.5 

class HasPathToPass(Constraint):
    def __init__(self, args={}):
        self.passerID = args.get('passer', None)
        self.receiverID = args.get('receiver', None)
        self.radius = args.get('path_width', None)
        self.radiusAvg = self.radius.get('avg', 0.0) if self.radius else 0.0

    def __call__(self, scene, sample=None):
        passer_list = findObj(self.passerID, scene.objects)
        receiver_list = findObj(self.receiverID, scene.objects)
        if not passer_list or not receiver_list: return False
        
        passer = passer_list[0]
        receiver = receiver_list[0]
        
        p1 = passer.position.toVector()
        p2 = receiver.position.toVector()
        
        for obj in scene.objects:
            if 'opponent' in obj.name.lower() and obj != passer and obj != receiver:
                line_vec = p2 - p1
                if np.linalg.norm(line_vec) == 0:
                    dist_to_line = np.linalg.norm(obj.position.toVector() - p1)
                else:
                    t = np.dot(obj.position.toVector() - p1, line_vec) / np.dot(line_vec, line_vec)
                    t = max(0, min(1, t))
                    closest_point_on_segment = p1 + t * line_vec
                    dist_to_line = np.linalg.norm(obj.position.toVector() - closest_point_on_segment)
                
                if dist_to_line < self.radiusAvg:
                    return False
        return True

class CloseTo(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.max = float(args.get('max', None))

    def __call__(self, scene, sample=None):
        obj_list = findObj(self.objID, scene.objects)
        ref_list = findObj(self.refID, scene.objects)
        if not obj_list or not ref_list: return False
        obj = obj_list[0]
        ref = ref_list[0]
        return obj.distanceTo(ref) <= self.max

class DistanceTo(Constraint):
    def __init__(self, args):
        self.fromID = args.get('from', None)
        self.toID = args.get('to', None)
        self.min = args.get('min', None)
        self.max = args.get('max', None)
        self.operator = args.get('operator', None)

    def __call__(self, scene, sample=None):
        obj1_list = findObj(self.fromID, scene.objects)
        obj2_list = findObj(self.toID, scene.objects)
        if not obj1_list or not obj2_list: return False
        obj1 = obj1_list[0]
        obj2 = obj2_list[0]
        
        dist = obj1.distanceTo(obj2)
        
        if self.operator == 'within':
            return (self.min is None or dist >= self.min) and \
                   (self.max is None or dist <= self.max)
        elif self.operator == 'less_than':
            return self.max is not None and dist < self.max
        elif self.operator == 'greater_than':
            return self.min is not None and dist > self.min
        return False

class HeightRelation(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.relation = args.get('relation', None)
        self.threshold = args.get('vertical_threshold', None)
        self.threshold_avg = self.threshold.get('avg') if self.threshold else None

    def __call__(self, scene, sample=None):
        player_objs = findObj(self.objID, scene.objects)
        if not player_objs: return False
        player_obj = player_objs[0]
        player_y = player_obj.position.y

        if self.refID:
            ref_objs = findObj(self.refID, scene.objects)
            if not ref_objs: return False
            ref_obj = ref_objs[0]
            ref_y = ref_obj.position.y
            value = player_y - ref_y
        else:
            value = player_y

        if self.threshold_avg is None: return False
        
        if self.relation == 'behind':
            return value < -self.threshold_avg
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

    def __call__(self, scene, sample=None):
        player_objs = findObj(self.objID, scene.objects)
        if not player_objs: return False
        player_obj = player_objs[0]
        player_x = player_obj.position.x

        if self.refID:
            ref_objs = findObj(self.refID, scene.objects)
            if not ref_objs: return False
            ref_obj = ref_objs[0]
            ref_x = ref_obj.position.x
            value = player_x - ref_x
        else:
            value = player_x

        if self.threshold_avg is None: return False
        
        if self.relation == 'left':
            return value < -self.threshold_avg
        elif self.relation == 'right':
            return value > self.threshold_avg
        return False


# Necessary constraint class instantiations
has_ball_possession_teammate = HasBallPossession(args={'player': 'teammate'})
has_ball_possession_coach = HasBallPossession(args={'player': 'Coach'})
opponent_close_to_teammate = CloseTo(args={'obj': 'opponent', 'ref': 'teammate', 'max': 3.0})
opponent_close_to_coach = CloseTo(args={'obj': 'opponent', 'ref': 'Coach', 'max': 3.0})
ball_close_to_coach = CloseTo(args={'obj': 'ball', 'ref': 'Coach', 'max': 0.5})
coach_close_to_goal = CloseTo(args={'obj': 'Coach', 'ref': 'goal', 'max': 4.0})
ball_in_goal = CloseTo(args={'obj': 'ball', 'ref': 'goal', 'max': 1.0})

coach_left_of_opponent = HorizontalRelation(args={'obj': 'Coach', 'ref': 'opponent', 'relation': 'left', 'horizontal_threshold': {'avg': 4.0, 'std': 1.0}})
coach_right_of_opponent = HorizontalRelation(args={'obj': 'Coach', 'ref': 'opponent', 'relation': 'right', 'horizontal_threshold': {'avg': 4.0, 'std': 1.0}})

teammate_path_to_coach = HasPathToPass(args={'passer': 'teammate', 'receiver': 'Coach', 'path_width': {'avg': 1.0, 'std': 0.2}})
coach_path_to_teammate = HasPathToPass(args={'passer': 'Coach', 'receiver': 'teammate', 'path_width': {'avg': 1.0, 'std': 0.2}})
coach_path_to_goal = HasPathToPass(args={'passer': 'Coach', 'receiver': 'goal', 'path_width': {'avg': 1.0, 'std': 0.2}})
teammate_path_to_goal = HasPathToPass(args={'passer': 'teammate', 'receiver': 'goal', 'path_width': {'avg': 1.0, 'std': 0.2}})

teammate_moving_towards_goal = MovingTowards(args={'obj': 'teammate', 'ref': 'goal'})


behavior CoachBehavior():
    do Speak("Coach: Waiting for my teammate to gain possession of the ball.")
    wait until has_ball_possession_teammate(scene)

    while True:
        if has_ball_possession_teammate(scene) and opponent_close_to_teammate(scene):
            do Speak("Coach: Teammate is under pressure! I need to get open for a pass.")
            do MoveTo(
                λ_dest=lambda: (coach_left_of_opponent(scene) or coach_right_of_opponent(scene)) and
                               teammate_path_to_coach(scene) and
                               not opponent_close_to_coach(scene),
                λ_termination=lambda: has_ball_possession_coach(scene),
                λ_precondition=lambda: has_ball_possession_teammate(scene)
            )
            if not has_ball_possession_coach(scene):
                do Speak("Coach: I'm in position! Now, just waiting for the pass.")
                do GetBallPossession(
                    λ_termination=lambda: has_ball_possession_coach(scene),
                    λ_precondition=lambda: ball_close_to_coach(scene)
                )
        elif has_ball_possession_coach(scene):
            if opponent_close_to_coach(scene):
                do Speak("Coach: Opponent is pressing me! Pass it to the teammate.")
                do Pass(
                    target=teammate,
                    λ_termination=lambda: has_ball_possession_teammate(scene),
                    λ_precondition=lambda: coach_path_to_teammate(scene) and
                                           teammate_path_to_goal(scene)
                )
            else:
                do Speak("Coach: I have space! Driving towards the goal for a clear shot.")
                do MoveTo(
                    λ_dest=lambda: coach_close_to_goal(scene) and coach_path_to_goal(scene),
                    λ_termination=lambda: coach_close_to_goal(scene),
                    λ_precondition=lambda: has_ball_possession_coach(scene)
                )
                do Speak("Coach: Perfect position! It's time to shoot for the goal!")
                do Shoot(
                    λ_termination=lambda: ball_in_goal(scene),
                    λ_precondition=lambda: has_ball_possession_coach(scene) and coach_close_to_goal(scene)
                )
        else:
            do Speak("Coach: Observing the play, waiting for my opportunity.")
            wait


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