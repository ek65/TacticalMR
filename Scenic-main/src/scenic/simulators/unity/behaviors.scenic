from scenic.simulators.unity.actions import *
from enum import Enum
model scenic.simulators.unity.model

# Language: scenic (python)
# This file defines all shared scenic behaviors. In order to use any behavior defined
# here, add "from scenic.simulators.vr.behaviors import *" to the top of the scenic file

timestep = 0.1 # sec per timestep

behavior IdleSpecial():
    while True:
        prev = ego.prevPosition
        pos = ego.position
        print(f"ego.prevPosition = {(prev.x, prev.y, prev.z)}")
        print(f"ego.currPosition = {(pos.x, pos.y, pos.z)}")
        take IdleAction()

behavior Idle():
    while True:
        take IdleAction()

behavior ShootBall(vec : Vector, string : str):
    take ShootAction(vec, string, "Shoot Ball")
    take StopAction()

behavior InterceptBall(ball):
    while (distance from self to ball) > 0.5:
        # print(distance from self to ball)
        take MoveToAction(ball.position, "Intercept Ball")
    take StopAction()

behavior PassTo(target):
    # if the target is moving, then pass in front of the target
    # target_pass_pos = target
    # if isinstance(target, UnityObject):
    #     target_pass_pos = target.position
        # currPos  = target.position
        # prevPos  = target.prevPosition
        # move_dir = currPos - prevPos

        # # compute predicted position of the target 1 second into the future
        # target_pass_pos = new Point at (currPos.x + move_dir.x * 1/timestep, currPos.y + move_dir.y * 1/timestep, currPos.z + move_dir.z * 1/timestep)
        # print(f"target name: {target.name}")
        # print(f"currPos: {currPos}")
        # print(f"move_dir: {move_dir}")
        # print(f"target_pass_pos: {(target_pass_pos.x, target_pass_pos.y, target_pass_pos.z)}")
    scene = simulation()

    if isinstance(target, UnityObject):
        target = target.position
    elif checkIfString(target):
        target = [obj for obj in scene.objects if obj.name.lower() == target][0].position # converts string into object reference

    print(f"Passing to {target}")

    take GroundPassFastAction(target, "Pass Ball")
    do Idle() for 1 seconds
    take StopAction()

behavior LookAt(vec):
    location = vec
    if isinstance(vec, UnityObject):
        location = vec.position
    take LookAtAction(location, "Look At")
    take StopAction()

behavior MoveTo(v, lookAtTarget = None, distance = 0.5, status=""):
    dist = 1000
    while not (dist < distance):
        if isinstance(v, Ball):
            take MoveToAction(v.position, status)
            # take LookAtAction(v)
        elif isinstance(v, Vector):
            take MoveToAction(v, status)
            # take LookAtAction(v)
        else:
            # take MoveToAction(v.position, status)
            take MoveToAction(v.position, status)
            # take LookAtAction(v)
        dist = distance from self to v
    
    if lookAtTarget is None:
        lookAtTarget = Vector(0, -13, 0)
    else:
        lookAtTarget = lookAtTarget.position
    do LookAt(lookAtTarget)
    

behavior ApproachGoal(v):
    dist = 1000
    while not (dist < 0.5):
        take MoveToAction(v, "Approach Goal")
        dist = distance from self to v

behavior DribbleTo(v):
    dist = 1000
    while not (dist < 0.5):
        take DribbleToAction(v)
        dist = distance from self to v

behavior SetPlayerSpeed(s):
    take SetPlayerSpeedAction(s)

behavior Print(o):
    take PrintAction(o)

behavior Speak(input : str):
    take SpeakAction(input)
    take StopAction()

behavior Explain(input : str):
    take SpeakAction(input)
    take StopAction()

behavior Pause():
    take PauseAction()
    take StopAction()

behavior Unpause():
    take UnpauseAction()
    take StopAction()

behavior segmentStart():
    take SegmentStartAction()

behavior segmentEnd():
    take SegmentEndAction()

behavior WaitFor(timesteps):
    for i in range(timesteps):
        take StopAction()


# MARK: moveTo
behavior moveTo(player: Player, target: Coordinate, ref: list, speed: Speed):
    """
    A player will move to the specified target with a specified velocity and style.

    Args:
        targetPosition (Vector): A position of the end point of the trajectory.
        style (MovingStyle): A moving style out of the options 'walk', 'run' and 'sprint'.
        velocity (float): The velocity to move to the target.
    """
    target_position = target.predict(ref)
    target_speed = speed.predict()

    while (distance from self to target_position > 0.01):
        target_position = target.predict(ref)
        take MoveToWithSpeed(target_position, target_speed), LookAtAction(ball)

behavior getTo(destination):
    while (distance from self to destination > 0.01):
        # take MoveToAction(destination)
        take MoveToLookAtBallWithSpeed(destination, 2.0)


behavior moveToLookAtBall(player: Player, target: Coordinate, ref: list, speed: Speed):
    """
    A player will move to the specified target with a specified velocity and style.
    Args:
        targetPosition (Vector): A position of the end point of the trajectory.
        style (MovingStyle): A moving style out of the options ‘walk’, ‘run’ and ‘sprint’.
        velocity (float): The velocity to move to the target.
    """
    target_position = target.predict(ref)
    target_speed = speed.predict()
    while (distance from self to target_position > 0.05):
        target_position = target.predict(ref)
        take MoveToLookAtBallWithSpeed(target_position, target_speed)

# MARK: faceTowards
# TODO: Need to Create an Action to called "FaceTowards"
# def faceTowards(player: Player, target: Object, offset: Angle = 0):
#     """
#     The player will face towards the specified taget object by an arbitrary offset angle in a 2D plane.

#     Args:
#         player (Player): The player facing the target object.
#         target (Object): The object the specified player should face towards.
#         offset (Angle): The offset angle at which the player should be facing the target object, positive values for right and negative values for left.
#     """
    

# MARK: mirrorActions
behavior mirrorActions(player: Player, reference: Player):
    """
    The player will mirror the actions of the reference player.

    Args:
        player (Player): The player mirroring the actions.
        reference (Player): The player whose actions should be mirrored.
    """
    return None

# MARK: spaceShoulders
behavior spaceShoulders(player: Player, distance: float):
    """
    The player will space their shoulders apart by a specified distance.

    Args:
        player (Player): The player.
        distance (float): The distance to space the player's shoulder apart from the body.
    """
    return None

# MARK: orientArms
behavior orientArms(player: Player, angle: int, target: Object = None):
    """ Orients the player's arms to a specified angle.
    Args:
    player (Player): The player whose arms are being oriented.
    angle (int): The angle to which the player's
        arms should be raised.
    target (Object, optional): The target to point the arms towards
    Returns:
    None: The function modifies the player's posture but does not return any value.
    """
    return None

# MARK: lean

behavior lean(player: Player, angle: Angle):
    '''
    The specified player will lean forward by an arbitrary angle.

    Args:
        player (Player): The player leaning at an angle.
        angle (Angle): The amount by which the player should lean. Postive values for fowards, negative for backwards.
    '''
    return None


# MARK: Tackle
behavior tackle(player: Player, target: Player):
    '''
    The specified player will attempt to tackle the target player.
    
    Args:
        player (Player): The player performing the tackle.
        target (Player): The player being tackled.
    '''
    take TackleBallAction(target)

behavior idle(player: Player):
    '''
    The specified player will stay in their own place and not move.

    Args:
        player (Player): The player who will stay idle.

    '''
    take IdleAction()

# --------------------
# MARK: Robot Scenario Behaviors
# --------------------
behavior MoveToRobot(v, lookAtTarget = None, distance = 0.5, status=""):
    dist = 1000
    while not (dist < distance):
        if isinstance(v, Vector):
            take MoveToRobotAction(v, status)
        else:
            take MoveToRobotAction(v.position, status)
        dist = distance from self to v