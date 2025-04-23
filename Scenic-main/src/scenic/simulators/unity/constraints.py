from scenic.core.vectors import Vector
from scenic.core.object_types import OrientedPoint, Point
import numpy as np

rows, cols = 34, 20
i, j = np.indices((rows, cols))

epsilon = 1e-2

def findObj(id, objects):
    if isinstance(id, str):
        key_lower = id.lower()
        return [obj for obj in objects if key_lower in obj.name.lower()]

def isEgo(id):
    return 'coach' in id.lower()

def _not(x):
    return 1 - x

def sigmoid(x):
    return 1 / (1 + np.exp(-x))

def location(vec):
    return vec.x + cols / 2, -vec.y + rows / 2

def true():
    dist = np.ones((rows, cols))
    dist[0][0] -= epsilon
    return dist

def false():
    dist = np.zeros((rows, cols))
    dist += epsilon
    dist[0][0] += epsilon
    return dist
    
# MARK: Constraint
class Constraint:
    def __init__(self, args):
        self.args = args

    def dist(self, scene, ego=False):
        return true()

# MARK: CloseTo
class _CloseTo(Constraint): # Checked for graceful failure
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
            self.max_std = self.max.get('std', 1.0)
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
    
# MARK: HeightRelation
class _HeightRelation(Constraint):
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

        print('heigh relation', self.relation, y, offset, dev)

        distances = (y + offset - i) if mirror else (i - y + offset) 
        height_relation = 1 - np.clip(distances / (dev if dev > 0 else 1), 0, 1) + epsilon

        return height_relation
    
class _SideRelation(Constraint):
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
    
class _ClearLine(Constraint):
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
    
class _DistanceTo(Constraint):
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
    
class _InZone(Constraint):

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
        
        zone_x = self.zone_x_labels.index(self.zone[0])
        zone_y = int(self.zone[1:]) - 1
        
        x_coord = j - self.width / 2 + 0.5
        y_coord = i - self.height / 2 + 0.5
        
        zone_idx_x = ((x_coord + self.width/2) // self.zone_width).astype(int)
        zone_idx_y = ((y_coord + self.height/2) // self.zone_height).astype(int)
        
        zone_field = np.where((zone_idx_x == zone_x) & (zone_idx_y == zone_y),
                                1.0, epsilon)
        
        return zone_field