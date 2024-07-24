from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

timestep = 0.1

footed = DiscreteRange(-1, 1)

pressingDistance = 3.5 #Uniform(4, 5)
shootingDistance = Uniform (4, 8)

behavior opponentBehavior():
    try:
        do InterceptBall(football)
    interrupt when hasBallPosession(self):
        do SetPlayerSpeed(5.0)
        do MoveTo(goal.position) for 0.1 seconds
        opponent.prevPosition = opponent.position
    interrupt when self.gameObject.ballPossession and distance from self to ego < pressingDistance:
        do SetPlayerSpeed(10.0)
        if abs(opponent.position.x - ego.position.x) < 1:
            do MoveTo(ego.position + Vector(1.5 * footed, 1.5, 0)) for 0.1 seconds
            opponent.prevPosition = opponent.position
        else:
            do MoveTo(ego.position + Vector(2 * footed, -1, 0)) for 0.1 seconds
            opponent.prevPosition = opponent.position
    interrupt when self.gameObject.ballPossession and distance from self to ego < (pressingDistance + 2) and distance from self to ego > (pressingDistance):
        do SetPlayerSpeed(1.5)
        do MoveTo(self.position + Vector((self.position.x - ego.position.x) * 5, 0, 0)) for 0.1 seconds
        opponent.prevPosition = opponent.position
    interrupt when distance from self to goal < distance from ego to goal: # ahead of defendant
            try:
                do SetPlayerSpeed(5.0)
                do MoveTo(goal.position + Vector(0, 4, 0)) for 0.1 seconds
                opponent.prevPosition = opponent.position
            interrupt when distance from self to goal < shootingDistance:
                do ShootBall(goal.position, "center-middle")
                do Idle()
                opponent.prevPosition = opponent.position

behavior expert():
    try:
        do Idle() for 1 seconds
        do Pause() 
        do Speak("You should do this when precondition") 
        do Idle() for 5 seconds
        do Unpause() for 5 seconds
    interrupt when hasBallPosession(self):
        do InterceptBall(football)
    interrupt when condition1():
        do action1()
    interrupt when condition2():
        do action2()

def condition1() -> bool:
    if hasBallPosession(opponent) and movingTowards(opponent, goal):
        print("condition1")
    return hasBallPosession(opponent) and movingTowards(opponent, goal)

behavior action1():
    attacker = closest(Player, self)
    point = positionInLine(attacker.position, goal.position, 3.0)
    do MoveTo(point) for timestep seconds

def condition2():
    if hasBallPosession(opponent):
        if not movingTowards(opponent, goal):
            if movingTowards(opponent, right_side) or movingTowards(opponent, left_side):
                print("condition2")
                return True
    return False

behavior action2():
    attacker = closest(Player, self)
    if attacker:
        new_position = positionInLine(attacker.position, goal.position, 3.0)
        do MoveTo(new_position) for 0.05 seconds
        do InterceptBall(opponent) for 0.05 seconds

ego = new Human at (0, 0, 0), with behavior expert()

opponent = new Player ahead of ego by Uniform(8, 10),
                facing directly toward ego,
                with behavior opponentBehavior()

right_side = new Player at (20, 0, 0)
left_side = new Player at (-20, 0, 0)

football = new Ball ahead of ego by 0.5
goal = new Goal behind ego by 8, facing away from ego
        
terminate when (ego.gameObject.stopButton)

# API

def hasBallPosession(player: Player) -> bool:
    """
    Checks if the player has the ball posession or not.
    
    Args:
    player (Player): The player to check the ball posession for.

    Returns:
    bool: True if the player has the ball posession, False otherwise.
    """

    return player.gameObject.ballPossession

def movingTowards(refObj: UnityObject, target: UnityObject, velocity: float = 0.0) -> bool:
    """
    Determines if the reference object (refObj) is moving towards the 
    target object.

    Args:
    refObj (Object): The reference object whose movement is being evaluated.
    target (Object): The target object towards which the reference object's 
                     movement is being evaluated.

    Returns:
    bool: True if the reference object is moving towards the target object,
          False otherwise. The determination is based on the direction of
          the reference object's velocity relative to the position of the 
          target object.
    """

    # Movement direction

    moveDir = refObj.position - refObj.prevPosition

    # Normalize the direction vector
    length = distance from Vector(0, 0, 0) to moveDir
    unit_moveDir = moveDir / length

    # Epected direction

    direction = target.position - refObj.position

    length = distance from Vector(0, 0, 0) to direction
    unit_direction = direction / length

    dot_product = unit_moveDir.x * unit_direction.x + unit_moveDir.y * unit_direction.y + unit_moveDir.z * unit_direction.z

    return dot_product > 0.75

    #prevDistance = distance from refObj.prevPosition to target
    #distance = distance from refObj.position to target

    #return (prevDistance - distance) > velocity

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

def closest(type: type, ref: Object, min: float = float('inf')) -> Object:
    """
    Finds the closest object of a specified type to the reference object
    within a simulation.

    Args:
    type (type): The type of objects to search for.
    ref (Object): The reference object from which distances are measured.
    min (float, optional): The minimum distance threshold. Defaults to infinity.
                           Objects further than this distance are ignored.

    Returns:
    Object: The closest object of the specified type that is closer than the
            given minimum distance. If no such object is found, returns None.
    """

    objects = simulation().objects
    closer = None

    for obj in objects:

        if not isinstance(obj, type):
            continue

        d = distance from ref to obj

        if 0 < d < min:
            min = d
            closer = obj

    return closer