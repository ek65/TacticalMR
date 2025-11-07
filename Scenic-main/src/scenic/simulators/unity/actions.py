from scenic.core.simulators import Action
from scenic.core.vectors import Vector
from scenic.core.object_types import OrientedPoint, Point
from scenic.simulators.unity.client import *
from enum import Enum
import math
import numpy as np

# Language: Python 3
# This file holds all base actions defined for scenic
# Each action should follow similar format:
#
#   class ClassAction(Action):
#       def __init__(self, ...):
#           ...
#       def applyTo(self, obj, sim):
#           ...class MoveToAction(Action):

class PrintAction(Action):
    def __init__(self, output):
        self.actionName = "ScenicPrint"
        
        if not isinstance(output, str):
            raise RuntimeError("Print output must be a string")
        self.output = output

    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName, self.output)

class StopAction(Action):
    def __init__(self):
        pass
    def applyTo(self, obj, sim):
        obj.gameObject.StopAction()

class IdleAction(Action):
    def __init__(self, behavior = "Idle"):
        self.actionName = "Idle"
        self.behavior = behavior

    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior(self.behavior)
        obj.gameObject.DoAction(self.actionName)

class SetPlayerSpeedAction(Action):
    def __init__(self, speed):
        self.actionName = "SetPlayerSpeed"
        
        if not isinstance(speed, float):
            raise RuntimeError("speed must be a float")
        self.speed = speed

    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName, self.speed)

class MoveToWithSpeed(Action):
    def __init__(self, obj, speed):
        self.actionName = "MoveToPos"

        if not isinstance(speed, float):
            raise RuntimeError("height must be a float")
        self.speed = speed

        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = (obj.position.x, obj.position.y, obj.position.z)
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID

    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName, self.position, self.speed)

class MoveToLookAtBallWithSpeed(Action):
    def __init__(self, obj, speed):
        self.actionName = "MoveToPosLookAtBall"
        self.behavior = ""

        if not isinstance(speed, float):
            raise RuntimeError("height must be a float")
        self.speed = speed

        if isinstance(obj, OrientedPoint):
            self.position = obj.position
            self.behavior = f"move to:{obj.name}"
        elif isinstance(obj, Point):
            self.position = (obj.position.x, obj.position.y, obj.position.z)
            self.behavior = f"move to:{self.position}"
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
            self.behavior = f"move to:{self.position}"
        else:
            raise RuntimeError(f"Unacceptable Input Detected: {obj}")

    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior(self.behavior)
        obj.gameObject.DoAction(self.actionName, self.position)

class MoveToAction(Action):
    def __init__(self, obj, behavior = None):
        self.actionName = "MoveToPos"
        self.behavior = behavior

        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = (obj.position.x, obj.position.y, obj.position.z)
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior(self.behavior)
        obj.gameObject.DoAction(self.actionName, self.position)

class LookAtAction(Action):
    def __init__(self, obj, behavior = None):
        self.actionName = "LookAt"
        self.behavior = behavior

        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = (obj.position.x, obj.position.y, obj.position.z)
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior(self.behavior)
        obj.gameObject.DoAction(self.actionName, self.position)

class DribbleToAction(Action):
    def __init__(self, obj):
        self.actionName = "DribbleFromOnePositionToAnother"

        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position)

class BallHeaderShootAction(Action):
    def __init__(self, obj, height):
        self.actionName = "BallHeaderShoot"

        if not isinstance(height, str):
            raise RuntimeError("height must be a str.")
        self.height = height
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position, self.height)

# class ReciveBallAction(Action):
#     def __init__(self, obj):
#         self.actionName = "ReceiveBall"

#         if isinstance(obj, tuple) or type(obj) is tuple:
#             self.position = obj
#         elif isinstance(obj, OrientedPoint):
#             self.position = obj.position
#         elif isinstance(obj, Point):
#             self.position = obj.position
#         elif isinstance(obj, Vector):
#             self.position = (obj.x, obj.y, obj.z)
#         else:
#             self.clientID = obj.gameObject.clientID
#     def applyTo(self, obj, sim):
#         if self.position is None:
#             obj.gameObject.MoveToObject(self.clientID)
#         else:
#             obj.gameObject.DoAction(self.actionName, self.position)

class InterceptBallAction(Action):
    def __init__(self):
        self.actionName = "InterceptBall"
    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)

class TackleBallAction(Action):
    def __init__(self, obj):
        self.actionName = "TackleBall"

        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID

    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position)

class GroundPassSlowAction(Action):
    def __init__(self, obj):
        self.actionName = "GroundPassSlow"
        print("in ground pass slow")

        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            print("in ground pass slow vec")
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            print(self.position)
            obj.gameObject.DoAction(self.actionName, self.position)

class GroundPassFastAction(Action):
    def __init__(self, obj, behavior = None):
        self.actionName = "GroundPassFast"
        self.behavior = behavior

        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint) or isinstance(obj, Point):
            pos = obj.position
            self.position = (pos.x, pos.y, pos.z)
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.position = obj.position

    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior(self.behavior)
        obj.gameObject.DoAction(self.actionName, self.position)

class AirPassAction(Action):
    def __init__(self, obj, height):
        self.actionName = "AirPass"

        if not isinstance(height, str):
            raise RuntimeError("height must be a str.")
        self.height = height
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position, self.height)

class ChipLeftAction(Action):
    def __init__(self, obj, height):
        self.actionName = "ChipLeft"

        if not isinstance(height, str):
            raise RuntimeError("height must be a str.")
        self.height = height
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position, self.height)

class ChipRightAction(Action):
    def __init__(self, obj, height):
        self.actionName = "ChipRight"
        
        if not isinstance(height, str):
            raise RuntimeError("height must be a str.")
        self.height = height
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position, self.height)

class ChipFrontAction(Action):
    def __init__(self, obj, height):
        self.actionName = "ChipFront"

        if not isinstance(height, str):
            raise RuntimeError("height must be a str.")
        self.height = height
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position, self.height)

class ShootAction(Action):
    def __init__(self, obj, zone, behavior = None):
        self.actionName = "Shoot"
        self.behavior = behavior

        if not isinstance(zone, str):
            raise RuntimeError("zone must be a str.")
        self.zone = zone
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.SetBehavior(self.behavior)
            obj.gameObject.DoAction(self.actionName, self.position, self.zone)

class BallThrowAction(Action):
    def __init__(self, obj, height):
        self.actionName = "BallThrow"

        if not isinstance(height, str):
            raise RuntimeError("zone must be a str.")
        self.height = height
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position, self.height)

# Goalkeeper Animations 
# TODO: make these actions specific to goalkeepers only

class IdleWithBallInHandAction(Action):
    def __init__(self):
        self.actionName = "IdleWithBallInHand"
    def applyTo(self, obj, sim):
        
        obj.gameObject.DoAction(self.actionName)

class BodyBlockLeftSideAction(Action):
    def __init__(self):
        self.actionName = "BodyBlockLeftSide"
    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)

class BodyBlockRightSideAction(Action):
    def __init__(self):
        self.actionName = "BodyBlockRightSide"
    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)

class CatchGroundBallAction(Action):
    def __init__(self):
        self.actionName = "CatchGroundBall"
    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)

class CatchBallInTheAirAction(Action):
    def __init__(self):
        self.actionName = "CatchBallInTheAir"
    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)

class CatchSlowBallAction(Action):
    def __init__(self):
        self.actionName = "CatchSlowBall"
    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)

class CatchSlowBallAction(Action):
    def __init__(self):
        self.actionName = "CatchSlowBall"
    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)

class DropKickShotAction(Action):
    def __init__(self, obj, height):
        self.actionName = "DropKickShot"

        if not isinstance(height, str):
            raise RuntimeError("height must be a str.")
        self.height = height
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position, self.height)

class OverHandThrowAction(Action):
    def __init__(self, obj, height):
        self.actionName = "OverHandThrow"

        if not isinstance(height, str):
            raise RuntimeError("height must be a str.")
        self.height = height
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position, self.height)

class RollingBallPassAction(Action):
    def __init__(self, obj):
        self.actionName = "RollingBallPass"
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position)

class PlacingAndLongPassAction(Action):
    def __init__(self, obj, height):
        self.actionName = "PlacingAndLongPass"

        if not isinstance(height, str):
            raise RuntimeError("height must be a str.")
        self.height = height
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position, self.height)

class PlacingAndShortPassAction(Action):
    def __init__(self, obj):
        self.actionName = "PlacingAndShortPass"
        
        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = obj.position
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        if self.position is None:
            obj.gameObject.MoveToObject(self.clientID)
        else:
            obj.gameObject.DoAction(self.actionName, self.position)

# Coach/Human/AIAgent Actions
class SpeakAction(Action):
    def __init__(self, input):
        self.actionName = "Speak"
        inputString = "Say \"" + input + "\""
        
        if not isinstance(input, str):
            raise RuntimeError("output must be a string")
        self.input = inputString
        # self.input = input

    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName, self.input)

class PauseAction(Action):
    def __init__(self):
        self.actionName = "CallPause"

    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)


class UnpauseAction(Action):
    def __init__(self):
        self.actionName = "CallUnpause"

    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)

class SegmentStartAction(Action):
    def __init__(self):
        self.actionName = "SegmentStart"

    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)

class SegmentEndAction(Action):
    def __init__(self):
        self.actionName = "SegmentEnd"

    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName)

class ExplainAction(Action):
    def __init__(self, input):
        self.actionName = "Explain"
        
        if not isinstance(input, str):
            raise RuntimeError("output must be a string")
        self.input = input

    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName, self.input)

# MARK: MovingStyle
class MovingStyle(Enum):
    WALK = 'walk'
    RUN = 'run'
    SPRINT = 'sprint'

class CoordinateInit(Enum):
    RELATIVE = 'relative'

class Coordinate():
    def __init__(self, init: CoordinateInit, ref: list):
        self.init = init
        self.ref = ref
        self.weights = {}

    def weighted(self, weights):
        self.weights = weights
        return self

    def predict(self, ref: list):
        weighted = {}
        for obj in ref:
            # print("type(obj): ", str(type(obj)))
            # print("obj.position: ", obj.position)

            w, m, wi = 0, 0, 0
            for w_obj, weight in self.weights.items():
                if isinstance(obj, type(w_obj)):
                    wi += 1
                    if "Player" in str(type(w_obj)) and obj.gameObject.ballPossession == w_obj.gameObject.ballPossession:
                        wi += 1
                                
                if wi > m:
                    m = wi
                    w = weight

            weighted[obj] = w

        destination = Vector(sum([w * obj.position.x for obj, w in weighted.items()])/ len(weighted.keys()),
                    sum([w * obj.position.y for obj, w in weighted.items()])// len(weighted.keys()))
        
        # print("destination: ", destination)

        return destination


class SpeedInit(Enum):
    MAGNITUDE = 'magnitude'
    RELATIVE = 'relative'
    PROPORTIONAL = 'proportional'

class Speed():
    def __init__(self, init: SpeedInit, ref = None):
        self.init = init
        self.ref = ref
        self.weight = 0.0

    def weighted(self, weight: float):
        self.weight = weight # TODO: the weight should be a dictionary with respect to a referenced object like Coordinate
        return self

    def predict(self):
        return float(5) # TODO: actually compute


class MoveToLookAtBallWithSpeed(Action):
    def __init__(self, obj, speed):
        self.actionName = "MoveToPosLookAtBall"
        if not isinstance(speed, float):
            raise RuntimeError("height must be a float")
        self.speed = speed

        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = (obj.position.x, obj.position.y, obj.position.z)
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID

    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior("Move While Looking at Ball")
        obj.gameObject.DoAction(self.actionName, self.position, self.speed)


class Object:
    def __init__(self, label, type, location):
        self.label = label
        self.type = type
        self.location = location
    
# class AheadOfLine(Constraint):

#     def __init__(self, args):
#         self.obj = args.get('obj', None)
#         self.height = args.get('height', None)

#     def __call__(self, scene, sample):
#         avg = self.height.get('avg', 0)
#         std = self.height.get('std', 0)

#         if isinstance(self.obj, str):
#             self.obj = [obj for obj in scene.objects if obj.name.lower() == self.obj.lower()][0] # converts string into object reference

#         # if no obj given, use coach object
#         if self.obj is None:
#             coachObj = [obj for obj in scene.objects if obj.name.lower() == "coach"][0]
#             self.obj = coachObj

#         # if no sample position, use self.obj's current position
#         if sample is None:
#             sample = self.obj.position

#         # TODO: change to use PDF later
#         h = avg

#         if sample[1] >= h:
#             return True
        
#         return False
    
# class DistanceToObject(Constraint):

#     def __init__(self, args):
#         self.ref = args.get('ref', None)  # Reference object to measure distance to
#         self.obj = args.get('obj', None)  # Object to check distance from (defaults to coach)
#         self.min_dist = args.get('min_dist', None)  # Minimum distance threshold
#         self.max_dist = args.get('max_dist', None)  # Maximum distance threshold
#         self.operator = args.get('operator', 'between')  # Comparison operator: 'between', 'less_than', 'greater_than'


#     def __call__(self, scene, sample):
#         """
#         Checks if the distance between objects satisfies the constraint based on the operator.
#         Returns True if the constraint is satisfied, False otherwise.
#         """
#         # Get reference object
#         ref_obj = [obj for obj in scene.objects if obj.name.lower() == self.ref][0]
        
#         # Get current position to check (either sample point or object position)
#         if self.obj is None:
#             current_pos = [obj for obj in scene.objects if obj.name.lower() == "coach"][0].position  # Direct position for coach/target
#         else:
#             current_pos = [obj for obj in scene.objects if obj.name.lower() == self.obj.lower()][0].position

#         distance = self.calculate_distance(ref_obj.position, current_pos)

#         # TODO: Change to actually sample from PDF
#         if (self.min_dist is not None):
#             min_dist_sample = self.min_dist.get('avg', 0) - self.min_dist.get('std', 0)
#         else:
#             min_dist_sample = 0

#         if (self.max_dist is not None):
#             max_dist_sample = self.max_dist.get('avg', 0) + self.max_dist.get('std', 0)
#         else:
#             max_dist_sample = 1000
        
#         # Check distance based on operator
#         if self.operator == 'between':
#             return (min_dist_sample <= distance <= max_dist_sample)
#         elif self.operator == 'less_than':
#             return distance <= max_dist_sample
#         elif self.operator == 'greater_than':
#             return distance >= min_dist_sample
        
#         return False  # Invalid operator
    
#     def calculate_distance(self, pos1, pos2):
#         """Helper function to calculate distance between two positions"""
#         return np.sqrt((pos1.x - pos2.x)**2 + (pos1.y - pos2.y)**2)
    
def checkIfString(target):
    return isinstance(target, str)


# --------------------
# MARK: Robot Scenario Actions
# --------------------
class MoveToRobotAction(Action):
    def __init__(self, obj, behavior = "Move To"):
        self.actionName = "FactoryMoveToPos"
        self.behavior = behavior

        if isinstance(obj, tuple) or type(obj) is tuple:
            self.position = obj
        elif isinstance(obj, OrientedPoint):
            self.position = obj.position
        elif isinstance(obj, Point):
            self.position = (obj.position.x, obj.position.y, obj.position.z)
        elif isinstance(obj, Vector):
            self.position = (obj.x, obj.y, obj.z)
        else:
            self.clientID = obj.gameObject.clientID
    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior(self.behavior)
        obj.gameObject.DoAction(self.actionName, self.position)

class PickUpAction(Action):
    def __init__(self, behavior = "Pick Up"):
        self.actionName = "PickUp"
        self.behavior = behavior
    
    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior(self.behavior)
        obj.gameObject.DoAction(self.actionName)

class PutDownAction(Action):
    def __init__(self, obj, behavior = "Put Down"):
        self.actionName = "PutDown"
        self.behavior = behavior

    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior(self.behavior)
        obj.gameObject.DoAction(self.actionName)


class PackagingAction(Action):
    def __init__(self, behavior = "Packaging"):
        self.actionName = "Packaging"
        self.behavior = behavior
    
    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior(self.behavior)
        obj.gameObject.DoAction(self.actionName)

class RaiseHandAction(Action):
    def __init__(self, behavior = "Raise Hand"):
        self.actionName = "RaiseHand"
        self.behavior = behavior
    
    def applyTo(self, obj, sim):
        obj.gameObject.SetBehavior(self.behavior)
        obj.gameObject.DoAction(self.actionName)