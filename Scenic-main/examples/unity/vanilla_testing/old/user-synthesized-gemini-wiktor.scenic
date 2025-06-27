from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

import numpy as np

# Helper functions for constraint classes
def findObj(id, objects):
    if isinstance(id, str):
        key_lower = id.lower()
        return [obj for obj in objects if key_lower in obj.name.lower()]
    return []

def isEgo(id, scene):
    if scene.egoObject is None:
        return False
    return id.lower() == scene.egoObject.name.lower()
    
# Constraint class definitions
class Constraint:
    def __init__(self, args):
        self.args = args

    def __call__(self, sample, scene):
        pass

class HasBallPossession(Constraint):
    def __init__(self, args):
        self.playerID = args.get('player', None)
    def __call__(self, scene, sample):
        player_objs = findObj(self.playerID, scene.objects)
        ball_objs = findObj('ball', scene.objects)
        if not player_objs or not ball_objs:
            return False
        player_obj = player_objs[0]
        ball_obj = ball_objs[0]
        return ball_obj.heldBy is player_obj
    
class InZone(Constraint):
    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.zone = args.get('zone', None)
    def __call__(self, scene, sample):
        return False # Not directly used in this behavior logic

class MovingTowards(Constraint):
    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
    def __call__(self, scene, sample):
        return False # Not explicitly used for decision in this behavior

class HasPathToPass(Constraint):
    def __init__(self, args={}):
        self.passerID = args.get('passer', None)
        self.receiverID = args.get('receiver', None)
        self.radius = args.get('path_width', None)
        self.radiusAvg = self.radius.get('avg', 0.0)
        self.radiusStd = self.radius.get('std', 1.0)
    def __call__(self, scene, sample):
        passer_objs = findObj(self.passerID, scene.objects)
        receiver_objs = findObj(self.receiverID, scene.objects)
        opponent_objs = findObj('opponent', scene.objects)

        if not passer_objs or not receiver_objs or not opponent_objs:
            return False
        
        passer = passer_objs[0]
        receiver = receiver_objs[0]
        opponent = opponent_objs[0]

        line_p1 = passer.position
        line_p2 = receiver.position
        
        line_vec = line_p2 - line_p1
        line_length_sq = line_vec.dot(line_vec)
        
        if line_length_sq == 0:
            return (opponent.position - passer.position).length > self.radiusAvg
        
        t = np.clip((opponent.position - line_p1).dot(line_vec) / line_length_sq, 0.0, 1.0)
        
        projection = line_p1 + t * line_vec
        dist = (opponent.position - projection).length
        
        return dist > self.radiusAvg

class CloseTo(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.max = float(args.get('max', None).get('avg'))

    def __call__(self, scene, sample):
        obj_objs = findObj(self.objID, scene.objects)
        ref_objs = findObj(self.refID, scene.objects)
        if not obj_objs or not ref_objs: return False
        obj = obj_objs[0]
        ref = ref_objs[0]
        
        return (obj.position - ref.position).length < self.max
        
class DistanceTo(Constraint):
    def __init__(self, args):
        self.fromID = args.get('from', None)
        self.toID = args.get('to', None)
        self.min = args.get('min', None)
        self.max = args.get('max', None)
        self.operator = args.get('operator', None)

        self.minAvg = self.min.get('avg') if self.min else None
        self.maxAvg = self.max.get('avg') if self.max else None

    def __call__(self, scene, sample):
        from_objs = findObj(self.fromID, scene.objects)
        to_objs = findObj(self.toID, scene.objects)
        if not from_objs or not to_objs: return False
        from_obj = from_objs[0]
        to_obj = to_objs[0]

        dist = (from_obj.position - to_obj.position).length

        if self.operator == 'within':
            return (self.minAvg is None or dist >= self.minAvg) and \
                   (self.maxAvg is None or dist <= self.maxAvg)
        elif self.operator == 'less_than':
            return dist < self.maxAvg
        elif self.operator == 'greater_than':
            return dist > self.minAvg
        return False
        
class HeightRelation(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.relation = args.get('relation', None)
        self.threshold = args.get('vertical_threshold', None)
        self.threshold_avg = self.threshold.get('avg') if self.threshold else None

    def __call__(self, scene, sample):
        obj_objs = findObj(self.objID, scene.objects)
        if not obj_objs: return False
        obj_y = obj_objs[0].position.y

        if self.refID:
            ref_objs = findObj(self.refID, scene.objects)
            if not ref_objs: return False
            ref_y = ref_objs[0].position.y
            value = obj_y - ref_y
        else:
            value = obj_y

        if self.threshold_avg is None: return False
        
        if self.relation == 'behind':
            return value < -self.threshold_avg
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
        obj_objs = findObj(self.objID, scene.objects)
        if not obj_objs: return False
        obj_x = obj_objs[0].position.x

        if self.refID:
            ref_objs = findObj(self.refID, scene.objects)
            if not ref_objs: return False
            ref_x = ref_objs[0].position.x
            value = obj_x - ref_x
        else:
            value = obj_x

        if self.threshold_avg is None: return False
        
        if self.relation == 'left':
            return value < -self.threshold_avg
        elif self.relation == 'right':
            return value > self.threshold_avg
        else:
            return False

# Define lambda functions for preconditions, terminations, and targets.
# These functions dynamically access object positions via the 'scene' object,
# assuming objects like Teammate, Opponent are globally available from the scenario.

# Lambda destination for MoveTo: Coach moves to an open position relative to Teammate and Opponent.
# These predicates define target zones and take a potential position 'p_pos'.
lambda_dest_move_right = lambda p_pos: \
    p_pos.x > (Teammate.position.x + 3) and \
    p_pos.x < (Teammate.position.x + 8) and \
    p_pos.y > (Opponent.position.y - 2) and p_pos.y < (Opponent.position.y + 4)

lambda_dest_move_left = lambda p_pos: \
    p_pos.x < (Teammate.position.x - 3) and \
    p_pos.x > (Teammate.position.x - 8) and \
    p_pos.y > (Opponent.position.y - 2) and p_pos.y < (Opponent.position.y + 4)

# λ_precondition functions
lambda_precondition_initial_move = lambda scene: \
    HasBallPossession(player='teammate')(scene, None) and \
    CloseTo(obj='opponent', ref='teammate', max={'avg': 3.0, 'std': 0.5})(scene, None)

lambda_precondition_get_ball = lambda scene: \
    DistanceTo(from='Ball', to='Coach', max={'avg': 0.5, 'std': 0.1})(scene, None)

lambda_precondition_pass = lambda scene: \
    HasBallPossession(player='Coach')(scene, None) and \
    CloseTo(obj='opponent', ref='Coach', max={'avg': 3.0, 'std': 0.5})(scene, None) and \
    HasPathToPass(passer='Coach', receiver='teammate', path_width={'avg': 1.5, 'std': 0.2})(scene, None)

lambda_precondition_shoot = lambda scene: \
    HasBallPossession(player='Coach')(scene, None) and \
    not CloseTo(obj='opponent', ref='Coach', max={'avg': 3.0, 'std': 0.5})(scene, None) and \
    CloseTo(obj='opponent', ref='teammate', max={'avg': 3.0, 'std': 0.5})(scene, None)

# λ_termination functions
lambda_termination_initial_move = lambda scene: \
    HasPathToPass(passer='teammate', receiver='Coach', path_width={'avg': 1.5, 'std': 0.2})(scene, None)

lambda_termination_get_ball = lambda scene: \
    HasBallPossession(player='Coach')(scene, None)

lambda_termination_pass = lambda scene: \
    HasBallPossession(player='teammate')(scene, None)

lambda_termination_shoot = lambda scene: \
    DistanceTo(from='Ball', to='Goal', max={'avg': 1.0, 'std': 0.2}, operator='less_than')(scene, None)

# λ_target functions
lambda_target_pass_to_teammate = lambda: Teammate

lambda_target_shoot_at_goal = lambda: Goal

behavior CoachBehavior():
    # Phase 1: Initial Positioning
    do Speak("Teammate has the ball; opponent is pressuring. Coach needs to move into an open position to receive a pass.")
    
    # Decide to move left or right based on demo patterns
    if Uniform(0, 1) < 0.5:
        do Speak("Move to the right flank now to create a clear passing lane for your teammate.")
        do MoveTo(lambda_dest_move_right,
                  precondition=lambda_precondition_initial_move,
                  termination=lambda_termination_initial_move)
    else:
        do Speak("Move to the left flank now to create a clear passing lane for your teammate.")
        do MoveTo(lambda_dest_move_left,
                  precondition=lambda_precondition_initial_move,
                  termination=lambda_termination_initial_move)
    
    do Speak("Excellent positioning! Now, wait for your teammate to pass the ball.")
    do Wait(termination=lambda_precondition_get_ball)
    
    do Speak("Get ball possession quickly after receiving the pass.")
    do GetBallPossession(precondition=lambda_precondition_get_ball,
                         termination=lambda_termination_get_ball)
    
    do Speak("You have the ball! Now, assess the opponent's movement to decide your next action.")
    
    # Phase 3: Decision and Execution (Pass or Shoot)
    # Check if opponent is pressuring Coach or still with teammate.
    if CloseTo(obj='opponent', ref='Coach', max={'avg': 3.0, 'std': 0.5})(scene, None):
        do Speak("Opponent is closing in on you! Pass the ball back to your teammate for a better attack opportunity.")
        do Pass(lambda_target_pass_to_teammate,
                precondition=lambda_precondition_pass,
                termination=lambda_termination_pass)
    else:
        do Speak("Opponent is focused on your teammate. You have a clear shot! Go for the goal!")
        do Shoot(lambda_target_shoot_at_goal,
                 precondition=lambda_precondition_shoot,
                 termination=lambda_termination_shoot)


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