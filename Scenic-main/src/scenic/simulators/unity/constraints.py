from scenic.core.vectors import Vector
from scenic.core.object_types import OrientedPoint, Point
from shapely.geometry import LineString, Point
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

# def sigmoid(x):
#     return 1 / (1 + np.exp(-5 * x))  # sharper transition

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
import matplotlib.patches as patches

def bool_sample(vec, dist, min=0.1):

    max_val = dist.max()
    if max_val > 0:
        dist = dist / max_val

    x = builtins.min(builtins.max(int(vec[0]), 0), cols - 1)
    y = builtins.min(builtins.max(int(vec[1]), 0), rows - 1)

    sample = (y, x)
    value = dist[sample]

    return value > min

def create_player_exclusion_mask(scene, exclusion_radius=1.5):
    """Create a mask that excludes positions within exclusion_radius meters of any player.
    
    Args:
        scene: The current scene containing objects
        exclusion_radius: Radius in meters around players to exclude (default: 1.5)
    
    Returns:
        numpy.ndarray: Boolean mask where True indicates allowed positions
    """
    # Create a mask initialized to True (allow all positions)
    mask = np.ones((rows, cols), dtype=bool)
    
    # For each player in the scene, exclude positions within exclusion_radius
    for obj in scene.objects:
        if hasattr(obj, 'gameObjectType') and obj.gameObjectType == 'player':
            player_x, player_y = location(obj.position)
            
            # Calculate distances from this player to all grid positions
            player_distances = np.sqrt((i - player_y)**2 + (j - player_x)**2)
            
            # Mark positions within exclusion_radius as excluded (False)
            mask = mask & (player_distances >= exclusion_radius)
    
    return mask
    
# MARK: Constraint
class Constraint:
    def __init__(self, args):
        self.args = args

    def dist(self, scene, ego=False):
        return true()
    
    def bool(self, scene):
        return True
    
    def __and__(self, other):
        print(f"DEBUG: __and__ called with {type(self)} and {type(other)}")
        return CompositeConstraint(self, other, 'AND')
    
    def __or__(self, other):
        return CompositeConstraint(self, other, 'OR')
    
    def __invert__(self):
        return NegationConstraint(self)
    
class CompositeConstraint(Constraint):
    def __init__(self, left: Constraint, right: Constraint, op: str):
        self.left = left
        self.right = right
        assert op in ('AND','OR')
        self.op = op

    def dist(self, scene, ego=False):
        print(f"DEBUG: CompositeConstraint.dist() called with op={self.op}")
        d1 = self.left.dist(scene, ego)
        d2 = self.right.dist(scene, ego)
        if self.op == 'AND':
            return np.exp(np.log(d1) + np.log(d2))
        else:
            return d1 + d2 - np.exp(np.log(d1) + np.log(d2)) #P(A U B) = P(A) + P(B) - P(A n B)
        
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
    
    



# MARK: CloseTo/Pressure
class Pressure(Constraint): # Checked for graceful failure
    def __init__(self, args):
        self.player1 = args.get('player1', None)
        self.player2 = args.get('player2', None)
        self.radius = 4.0  # 2 meter radius

    def dist(self, scene, ego=False):
        if ego and not isEgo(self.player1):
            return true()
        
        player1 = findObj(self.player1, scene.objects)
        player2 = findObj(self.player2, scene.objects)

        if not (player1 and player2):
            return false()
        
        # Calculate distance using grid-based location like DistanceTo
        x1, y1 = location(player1[0].position)
        x2, y2 = location(player2[0].position)
        distance = np.sqrt((x1 - x2)**2 + (y1 - y2)**2)
        
        # Check if moving towards
        x1_prev, y1_prev = location(player1[0].prevPosition)
        x2_prev, y2_prev = location(player2[0].prevPosition)
        prev_distance = np.sqrt((x1_prev - x2_prev)**2 + (y1_prev - y2_prev)**2)
        moving_towards = prev_distance > distance
        
        # Return true if within radius OR moving towards
        if distance <= self.radius or (moving_towards and distance <= self.radius + 1):
            return true()
        else:
            return false()
    
    def bool(self, scene):
        player1 = findObj(self.player1, scene.objects)
        player2 = findObj(self.player2, scene.objects)

        if not (player1 and player2):
            return False
        
        # Calculate distance using grid-based location like DistanceTo
        x1, y1 = location(player1[0].position)
        x2, y2 = location(player2[0].position)
        distance = np.sqrt((x1 - x2)**2 + (y1 - y2)**2)
        
        # Check if moving towards
        x1_prev, y1_prev = location(player1[0].prevPosition)
        x2_prev, y2_prev = location(player2[0].prevPosition)
        prev_distance = np.sqrt((x1_prev - x2_prev)**2 + (y1_prev - y2_prev)**2)
        moving_towards = prev_distance > distance
        
        print('distance', distance)
        print('radius', self.radius)
        print('p1_pos', player1[0].position)
        print('p2_pos', player2[0].position)
        print('moving_towards', moving_towards)
        
        # Return true if within radius OR moving towards
        return distance <= self.radius or (moving_towards and distance <= self.radius + 1)

    
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
            obj = findObj(self.objID, scene.objects)
            x, y = location(obj[0].position)
        
        mirror = (self.relation == 'below')

        offset = self.threshold_avg
        dev = self.threshold_std

        # print('height relation', self.relation, y, offset, dev)

        distances = (y + offset - i) if mirror else (i - y + offset) 
        height_relation = 1 - np.clip(distances / (dev if dev > 0 else 1), 0, 1) + epsilon

        # Apply player exclusion mask
        player_exclusion_mask = create_player_exclusion_mask(scene)
        height_relation = np.where(player_exclusion_mask, height_relation, epsilon)

        return height_relation
    
    def bool(self, scene):

        obj = findObj(self.objID, scene.objects)

        if not obj:
            return False
        
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
            obj = findObj(self.objID, scene.objects)
            x, y = location(obj[0].position)
        
        mirror = (self.relation == 'right')

        offset = self.threshold_avg
        dev = self.threshold_std

        # print('side relation', self.relation, mirror, x, offset, dev)

        distances = (x + offset - j) if mirror else (j - x + offset) 
        side_relation = 1 - np.clip(distances / (dev if dev > 0 else 1), 0, 1) + epsilon

        # Apply player exclusion mask
        player_exclusion_mask = create_player_exclusion_mask(scene)
        side_relation = np.where(player_exclusion_mask, side_relation, epsilon)

        return side_relation
    
    def bool(self, scene):

        obj = findObj(self.objID, scene.objects)

        if not obj:
            return False
        
        dist = self.dist(scene)
        sample = location(obj[0].position)

        return bool_sample(sample, dist)

class HasPath(Constraint):
    def __init__(self, args):
        self.passerID = args.get('obj1', None)
        self.receiverID = args.get('obj2', None)
        self.radius = args.get('path_width', None)
        self.radiusAvg = self.radius.get('avg', 0.0)
        self.radiusStd = self.radius.get('std', 1.0)
        self.path_width = np.random.normal(loc=self.radius['avg'], scale=self.radius['std'],size=1)
        #print("Path width: ", self.path_width)
 
    def dist(self, scene, ego=False):
        if ego and not (isEgo(self.passerID) or isEgo(self.receiverID)):
            return true()

        passer = findObj(self.passerID, scene.objects)
        receiver = findObj(self.receiverID, scene.objects)

        if not (passer and receiver):
            raise ValueError(f'HasPath constraint requires passer and receiver objects to match the names defined in the program.')

        print(f"DEBUG: Passer: {passer[0].name}, Receiver: {receiver[0].name}")
        
        # Create a 2D grid: 1.0 where safe, epsilon where blocked
        field = np.ones((rows, cols), dtype=float)
        
        # Get teammate position (the one that's not Coach)
        teammate = passer if 'coach' not in passer[0].name.lower() else receiver
        teammate_x, teammate_y = location(teammate[0].position)
        
        # Find all obstacles (red team players)
        obstacles = []
        for obj in scene.objects:
            if hasattr(obj, "team") and obj.team.lower() == 'red':
                obstacles.append(obj)
        
        # For each obstacle, create exclusion zones
        for obstacle in obstacles:
            obstacle_x, obstacle_y = location(obstacle.position)
            # Convert to Unity coordinates for calculations
            obstacle_unity_x = obstacle_x - cols/2
            obstacle_unity_y = -(obstacle_y - rows/2)
            teammate_unity_x = teammate_x - cols/2
            teammate_unity_y = -(teammate_y - rows/2)
            
            # Calculate distance from teammate to obstacle in Unity coordinates
            distance_to_obstacle = np.sqrt((teammate_unity_x - obstacle_unity_x)**2 + (teammate_unity_y - obstacle_unity_y)**2)
            
            # Calculate angle from teammate to obstacle
            angle_to_obstacle = np.arctan2(obstacle_unity_y - teammate_unity_y, obstacle_unity_x - teammate_unity_x)
            
            # Calculate perpendicular angle (90 degrees from obstacle direction)
            perp_angle = angle_to_obstacle + np.pi/2
            
            # Calculate points 1.5m to the sides of the obstacle
            left_point_x = obstacle_unity_x + 1.5 * np.cos(perp_angle)
            left_point_y = obstacle_unity_y + 1.5 * np.sin(perp_angle)
            right_point_x = obstacle_unity_x - 1.5 * np.cos(perp_angle)
            right_point_y = obstacle_unity_y - 1.5 * np.sin(perp_angle)
            
            # Create lines from teammate through the side points
            # Line 1: teammate to left point
            # Line 2: teammate to right point
            
            # For each grid point, check if it's in the excluded area
            for i_idx in range(rows):
                for j_idx in range(cols):
                    # Convert grid coordinates to Unity coordinates
                    grid_unity_x = j_idx - cols/2 + 0.5
                    grid_unity_y = -(i_idx - rows/2 + 0.5)
                    
                    # Distance from obstacle to this grid point in Unity coordinates
                    dist_to_obstacle = np.sqrt((grid_unity_x - obstacle_unity_x)**2 + (grid_unity_y - obstacle_unity_y)**2)
                    
                    # Check if within 1.5m radius of obstacle
                    if dist_to_obstacle <= 1.5:
                        field[i_idx, j_idx] = epsilon
                        continue
                    
                    # Check if point is behind the obstacle (further from teammate than obstacle)
                    dist_to_teammate = np.sqrt((grid_unity_x - teammate_unity_x)**2 + (grid_unity_y - teammate_unity_y)**2)
                    if dist_to_teammate <= distance_to_obstacle:
                        continue  # Point is not behind obstacle
                    
                    # Check if point is in the sector behind the obstacle
                    # Vector from teammate to obstacle
                    vec_to_obstacle = np.array([obstacle_unity_x - teammate_unity_x, obstacle_unity_y - teammate_unity_y])
                    # Vector from teammate to grid point
                    vec_to_point = np.array([grid_unity_x - teammate_unity_x, grid_unity_y - teammate_unity_y])
                    
                    # Calculate angle between these vectors
                    dot_product = np.dot(vec_to_obstacle, vec_to_point)
                    norms_product = np.linalg.norm(vec_to_obstacle) * np.linalg.norm(vec_to_point)
                    
                    if norms_product > 0:
                        cos_angle = np.clip(dot_product / norms_product, -1, 1)
                        angle_diff = np.arccos(cos_angle)
                        
                        # If angle is small (point is roughly in line with obstacle), exclude it
                        if angle_diff < np.pi/6:  # 30 degrees
                            field[i_idx, j_idx] = epsilon
        
        # Visualization
        print("DEBUG: Starting visualization...")
        try:
            plt.figure(figsize=(12, 8))
            
            # Create the main plot
            plt.subplot(1, 2, 1)
            plt.imshow(field, cmap='viridis', origin='upper', extent=[-cols/2, cols/2, -rows/2, rows/2])
            plt.colorbar(label='Probability')
            plt.title('HasPath Dist Output')
            plt.xlabel('X')
            plt.ylabel('Y')
            
            # Add teammate position (convert back to Unity coordinates for display)
            teammate_unity_x = teammate_x - cols/2
            teammate_unity_y = -(teammate_y - rows/2)
            plt.plot(teammate_unity_x, teammate_unity_y, 'go', markersize=10, label='Teammate')
            
            # Add obstacle positions and exclusion zones
            for obstacle in obstacles:
                obs_x, obs_y = location(obstacle.position)
                obs_unity_x = obs_x - cols/2
                obs_unity_y = -(obs_y - rows/2)
                plt.plot(obs_unity_x, obs_unity_y, 'ro', markersize=8, label='Obstacle' if obstacle == obstacles[0] else "")
                
                # Draw 1.5m radius circle around obstacle
                circle = plt.Circle((obs_unity_x, obs_unity_y), 1.5, color='red', fill=False, linestyle='--', alpha=0.7)
                plt.gca().add_patch(circle)
            
            plt.legend()
            plt.grid(True, alpha=0.3)
            
            # Create binary mask plot
            plt.subplot(1, 2, 2)
            binary_field = (field > epsilon).astype(float)
            plt.imshow(binary_field, cmap='RdYlGn', origin='upper', extent=[-cols/2, cols/2, -rows/2, rows/2])
            plt.colorbar(label='Safe (1) / Blocked (0)')
            plt.title('Binary Safe/Blocked Areas')
            plt.xlabel('X')
            plt.ylabel('Y')
            
            # Add teammate and obstacles to binary plot (convert back to Unity coordinates)
            plt.plot(teammate_unity_x, teammate_unity_y, 'go', markersize=10, label='Teammate')
            for obstacle in obstacles:
                obs_x, obs_y = location(obstacle.position)
                obs_unity_x = obs_x - cols/2
                obs_unity_y = -(obs_y - rows/2)
                plt.plot(obs_unity_x, obs_unity_y, 'ro', markersize=8, label='Obstacle' if obstacle == obstacles[0] else "")
            
            plt.legend()
            plt.grid(True, alpha=0.3)
            
            plt.tight_layout()
            plt.show()
            
            print(f"Field shape: {field.shape}, Min: {field.min():.3f}, Max: {field.max():.3f}")
            print(f"Safe areas: {np.sum(field > epsilon)}/{field.size} grid points")
            
        except Exception as e:
            print(f"Visualization failed: {e}")
        
        return field

    def bool(self, scene, ego=False):
        if ego and not (isEgo(self.passerID) or isEgo(self.receiverID)):
            return False

        passer = findObj(self.passerID, scene.objects)
        # print('passer', passer[0].name)
        receiver = findObj(self.receiverID, scene.objects)
        # print('receiver', receiver[0].name)

        obstacles = []
        for obj in scene.objects:
            # TODO: Should check for team

            if hasattr(obj, "team") and obj.team.lower() == 'red':
                obstacles.append(obj)

        # print('obstacles', obstacles[0].name)

        if not (passer and receiver):
            raise ValueError(f'Pass constraint requires passer and receiver objects to match the names defined in the program.')

        xp, yp = location(passer[0].position)
        xr, yr = location(receiver[0].position)
        
        #print("############INFO################")
        #print((xp, yp), (xr, yr), obstacles, self.radiusAvg, self.radiusStd)

        line = LineString([(xp, yp), (xr, yr)])
#         print("Line: ", line)
        
        for obstacle in obstacles:
            # print('obstacle', obstacle.name)
            x_obstacle, y_obstacle = location(obstacle.position)
            p = Point(x_obstacle, y_obstacle)
            dist = p.distance(line)
            # print(dist)
            #print(dist)
#             print("Distance: ", dist)
#             print("Trying to pass to: ", self.receiverID)
            # print('dist<self.path_width', dist < self.path_width)
            # print('yp <= y_obstacle <= yr', yp <= y_obstacle <= yr)
            # print('yp', yp)
            # print('y_obstacle', y_obstacle)
            # print('yr', yr)
            if dist < self.path_width and (yp <= y_obstacle <= yr or yr <= y_obstacle <= yp):
                return False

        return True
    

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
            raise Exception(f'DistanceTo constraint requires a valid reference object (toID).')

        x, y = location(ref[0].position)
        distances = np.sqrt((i - y)**2 + (j - x)**2)

        # print('distance to', x, y, self.minAvg, self.maxAvg, self.operator)

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

        # Apply player exclusion mask
        player_exclusion_mask = create_player_exclusion_mask(scene)
        map = np.where(player_exclusion_mask, map, epsilon)

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
        
        # print('in zone', self.zone)

        if isinstance(self.zone, list):
            zone_str = self.zone[0]
        else:
            zone_str = self.zone

        # print('zone', zone_str, zone_str[0], zone_str[1])
        
        zone_x = self.zone_x_labels.index(zone_str[0])
        zone_y = int(zone_str[1]) - 1
        
        x_coord = j - self.width / 2 + 0.5
        y_coord = i - self.height / 2 + 0.5
        
        zone_idx_x = ((x_coord + self.width/2) // self.zone_width).astype(int)
        zone_idx_y = ((y_coord + self.height/2) // self.zone_height).astype(int)
        
        zone_field = np.where((zone_idx_x == zone_x) & (zone_idx_y == zone_y),
                                1.0, epsilon)
        
        # Apply player exclusion mask
        player_exclusion_mask = create_player_exclusion_mask(scene)
        zone_field = np.where(player_exclusion_mask, zone_field, epsilon)
        
        return zone_field

    def bool(self, scene):

        obj = findObj(self.objID, scene.objects)

        if not obj:
            return False
        
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
            raise ValueError(f'MovingTowards constraint requires obj and ref objects to match the names defined in the program.')

        distance = lambda pos1, pos2: np.sqrt((pos1.x - pos2.x) ** 2 + (pos1.y - pos2.y) ** 2)
        current_distance = distance(obj[0].position, ref[0].position)
        previous_distance = distance(obj[0].prevPosition, ref[0].prevPosition)

        return previous_distance - current_distance <= -0.05
    

# MARK: Pressure
# class Pressure(Constraint):
#     def __init__(self, args={}):
#         self.player1 = args.get('player1', None)
#         self.player2 = args.get('player2', None)
# 
#     def dist(self, scene, ego=False):
# 
#         if ego and not isEgo(self.player1):
#             return true()
#         
#         return true() if self.bool(scene) else false()
# 
#     def bool(self, scene):
# 
#         player1 = findObj(self.player1, scene.objects)
#         player2 = findObj(self.player2, scene.objects)
# 
#         if not (player1 and player2):
#             return False
# 
#         behav = player1[0].gameObject.behavior.lower()
#         name = player2[0].name.lower()
# 
#         if 'follow' in behav and name in behav:
#             return True
#         return False
    

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

        # Check if Coach has ball possession
        coach = findObj('Coach', scene.objects)
        if coach and coach[0].gameObject.ballPossession:
            return True

        return False

# MARK: AtAngle
class AtAngle(Constraint):
    def __init__(self, args):
        # required
        self.playerID = args.get('player', None)
        self.ballID   = args.get('ball',   None)
        # left/right parameter dicts
#         self.direction = args.get('direction', )
#         self.theta = args.get('theta', {'avg':45., 'std':15.})
#         self.dist = args.get('dist', {'avg':3., 'std':1.})
        self.left  = args.get('left',  {'theta':{'avg':45., 'std':15.}, 'dist':{'avg':3., 'std':1.}})
        self.right = args.get('right', {'theta':{'avg':45., 'std':15.}, 'dist':{'avg':3., 'std':1.}})
        

        lt = self.left['theta'];   ld = self.left['dist']
        rt = self.right['theta'];  rd = self.right['dist']

        self.left_theta_avg = lt['avg'];   self.left_theta_std = lt['std']
        self.left_dist_avg  = ld['avg'];   self.left_dist_std  = ld['std']
        self.right_theta_avg = rt['avg'];  self.right_theta_std = rt['std']
        self.right_dist_avg  = rd['avg'];  self.right_dist_std  = rd['std']

    def dist(self, scene, ego=False):
        # only evaluate for ego if it's the coach  
        if ego and not isEgo(self.playerID):
            return true()

        # find the two objects
        player = findObj(self.playerID, scene.objects)
        ball   = findObj(self.ballID,   scene.objects)
        if not (player and ball):
            raise ValueError(f'AtAngle constraint requires player and ball objects to match the names defined in the program.')

        # grid coords for vector math
        P_x, P_y = location(player[0].position)
        B_x, B_y = location(ball[0].position)

        # compute the 2D angle+distance field
        field = self.make_dist((P_x, P_y), (B_x, B_y))
        
        # Apply player exclusion mask
        player_exclusion_mask = create_player_exclusion_mask(scene)
        field = np.where(player_exclusion_mask, field, epsilon)
        
        return field

    def make_dist(self, player_pos, ball_pos):
        """Vectorized angle+distance probability field."""
        P_x, P_y = player_pos
        B_x, B_y = ball_pos

        # vector from player -> ball
        v1_x = B_x - P_x
        v1_y = B_y - P_y
        norm1 = np.hypot(v1_x, v1_y) + epsilon

        # vector from player -> each grid cell
        v2_x = j - P_x
        v2_y = i - P_y
        norm2 = np.hypot(v2_x, v2_y) + epsilon

        # cosine and angle (in degrees)
        cosang = np.clip((v1_x*v2_x + v1_y*v2_y) / (norm1*norm2), -1.0, 1.0)
        angle  = np.arccos(cosang) * 180/np.pi

        # radial distance field from player
        dist   = norm2

        # which side is each cell on?
        cross = v1_x*v2_y - v1_y*v2_x
        left_mask  = cross > 0
        right_mask = ~left_mask

        field = np.zeros((rows, cols))

        # left side prob
        if np.any(left_mask):
            a_p = np.exp(-((angle - self.left_theta_avg)**2) / (2*self.left_theta_std**2))
            d_p = np.exp(-((dist  - self.left_dist_avg )**2) / (2*self.left_dist_std **2))
            field = np.where(left_mask, a_p*d_p, field)

        # right side prob
        if np.any(right_mask):
            a_p = np.exp(-((angle - self.right_theta_avg)**2) / (2*self.right_theta_std**2))
            d_p = np.exp(-((dist  - self.right_dist_avg )**2) / (2*self.right_dist_std **2))
            field = np.where(right_mask, a_p*d_p, field)

        # ensure no zero-probability cells
        return field + epsilon

    def bool(self, scene):
        player = findObj(self.playerID, scene.objects)
        if not player:
            return false()
        dist = self.dist(scene)
        # sample at the player's actual position
        sample = location(player[0].position)
        return bool_sample(sample, dist)
    
# MARK: Overlap
class Overlap(Constraint):
    def __init__(self, args):
        # required
        self.playerID   = args.get('player',   None)
        self.ballID     = args.get('ball',     None)
        self.goalID     = args.get('goal',     None)
        self.opponentID = args.get('opponent', None)
        # optional: learned theta & dist distributions
        th = args.get('theta', {'avg': 35., 'std': 5.})
        ds = args.get('dist',  {'avg': 5.,  'std': 2.})
        self.theta_avg = th['avg'];   self.theta_std = th['std']
        self.dist_avg  = ds['avg'];   self.dist_std  = ds['std']

    def dist(self, scene, ego=False):
        # only enforce for ego
        if ego and not isEgo(self.playerID):
            return true()

        # fetch objects
        ball    = findObj(self.ballID,     scene.objects)
        goal    = findObj(self.goalID,     scene.objects)
        opponent= findObj(self.opponentID, scene.objects)
        if not (ball and goal and opponent):
            raise Exception(f'Overlap constraint requires ball, goal, and opponent objects to match the names defined in the program.')

        Bx, By = location(ball[0].position)
        Gx, Gy = location(goal[0].position)
        Ox, Oy = location(opponent[0].position)

        # vector ball -> goal
        v1x = Gx - Bx;  v1y = Gy - By
        # determine side by opponent
        cross_o = v1x*(Oy - By) - v1y*(Ox - Bx)
        side = 'left' if cross_o > 0 else 'rightH'

        field = self.make_dist((Bx, By), (v1x, v1y), side)
        
        # Apply player exclusion mask
        player_exclusion_mask = create_player_exclusion_mask(scene)
        field = np.where(player_exclusion_mask, field, epsilon)
        
        return field

    def make_dist(self, ball_pos, v1, side):
        """Build a grid of angle/dist probabilities around the ball."""
        Bx, By = ball_pos
        v1x, v1y = v1
        norm1 = np.hypot(v1x, v1y) + epsilon

        # grid vectors from ball -> each cell
        v2x = j - Bx
        v2y = i - By
        norm2 = np.hypot(v2x, v2y) + epsilon

        # angle between v1 and v2 (0–180 deg)
        cosang = np.clip((v1x*v2x + v1y*v2y) / (norm1*norm2), -1.0, 1.0)
        angle  = np.arccos(cosang) * 180.0/np.pi

        # side masks
        cross = v1x*v2y - v1y*v2x
        left_mask  = cross > 0
        right_mask = ~left_mask
        valid_mask = left_mask if side == 'left' else right_mask

        # compute gaussian probs
        a_p = np.exp(-((angle - self.theta_avg)**2) / (2*self.theta_std**2))
        d_p = np.exp(-((norm2   - self.dist_avg )**2) / (2*self.dist_std **2))
        field = np.where(valid_mask, a_p * d_p, 0.0)

        # no zeros
        return field + epsilon

    def bool(self, scene):
        player = findObj(self.playerID, scene.objects)
        if not player:
            return false()
        dist = self.dist(scene)
        # sample at the player's actual location
        sample = location(player[0].position)
        return bool_sample(sample, dist)

# MARK: IsMoving
class IsMoving(Constraint):
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
        
        return player[0].gameObject.isMoving