from scenic.simulators.unity.actions import *
from scenic.simulators.unity.constraints import *
from enum import Enum
model scenic.simulators.unity.model
from scenic.core.vectors import Orientation, Vector
from scenic.core.object_types import Point

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

behavior InterceptBall(ball):
    while (distance from self to ball) > 0.5:
        # print(distance from self to ball)
        take MoveToAction(ball.position, "Intercept Ball")
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
        target = [obj for obj in scene.objects if obj.name.lower() == target][0].position # converts string into object reference
        # print("elif case")
    # print(f"Passing to {target}")
    # print(f"type: {type(target)}")

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
    
    if lookAtTarget is not None:
        do LookAt(lookAtTarget)
    else:
        do LookAt(v)
    

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

# MARK: MoveTo
behavior MoveTo(λ_dest):
    scene = simulation()
    sample = Vector(0, 0)
    sample = sample_target(scene, sample, λ_dest)
    timestep = 0.3
    # print(f"sample: {sample}")
    while (distance from self to sample > 0.5):
        # print('moving to', sample)
        do MoveToBehavior(sample) for timestep seconds
        scene = simulation()
        sample = sample_target(scene, sample, λ_dest)
        # print(f"sample: {sample}")
    do Idle() for 1 seconds

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

behavior GetBallPossession(ball):
    while not self.gameObject.ballPossession:
        take MoveToAction(ball.position)


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

# -------------------------------

import numpy as np
import matplotlib.pyplot as plt

rows, cols = 34, 20
i, j = np.indices((rows, cols))

def sample_from(dist, _min=0.4):

    max_val = dist.max()
    if max_val > 0:
        dist = dist / max_val

    filtered = np.where(dist >= _min, dist, 0.0)
    
    total = filtered.sum()
    if total == 0:
        filtered += epsilon
    
    probs = filtered / total
    flat = probs.ravel()
    
    idx = np.random.choice(flat.size, p=flat)
    coord = np.unravel_index(idx, dist.shape)

    x, y = coord[1].item(), coord[0].item()
    print('real sampled', x, y)
    sample = Vector(x - cols / 2, rows / 2 - y)
    print('Sampled', sample)

    return sample

behavior MoveAs(dist):
    sample = sample_from(dist)
    dt = 0.2
    while (distance from self to sample > 0.5):
        print('moving to', sample)
        do MoveToBehavior(sample) for dt seconds
    do Idle() for 1 seconds
    # TODO: Check if sample is still satisfied and if not re-sample