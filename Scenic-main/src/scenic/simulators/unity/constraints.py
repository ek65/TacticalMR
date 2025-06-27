from scenic.core.vectors import Vector
from scenic.core.object_types import OrientedPoint, Point
import numpy as np
import builtins

rows, cols = 34, 20
i, j = np.indices((rows, cols))

epsilon = 1e-2

def findObj(id, objects):
    if isinstance(id, str):
        key_lower = id.lower()
        return [obj for obj in objects if key_lower in obj.name.lower()]

def isEgo(id):
    return 'coach' in id.lower()

def sigmoid(x):
    return 1 / (1 + np.exp(-x))

def location(vec):
    return vec.x + cols / 2, -vec.y + rows / 2 #ask about coordinate system

def true():
    dist = np.ones((rows, cols))
    dist[0][0] -= epsilon
    return dist

def false():
    dist = np.zeros((rows, cols))
    dist += epsilon
    dist[0][0] += epsilon
    return dist

import matplotlib.pyplot as plt

def bool_sample(vec, dist, min=0.1):

    max_val = dist.max()
    if max_val > 0:
        dist = dist / max_val

    x = builtins.min(builtins.max(int(vec[0]), 0), cols - 1)
    y = builtins.min(builtins.max(int(vec[1]), 0), rows - 1)

    sample = (y, x)
    value = dist[sample]

    return value > min
    
# MARK: Constraint
class Constraint:
    def __init__(self, args):
        self.args = args

    def dist(self, scene, ego=False):
        return true()
    
    def bool(self, scene):
        return True
    
class CompositeConstraint(Constraint):
    def __init__(self, left: Constraint, right: Constraint, op: str):
        self.left = left
        self.right = right
        assert op in ('AND','OR')
        self.op = op

    def dist(self, scene, ego=False):
        d1 = self.left.dist(scene, ego)
        d2 = self.right.dist(scene, ego)
        if self.op == 'AND':
            return np.exp(np.log(d1) + np.log(d2))
        else:
            return d1 + d2 #ask jorge cap at 1
        
    def bool(self, scene):
        b1 = self.left.bool(scene)
        b2 = self.right.bool(scene)
        if self.op == 'AND':
            return b1 and b2
        else:
            return b1 or b2

        
class NegationConstraint(Constraint):
    def __init__(self, inner: Constraint):
        self.inner = inner

    def dist(self, scene, ego=False):
        d = self.inner.dist(scene, ego)
        return np.clip(1 - d, epsilon, None)
    
    def bool(self, scene):
        b = self.inner.bool(scene)
        return not b
    
    
Constraint.__and__ = lambda self, other: CompositeConstraint(self, other, 'AND')
Constraint.__or__ = lambda self, other: CompositeConstraint(self, other, 'OR')
Constraint.__invert__ = lambda self: NegationConstraint(self)


# MARK: CloseTo
class CloseTo(Constraint): # Checked for graceful failure
    def __init__(self, args):
        self.obj = args.get('obj', None)
        self.ref = args.get('ref', None)
        self.max = args.get('max', None)

        if isinstance(self.max, (int, float)):
            self.max_avg = self.max
            self.max_std = 1.0
        elif self.max is not None:
            self.max = args.get('max', None)
            self.max_avg = self.max.get('avg', 3.0)
            self.max_std = self.max.get('std', 1.0) #should be using std
        else:
            self.max = {'avg': 3.0, 'std': 1.0}
            self.max_avg = 3.0
            self.max_std = 1.0

    def dist(self, scene, ego=False):

        if ego and not isEgo(self.obj):
            return true()
        
        ref = findObj(self.ref, scene.objects)

        if not ref:
            return false()
        
        print('close to', ref[0].position)
        x, y = location(ref[0].position)
        radius = self.max_avg

        print('close to', x, y, radius)

        distances = np.sqrt((i - y)**2 + (j - x)**2)
        close_to = np.exp(-distances**2 / (2 * radius**2)) + epsilon 

        return close_to
    
    def bool(self, scene):

        obj = findObj(self.obj, scene.objects)

        if not obj:
            return false()
        
        dist = self.dist(scene)
        sample = location(obj[0].position)

        return bool_sample(sample, dist)

    
# MARK: HeightRelation
class HeightRelation(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.relation = args.get('relation', None)
        self.threshold = args.get('height_threshold', None)
        if self.threshold is None:
            self.threshold = {'avg': 2.0, 'std': 1.0}
        self.threshold_avg = self.threshold.get('avg')
        self.threshold_std = self.threshold.get('std')

    def dist(self, scene, ego=False):

        if ego and not isEgo(self.objID):
            return true()

        ref = findObj(self.refID, scene.objects)

        if ref:
            x, y = location(ref[0].position)
        else:
            x, y = location(Vector(0, 0))
        
        mirror = (self.relation == 'below')

        offset = self.threshold_avg
        dev = self.threshold_std

        print('height relation', self.relation, y, offset, dev)

        distances = (y + offset - i) if mirror else (i - y + offset) 
        height_relation = 1 - np.clip(distances / (dev if dev > 0 else 1), 0, 1) + epsilon

        return height_relation
    
    def bool(self, scene):

        obj = findObj(self.objID, scene.objects)

        if not obj:
            return false()
        
        dist = self.dist(scene)
        sample = location(obj[0].position)

        return bool_sample(sample, dist)
    
# MARK: HorizontalRelation
class HorizontalRelation(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.relation = args.get('relation', None)
        self.threshold = args.get('horizontal_threshold', None)
        if self.threshold is None:
            self.threshold = {'avg': 2.0, 'std': 1.0}
        self.threshold_avg = self.threshold.get('avg') 
        self.threshold_std = self.threshold.get('std')

    def dist(self, scene, ego=False):

        if ego and not isEgo(self.objID):
            return true()

        ref = findObj(self.refID, scene.objects)

        if ref:
            x, y = location(ref[0].position)
        else:
            x, y = location(Vector(0, 0))
        
        mirror = (self.relation == 'right')

        offset = self.threshold_avg
        dev = self.threshold_std

        print('side relation', self.relation, mirror, x, offset, dev)

        distances = (x + offset - j) if mirror else (j - x + offset) 
        side_relation = 1 - np.clip(distances / (dev if dev > 0 else 1), 0, 1) + epsilon

        return side_relation
    
    def bool(self, scene):

        obj = findObj(self.objID, scene.objects)

        if not obj:
            return false()
        
        dist = self.dist(scene)
        sample = location(obj[0].position)

        return bool_sample(sample, dist)


# MARK: HasPath  
class HasPath(Constraint):
    def __init__(self, args):
        self.passerID = args.get('obj1', None)
        self.receiverID = args.get('obj2', None)
        self.radius = args.get('path_width', None)
        self.radiusAvg = self.radius.get('avg', 0.0)
        self.radiusStd = self.radius.get('std', 1.0)

    def dist(self, scene, ego=False):

        if ego and not (isEgo(self.passerID) or isEgo(self.receiverID)):
            return true()

        passer = findObj(self.passerID, scene.objects)
        receiver = findObj(self.receiverID, scene.objects)

        obstacles = []
        for obj in scene.objects:
            # TODO: Should check for team
            if 'opponent' in obj.name.lower():
                obstacles.append(location(obj.position))

        if not (passer and receiver):
            return false()
        
        xp, yp = location(passer[0].position)
        xr, yr = location(receiver[0].position)

        print((xp, yp), (xr, yr), obstacles, self.radiusAvg, self.radiusStd)

        safety_p = self.make_dist((xp, yp), (xr, yr), obstacles, self.radiusAvg, self.radiusStd)
        safety_r = self.make_dist((xr, yr), (xp, yp), obstacles, self.radiusAvg, self.radiusStd)
            
        return safety_p * safety_r
    
    def make_dist(self, start, end, obstacles, radius, dev=None):

        if dev is None:
            dev = radius / 3.0

        T_x, T_y = end

        v_x, v_y = T_y - i, T_x - j
        norm_v = np.sqrt(v_x**2 + v_y**2) + epsilon

        safety = np.ones((rows, cols))
        
        for (O_y, O_x) in obstacles:

            w_x, w_y = O_x - i, O_y - j
            
            dot = w_x * v_x + w_y * v_y
            t = dot / (norm_v**2)
            
            d = np.where(t < 0, 
                        np.sqrt((O_x - i)**2 + (O_y - j)**2),
                        np.where(t > 1, 
                                np.sqrt((O_x - T_x)**2 + (O_y - T_y)**2),
                                np.abs(v_x * w_y - v_y * w_x) / norm_v))

            opponent_safety = sigmoid((d - radius) / dev)
            safety = np.minimum(safety, opponent_safety)
            
        return safety
    
    def bool(self, scene):

        passer = findObj(self.passerID, scene.objects)

        if not passer:
            return false()
        
        dist = self.dist(scene)
        sample = location(passer[0].position)

        return bool_sample(sample, dist, min=0.4)
    

# MARK: DistanceTo  
class DistanceTo(Constraint):
    def __init__(self, args):
        self.fromID = args.get('from', None)
        self.toID = args.get('to', None)
        self.min = args.get('min', None)
        self.max = args.get('max', None)
        self.operator = args.get('operator', None)

        if self.min is not None:
            self.minAvg = self.min.get('avg', None)
        else:
            self.minAvg = {'avg': 3.0, 'std': 1.0}
        if self.max is not None:
            self.maxAvg = self.max.get('avg', None)
        else:
            self.maxAvg = {'avg': 3.0, 'std': 1.0}

    def dist(self, scene, ego=False):

        if ego and not isEgo(self.fromID):
            return true()

        ref = findObj(self.toID, scene.objects)

        if not ref:
            return false()

        x, y = location(ref[0].position)
        distances = np.sqrt((i - y)**2 + (j - x)**2)

        print('distance to', x, y, self.minAvg, self.maxAvg, self.operator)

        if self.operator == 'within':
            mu = (self.minAvg + self.maxAvg) / 2.0
            sigma = (self.maxAvg - self.minAvg) / 2.0
            mask = (distances >= self.minAvg) & (distances <= self.maxAvg)
            bump = np.exp(-((distances - mu)**2) / (2 * sigma**2))
            map = np.where(mask, bump + epsilon, epsilon)

        elif self.operator == 'less_than':
            sigma = self.maxAvg
            mask = distances < self.maxAvg
            bump = np.exp(-(distances**2) / (2 * sigma**2))
            map = np.where(mask, bump + epsilon, epsilon)

        elif self.operator == 'greater_than':
            sigma = self.minAvg
            mask = distances > self.minAvg
            bump = np.exp(-((distances - self.minAvg)**2) / (2 * sigma**2))
            map = np.where(mask, bump + epsilon, epsilon)

        else:
            print('Invalid operator.')
            return false()

        return map

#     def dist(self, scene, ego=False):
#         if ego and not isEgo(self.fromID):
#             return True
# 
#         targets = findObj(self.toID, scene.objects)
#         if not targets:
#             return False
# 
#         sources = findObj(self.fromID, scene.objects)
#         if not sources:
#             return False
# 
#         src_x, src_y = location(sources[0].position)
#         tgt_x, tgt_y = location(targets[0].position)
# 
#         d = np.hypot(src_x - tgt_x, src_y - tgt_y)
# 
#         if self.operator == 'within':
#             return self.minAvg <= d <= self.maxAvg
#         elif self.operator == 'less_than':
#             return d < self.maxAvg
#         elif self.operator == 'greater_than':
#             return d > self.minAvg
#         else:
#             return False
    
    def bool(self, scene):

        obj = findObj(self.fromID, scene.objects)

        if not obj:
            return false()

        dist = self.dist(scene)
        sample = location(obj[0].position)

        return bool_sample(sample, dist)

#     def bool(self, scene):
#             # Simply return the same boolean that .dist would
#             return self.dist(scene)
    

# MARK: InZone
class InZone(Constraint):

    width, height = cols, rows
    num_zones_x, num_zones_y = 4, 5
    zone_width = width / num_zones_x
    zone_height = height / num_zones_y

    zone_x_labels = ['A', 'B', 'C', 'D']
    zone_y_labels = ['1', '2', '3', '4', '5']

    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.zone = args.get('zone', None)

    def dist(self, scene, ego=False):

        if ego and not isEgo(self.objID):
            return true()
        
        if not self.zone:
            return false()
        
        print('in zone', self.zone)

        if isinstance(self.zone, list):
            zone_str = self.zone[0]
        else:
            zone_str = self.zone

        print('zone', zone_str, zone_str[0], zone_str[1])
        
        zone_x = self.zone_x_labels.index(zone_str[0])
        zone_y = int(zone_str[1]) - 1
        
        x_coord = j - self.width / 2 + 0.5
        y_coord = i - self.height / 2 + 0.5
        
        zone_idx_x = ((x_coord + self.width/2) // self.zone_width).astype(int)
        zone_idx_y = ((y_coord + self.height/2) // self.zone_height).astype(int)
        
        zone_field = np.where((zone_idx_x == zone_x) & (zone_idx_y == zone_y),
                                1.0, epsilon)
        
        return zone_field

    def bool(self, scene):

        obj = findObj(self.objID, scene.objects)

        if not obj:
            return false()
        
        dist = self.dist(scene)
        sample = location(obj[0].position)

        return bool_sample(sample, dist)
    
# MARK: HasBallPosession
class HasBallPossession(Constraint):
    def __init__(self, args):
        self.playerID = args.get('player', None)

    def dist(self, scene, ego=False):

        if ego and not isEgo(self.playerID):
            return true()
        
        return true() if self.bool(scene) else false()
        
    def bool(self, scene):
        
        player = findObj(self.playerID, scene.objects)
        
        if not player:
            return False
        
        return player[0].gameObject.ballPossession
    

# MARK: MovingTowards
class MovingTowards(Constraint):
    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)

    def dist(self, scene, ego=False):

        if ego and not isEgo(self.objID):
            return true()
        
        return true() if self.bool(scene) else false()
    
    def bool(self, scene):

        obj = findObj(self.objID, scene.objects)
        ref = findObj(self.refID, scene.objects)

        if not (obj and ref):
            return False

        distance = lambda pos1, pos2: np.sqrt((pos1.x - pos2.x) ** 2 + (pos1.y - pos2.y) ** 2)
        current_distance = distance(obj[0].position, ref[0].position)
        previous_distance = distance(obj[0].prevPosition, ref[0].prevPosition)

        return previous_distance - current_distance <= -0.05
    

# MARK: Pressure
class Pressure(Constraint):
    def __init__(self, args={}):
        self.player1 = args.get('player1', None)
        self.player2 = args.get('player2', None)

    def dist(self, scene, ego=False):

        if ego and not isEgo(self.player1):
            return true()
        
        return true() if self.bool(scene) else false()

    def bool(self, scene):

        player1 = findObj(self.player1, scene.objects)
        player2 = findObj(self.player2, scene.objects)

        if not (player1 and player2):
            return False

        behav = player1[0].gameObject.behavior.lower()
        name = player2[0].name.lower()

        if 'follow' in behav and name in behav:
            return True
        return False
    

# MARK: MakePass
class MakePass(Constraint):
    def __init__(self, args={}):
        self.player = args.get('player', None)

    def dist(self, scene, ego=False):

        if ego and not isEgo(self.player):
            return true()
        
        return true() if self.bool(scene) else false()

    def bool(self, scene):

        player = findObj(self.player, scene.objects)

        if not player:
            return False

        behav = player[0].gameObject.behavior

        if behav and 'pass' in behav.lower():
            return True

        return False
        
    # MARK: HandRaised
    class HandRaised(Constraint):
        def __init__(self, args):
            self.objID = args.get('player', args.get('obj', None))
    
        def dist(self, scene, ego=False):
            if ego and not isEgo(self.objID):
                return true()
    
            objs = findObj(self.objID, scene.objects)
            if not objs:
                return false()
    
            beh = objs[0].gameObject.behavior.lower()

            raised = ('raise hand' in beh or 'raise hand' in beh)
            return true() if raised else false()
    
        def bool(self, scene):
            return bool(self.dist(scene))