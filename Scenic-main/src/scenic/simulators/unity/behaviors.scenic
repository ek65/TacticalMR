from scenic.simulators.unity.actions import *
from scenic.simulators.unity.constraints import *
from enum import Enum
model scenic.simulators.unity.model
from scenic.core.vectors import Orientation, Vector
from scenic.core.object_types import Point
import random

# Language: scenic (python)
# This file defines all shared scenic behaviors. In order to use any behavior defined
# here, add "from scenic.simulators.vr.behaviors import *" to the top of the scenic file

timestep = 0.1 # sec per timestep

behavior Idle():
    while True:
        take IdleAction()
        take StopAction()
        # print(f"ego.x: {ego.position.x}, ego.y: {ego.position.y}, ego.z: {ego.position.z}")

behavior ShootBall(vec : Vector, string : str):
    take ShootAction(vec, string, "Shoot Ball")
    take StopAction()

behavior InterceptBall():
    take InterceptBallAction()
    take StopAction()

behavior Pass(target, slow=False):
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

    if isinstance(target, UnityObject) or isinstance(target,Point):
        target = target.position
        # print("UnityObj")
    elif checkIfString(target):
        #print([obj.name for obj in scene.objects])
        target = [obj for obj in scene.objects if obj.name.lower() == target.lower()][0].position # converts string into object reference
        # print("elif case")
    print(f"Passing to {target}")
    print(f"type: {type(target)}")

    if slow:
        take GroundPassSlowAction(target, "Pass Ball")
    else:
        take GroundPassFastAction(target, "Pass Ball")
    do Idle() for 1 seconds
    take StopAction()

behavior LookAt(vec):
    location = vec
    if isinstance(vec, UnityObject):
        location = vec.position
    take LookAtAction(location, "Look At")
    take StopAction()

behavior Shoot(goal):
    scene = simulation()
    if checkIfString(goal):
        goal = [obj for obj in scene.objects if obj.name.lower() == goal.lower()][0] # converts string into object reference
    take  GroundPassFastAction(goal.position, "Shoot Ball")

behavior MoveToBehavior(v, lookAtTarget = None, distance = 0.2, status=""):
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
    
    take StopAction()
    
    if lookAtTarget is not None:
        do LookAt(lookAtTarget)
    else:
        do LookAt(v)

behavior WalkTo(v, lookAtTarget = None, distance = 0.2, status=""):
    do SetPlayerSpeed(2)
    do MoveToBehavior(v, lookAtTarget, distance, status)

behavior JogTo(v, lookAtTarget = None, distance = 0.2, status=""):
    do SetPlayerSpeed(3.5)
    do MoveToBehavior(v, lookAtTarget, distance, status)

behavior SprintTo(v, lookAtTarget = None, distance = 0.2, status=""):
    do SetPlayerSpeed(5)
    do MoveToBehavior(v, lookAtTarget, distance, status)

behavior ApproachGoal(v):
    dist = 1000
    while not (dist < 0.5):
        take MoveToAction(v, "Approach Goal")
        dist = distance from self to v

behavior DribbleTo(v):
    if self.gameObject.ballPossession:
        do MoveToBehavior(v)
    # dist = 1000
    # while not (dist < 0.5):
    #     take DribbleToAction(v)
    #     dist = distance from self to v

# Max player speed is 5
behavior SetPlayerSpeed(s):
    if (s > 5.0):
        s = 5.0
    take SetPlayerSpeedAction(s)

behavior Print(o):
    take PrintAction(o)

behavior Speak(input : str):
    do Idle() for 1 seconds
    take SpeakAction(input)
    do Idle() for 1 seconds
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

def sample_target(scene, prev_target, λ_dest) -> Vector: 
    # global sample
    i = 0
    target = [prev_target.x, prev_target.y]
    
    while not λ_dest(scene, target):
        x = Range(-FIELD_WIDTH / 2, FIELD_WIDTH / 2)
        y = Range(0, FIELD_HEIGHT / 2)
        target = [x,y]
        if i > 100000:
            raise Exception("Maximum sample depth exceeded.")
        i += 1

    sample = Vector(target[0], target[1])
    # print(f"sample: {sample}")
    return sample

# # MARK: MoveTo
# behavior MoveTo(λ_dest):
#     scene = simulation()
#     sample = Vector(0, 0)
#     sample = sample_target(scene, sample, λ_dest)
#     timestep = 0.3
#     # print(f"sample: {sample}")
#     while (distance from self to sample > 0.5):
#         # print('moving to', sample)
#         do MoveToBehavior(sample) for timestep seconds
#         scene = simulation()
#         sample = sample_target(scene, sample, λ_dest)
#         # print(f"sample: {sample}")
#     do Idle() for 1 seconds

# # MARK: moveTo
# behavior moveTo(player: Player, target: Coordinate, ref: list, speed: Speed):
#     """
#     A player will move to the specified target with a specified velocity and style.

#     Args:
#         targetPosition (Vector): A position of the end point of the trajectory.
#         style (MovingStyle): A moving style out of the options 'walk', 'run' and 'sprint'.
#         velocity (float): The velocity to move to the target.
#     """
#     target_position = target.predict(ref)
#     target_speed = speed.predict()

#     while (distance from self to target_position > 0.01):
#         target_position = target.predict(ref)
#         take MoveToWithSpeed(target_position, target_speed), LookAtAction(ball)

behavior getTo(destination):
    while (distance from self to destination > 0.01):
        # take MoveToAction(destination)
        take MoveToLookAtBallWithSpeed(destination, 2.0)

behavior MoveToBallAndGetPossession():
    while not self.gameObject.ballPossession:
        do MoveTo("ball")
    
    do Idle() for 1.0 seconds


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
    target (moect, optional): The target to point the arms towards
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

# behavior MoveTo(λ_dest):
#     scene = simulation()
#     sample = Vector(0, 0)
#     sample = sample_target(scene, sample, λ_dest)
#     timestep = 0.3
#     # print(f"sample: {sample}")
#     while (distance from self to sample > 0.5):
#         # print('moving to', sample)
#         do MoveToBehavior(sample) for timestep seconds
#         scene = simulation()
#         sample = sample_target(scene, sample, λ_dest)
#         # print(f"sample: {sample}")
#     do Idle() for 1 seconds

behavior PickUp():
    take PickUpAction()
    do Idle() for 2 seconds
    take StopAction()

behavior PutDown():
    take PutDownAction()
    do Idle() for 2 seconds
    take StopAction()

behavior Packaging():
    take PackagingAction()
    do Idle() for 4 seconds
    take StopAction()

behavior RaiseHand():
    take RaiseHandAction()
    do Idle() for 1 seconds
    take StopAction()

# -------------------------------

import numpy as np
import matplotlib.pyplot as plt

def location(vec):
    return vec.x + cols / 2, -vec.y + rows / 2

def bool_sample(vec, dist, min=0.1):

    max_val = dist.max()
    if max_val > 0:
        dist = dist / max_val

    x = builtins.min(builtins.max(int(vec[0]), 0), cols - 1)
    y = builtins.min(builtins.max(int(vec[1]), 0), rows - 1)

    sample = (y, x)
    value = dist[sample]

    return value > min

rows, cols = 34, 20
i, j = np.indices((rows, cols))

def get_top_10_indices(array_2d):
    """
    Get the indices of the top 10 elements in a 2D numpy array that are greater than 0
    
    Parameters:
    array_2d (numpy.ndarray): A 2D numpy array
    
    Returns:
    tuple: A tuple containing (row_indices, col_indices) of the top 10 elements
    """
    # Get the indices of all elements greater than 0
    valid_indices = np.argwhere(array_2d > 0)

    # If there are no valid indices, return empty arrays
    if valid_indices.size == 0:
        return np.array([]), np.array([])

    # Get the values of the valid indices
    valid_values = array_2d[valid_indices[:, 0], valid_indices[:, 1]]

    # Get the indices of the top 10 elements
    top_10_indices = np.argsort(valid_values)[-10:]

    # Get the row and column indices of the top 10 elements
    row_indices = valid_indices[top_10_indices, 0]
    col_indices = valid_indices[top_10_indices, 1]

    return row_indices, col_indices

def sample_from(dist, _min=0.4):
    # row_indices, col_indices = get_top_10_indices(dist)
    # row_size = len(row_indices)
    # col_size = len(col_indices)

    # print(f"Top 10 indices - Rows: {len(row_indices)}, Cols: {len(col_indices)}")
    
    # coord = None
    # if row_size > 0 and col_size > 0:
    #     print("Selecting Destination position from the top 10 indices")
    #     row = random.randint(0, row_size - 1)
    #     # print("row:", row)
    #     col = random.randint(0, col_size - 1)
    #     # print("col:", col)
    #     coord = (row, col)
    #     # print("finished selecting")
    # else:
    #     print("No valid indices found. Randomly selecting from the entire distribution.")
    #     row_num, column_num = dist.shape
    #     row = random.randint(0, row_num - 1)
    #     col = random.randint(0, column_num - 1)
    #     coord = (row, col)

    # Check if dist is a DistanceTo constraint object with a sample method
    if hasattr(dist, 'sample') and callable(getattr(dist, 'sample')):
        print('AVG VECTOR', dist)
        sample = dist #.sample()
        print(f"SAMPLED directly from constraint: {sample}")
        return sample

    # Otherwise, use the grid-based sampling
    # print(dist)

    max_val = dist.max()
    if max_val > 0:
        dist = dist / max_val

    filtered = np.where(dist >= _min, dist, 0.0)
    
    total = filtered.sum()
    if total == 0:
        print("Warning: No valid samples found. Randomly sampling from the field.")
        flat = np.ones(dist.size, dtype=np.float64) / dist.size
    else:
        probs = filtered / total
        flat = probs.ravel()
    
    probs = filtered / total
    flat = probs.ravel()
    
    idx = np.random.choice(flat.size, p=flat)
    coord = np.unravel_index(idx, dist.shape)
    
    #print(f"coord: {coord}, dist.shape: {dist.shape}, idx: {idx}")
    x, y = int(coord[1]), int(coord[0])
    #print('real sampled', x, y)
    sample = Vector(x - cols / 2, rows / 2 - y)
    print("Sample: ", sample)
    #print('Sampled', sample)

    return sample
    


#behavior MoveTo(dist):
#    sample = sample_from(dist)
#    dt = 0.2
#    while (distance from self to sample > 0.5):
#        print('moving to', sample)
#        do MoveToBehavior(sample) for dt seconds
#       if not bool_sample(location(sample), dist):
#            sample = sample_from(dist)
#    do Idle() for 1 seconds

# Soccer MoveTo
# behavior MoveTo(param, doPass: bool = False):
#     # --- try the “param is a distribution” path ---
#     try:
#         sample  = sample_from(param)
#         print(f"MoveTo: sample is {sample}")
#         dynamic = False  # Changed to False to prevent re-sampling
#         dist    = param
#     # if sample_from isn’t defined for this type, assume it’s already a goal
#     except Exception:
#         scene = simulation()

#         if checkIfString(param):
#             param = [obj for obj in scene.objects if obj.name.lower() == param.lower()][0].position # converts string into object reference
#             sample  = param
#             dynamic = False
#         else:
#             sample  = param
#             dynamic = False

#     print("MoveTo: sample is ", sample)
#     # only set these values if coach called moveTo
#     if (self == ego and self.gameObjectType == "Coach"):
#         # set xMark to the sample position
#         ego.xMark = sample
#         #print(f"MoveTo: xMark set to {ego.xMark}")
#         # set triggerPass to doPass
#         ego.triggerPass = doPass
#         print(f"MoveTo: triggerPass set to {ego.triggerPass}")

#     dt = 0.2
#     # loop until we get within 0.5 units of our current target
#     while (distance from self to sample) > 0.5:
#         do MoveToBehavior(sample) for dt seconds
#         # Removed re-sampling logic to prevent zig-zagging
#         # The agent will now stick to the initially sampled target
#         # # if it was a distribution, re-sample whenever we leave its support
#         # if dynamic and not bool_sample(location(sample), dist):
#         #     sample = sample_from(dist)

#     # once we’ve arrived, pause for a bit
#     do Idle() for 1 seconds

# Factory MoveTo
behavior MoveTo(param, doPass: bool = False):
    # --- try the “param is a distribution” path ---
    try:
        sample  = sample_from(param)
        print(f"MoveTo: sample is {sample}")
        dynamic = False  # Changed to False to prevent re-sampling
        dist    = param
    # if sample_from isn’t defined for this type, assume it’s already a goal
    except Exception:
        scene = simulation()

        if checkIfString(param):
            param = [obj for obj in scene.objects if obj.name.lower() == param.lower()][0].position # converts string into object reference
            sample  = param
            dynamic = False
        else:
            sample  = param
            dynamic = False

    print("MoveTo: sample is ", sample)
    # only set these values if coach called moveTo
    if (self == ego and self.gameObjectType == "Coach"):
        # set xMark to the sample position
        ego.xMark = sample
        #print(f"MoveTo: xMark set to {ego.xMark}")
        # set triggerPass to doPass
        ego.triggerPass = doPass
        print(f"MoveTo: triggerPass set to {ego.triggerPass}")

    dt = 0.2
    # loop until we get within 2.5 units of our current target
    while (distance from self to sample) > 2.5:
        do MoveToBehavior(sample) for dt seconds
        # Removed re-sampling logic to prevent zig-zagging
        # The agent will now stick to the initially sampled target
        # # if it was a distribution, re-sample whenever we leave its support
        # if dynamic and not bool_sample(location(sample), dist):
        #     sample = sample_from(dist)

    # once we’ve arrived, pause for a bit
    do Idle() for 1 seconds

# behavior StopAndReceiveBall():
#     do Idle() until self.gameObject.ballPossession

# behavior WaitForConditionWithTimeout(condition, timeout_seconds=5):
#     """
#     Wait for a condition to be true, but timeout after specified seconds.
#     If timeout occurs, continue execution.
    
#     Args:
#         condition: A lambda function that returns a boolean
#         timeout_seconds: Maximum time to wait (default 5 seconds)
#     """
#     do Idle() until condition() for timeout_seconds seconds