"""We need to define things here like positions, players, etc. 
This is how scenic actually grabs information.
"""
from scenic.simulators.unity.simulator import UnitySimulator
# from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.client import gameObject
from scenic.core.vectors import Orientation, Vector
param unity_map = None
# param address = '10.40.115.55'
param address = 'localhost'
param port = 5555
param timeout = 10
param timestep = .1
CATCH_RADIUS = 1.2
#TEAMMATE_SPEED = 2.5
#PLAYER_Y_OFFSET = 1.3
#goal_position = OrientedPoint at (25.24,-0.118,0.46)

simulator UnitySimulator(
    ip=globalParameters.address,
    port=int(globalParameters.port),
    timeout=int(globalParameters.timeout),
    render=True,
    timestep=float(globalParameters.timestep)
)

class UnityObject:
    position : (0,0,0)
    prevPosition: (0,0,0)
    isUnityObject : True
    gameObjectType : ""
    yaw : 0 deg
    pitch : 0 deg
    roll : 0 deg
    gameObject : gameObject((0,0,0), Orientation.fromEuler(0,0,0))
    width : 0.0
    length : 0.0
    height : 0.0

class Angle:
    angle : 0

class Ball(UnityObject):
    position : (0,0,1)
    gameObject : gameObject((0,0,1), Orientation.fromEuler(0,0,0))
    gameObjectType : "ball"
    width : 0.15
    length : 0.15
    height : 0.15
    name: "Ball"

class Goal(UnityObject):
    gameObjectType: "goal"
    name: "goal"
    width: 1
    length : 1
    height : 1

class Line(UnityObject):
    gameObjectType: "line"
    name: "line"
    width: 20
    length : 0.25
    height : 0.25

class Player(UnityObject):
    gameObjectType : "player"
    width : 1.0
    length : 1.0
    height : 1.0
    team : "red"
    path : None
    ballPossession : False
    tacticalPosition : Vector(0, 0, 0)
    name : "Player"
    debug: True

class OffensePlayer(Player):
    team : "red"
    path : None
    ballPossession : False
    tacticalPosition : Vector(0, 0, 0)
    name : "Unknown"
    debug: True

class DefensePlayer(Player):
    team : "blue"
    path : None
    ballPossession : False
    tacticalPosition : Vector(0, 0, 0)
    name : "Unknown"
    debug: True
    
class GoalKeeper(Player):
    gameObjectType : "goalkeeper"

#goalpost

# human controlled
class Human(UnityObject):
    gameObjectType : "human"
    width : 0.1
    length : 0.1
    height : 0.1
    team : "blue"
    path : None
    name : "Coach"
    systemControlled : False
    xMark : Vector(0, 0, 0)  # Position of the X mark on the field
    triggerPass : False  # Indicates whether the coach has told the teammate to pass

# system controlled
class Coach(Human):
    systemControlled : True
    name: "Coach"

class AIAgent(UnityObject):
    gameObjectType : "aiAgent"
    width : 1.0
    length : 1.0
    height : 1.0

# --------------------
# MARK: Robot Scenario Models
# --------------------
class Robot(UnityObject):
    gameObjectType : "robot"
    width : 1.0
    length : 1.0
    height : 1.0
    name : "Robot"

class NPC(UnityObject):
    gameObjectType : "npc"
    width : 1.0
    length : 1.0
    height : 1.0
    name : "NPC"

# MARK: movingTowards
def isMovingTowards(refObject: UnityObject, targetObject: UnityObject) -> bool:
    """
    Determines if the reference object (refObj) is moving towards the target object.

    Args:
    refObject (Object): The reference object whose movement is being evaluated.
    targetObject (Object): The target object towards which the reference object's 
                    movement is being evaluated.

    Returns:
    bool: True if the reference object is moving towards the target object,
        False otherwise. The determination is based on the direction of
        the reference object's velocity relative to the position of the 
        target object.
    """

    # Movement direction
    moveDir = refObject.position - refObject.prevPosition

    # Normalize the direction vector
    length = distance from Vector(0, 0, 0) to moveDir
    if length == 0:
        return False

    unit_moveDir = Vector(moveDir.x / length, moveDir.y / length, moveDir.z / length)
    # print(f"unit_moveDir: {unit_moveDir}")

    # Epected direction
    direction = targetObject.position - refObject.position

    length = distance from Vector(0, 0, 0) to direction
    if length == 0:
        return False
    unit_direction = Vector(direction.x / length, direction.y / length, direction.z / length)
    # print(f"unit_direction: {unit_direction}")

    dot_product = unit_moveDir.x * unit_direction.x + unit_moveDir.y * unit_direction.y + unit_moveDir.z * unit_direction.z
    # print(f"dot_product: {dot_product}")
    return dot_product > 0.866    

# Mark: movesBeyond
def movesBeyond(referenceObj, target):
    """
    Checks whether the reference object moves beyond, or drops below, the target object or position. 

    Args:
    referenceObj (Object): The reference object 
    target (Object | Vector): The target object or position

    Returns:
    bool: True if the reference object is moves beyond, or drops below, the target object or position. 
          Otherwise, returns False. 
    """
    return abs(referenceObj.position.y - target.position.y) < 0.1

# Mark: isMovingUpTheField()
def isMovingUpTheField(referenceObj):
    """
    Checks whether the reference object is moving up the field pitch. 

    Args:
    referenceObj (Object): The reference object 

    Returns:
    bool: True if the reference object is moving up the field pitch. 
          Otherwise, returns False. 
    """
    return (referenceObj.position.y - referenceObj.prevPosition.y) > 0.01

# Mark: isMovingDownTheField()
def isMovingDownTheField(referenceObj):
    """
    Checks whether the reference object is moving down the field pitch. 

    Args:
    referenceObj (Object): The reference object 

    Returns:
    bool: True if the reference object is moving down the field pitch. 
          Otherwise, returns False. 
    """
    return (referenceObj.position.y - referenceObj.prevPosition.y) < -0.01

# Mark: movesDownTheMidLine
def movesDownTheMidLine(referenceObj):
    """
    Checks whether the reference object moves below the middle line of the field. 

    Args:
    referenceObj (Object): The reference object 

    Returns:
    bool: True if the reference object moves below the middle line of the field
          Otherwise, returns False. 
    """
    return referenceObj.position.y < 0

# Mark: movesAboveTheMidLine
def movesAboveTheMidLine(referenceObj):
    """
    Checks whether the reference object moves above the middle line of the field. 

    Args:
    referenceObj (Object): The reference object 

    Returns:
    bool: True if the reference object moves above the middle line of the field
          Otherwise, returns False. 
    """
    return referenceObj.position.y > 0

# Mark: isCloseTo
def isCloseTo(referenceObj, target, distance = 4):
    """
    The reference object is close to the target object or position

    Args:
    referenceObj (Object): The reference object 
    target (Object | Vector): The target object or position
    distance (float): The threshold to distance to determine closenss. By default, it is set to 4 meters. 

    Returns:
    bool: True if the reference object is close to the target. 
          Otherwise, returns False. 
    """
    return (distance from referenceObj to target) < distance

def positionInLine(start: Vector, end: Vector, distance: float) -> Vector:
    """
    Calculate a position along a line defined by two points in 3D space at a
    specified distance from the start point.

    Parameters:
    start (Vector): A vector representing the starting point of the line of
                    direction (x, y, z).
    end (Vector): A vector representing the ending point the line of
                  direction (x, y, z).
    distance (float): The distance from the start point to the desired
                      position along the specifed line of direction.

    Returns:
    Vector: A vector representing the position (x, y, z) along the line at
            the specified distance from the start point in the direction
            determined by the end point.
    """

    direction = end - start

    # Normalize the direction vector
    length = distance from Vector(0, 0, 0) to direction
    unit_direction = direction / length

    return start + unit_direction * distance


# MARK: closest
def closest(objType: type, ref: Object, max: float = float('inf')) -> Object:
    """
    Finds the closest object of a specified type to the reference object within a simulation.

    Args:
    type (type): The type of objects to search for.
    ref (Object): The reference object from which distances are measured.
    max (float, optional): The maximum distance threshold. Defaults to infinity.
                           Objects further than this distance are ignored.

    Returns:
    Object: The closest object of the specified type that is closer than the
            given minimum distance. If no such object is found, returns None.
    """

    objects = simulation().objects
    closer = None

    for obj in objects:

        if not isinstance(obj, objType):
            continue

        d = distance from Vector(0,0,0) to obj

        if 0 < d < max:
            max = d
            closer = obj

    return closer



# MARK: hasBallPosession
def hasBallPosession(player: Player) -> bool:
    """
    Checks if the player has the ball posession or not. 
    Note that the function will always only return true for a single object in the scene at a time.

    Args:
    player (Player): The player to check the ball posession for.

    Returns:
    bool: True if the player has the ball posession, False otherwise.
    """
    return player.gameObject.ballPossession


# MARK: isFacingAt
def isFacingAt(player: Player, target: UnityObject) -> bool:
    """
    Checks if the current instance of the player is facing towards the target object.
    Args:
    target (Object): The target object's position to compare with the player.
    Returns:
    bool: True if the player is looking at the target, False otherwise.
    """
    seeing = player can see target
    return seeing


# MARK: footPreference
def footed(player: Player):
    """
    The foot preference of the specified player.

    Args:
        player (Player): The player.

    Returns:
        Direction: 'right' if the player is right footed, 'left' otherwise.
    """
    return None



# MARK: distanceBetween
def distanceBetween(reference: Object, target: Object) -> float:
    '''Returns the distance between two specified objects in the scene.

    Args:
        reference (Object): The reference object.
        target (Object): The target object.
    
    Returns:
        float: The eucledian distance between the reference and target object in a 2D plane.
    '''
    distance = distance from reference to target
    return distance


# MARK: spaceFeet
def spaceFeet(player: Player, distance: float):
    """
    The player will space their feet apart by a specified distance.

    Args:
        player (Player): The player.
        distance (float): The distance to space the player's feet apart.
    """
    return None    


# MARK: crouch
def crouch(player: Player, angle):
    """
    The player will crouch by bending their knees by a specified angle.
    
    Args:
        player (Player): The player crouching.
        angle (Angle): The amount by which the player should bend their knees to crocuh.
    """
    return None




# MARK: orientWaist
def orientWaist(player: Player, angle: int) -> None:
    ''' Twists the player's waist in a specified direction by a specified angle.
    Args:
    player (Player): The player whose waist is being twisted.
    angle (int): The angle by which to twist the player's waist.
    Returns:
    None: The function modifies the player's posture but does not return any value.
    '''
    return None


# # MARK: isWithinFieldBounds
# def withinBounds(player: Player) -> bool:
#     '''
#     Checks if the object is within the specified bounds of the field.

#     Args:
#     object (Object): The object whose position is being checked.

#     Returns:
#     bool: True if the object is within the specified bounds, False otherwise.
#     '''

def inBetween(object1, object2) -> Vector:
    ''' 
    compute a center position in between two objects. 
    Args:
    object1 (Player)
    object1: (Player)
    
    Returns: Vector
    '''

    pos1 = object1 if isinstance(object1, Vector) else object1.position
    pos2 = object2 if isinstance(object2, Vector) else object2.position
    pos = (pos1+pos2) / 2
    return pos

    