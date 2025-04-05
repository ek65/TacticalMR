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
            raise RuntimeError("spped must be a float")
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
    def __init__(self, obj):
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

class ReciveBallAction(Action):
    def __init__(self, obj):
        self.actionName = "ReceiveBall"

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
        # self.actionName = "Speak"
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
    def __init__(self, obj, behavior = None):
        self.actionName = "MoveToPosRobot"
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
    
def findObj(id, objects):
    if isinstance(id, str):
        key_lower = id.lower()
        return [obj for obj in objects if key_lower in obj.name.lower()]

def isEgo(id, scene):
    return 'coach' in id.lower()
    
# MARK: Constraint
class Constraint:
    def __init__(self, args):
        self.args = args

    def __call__(self, sample, scene):
        pass

# MARK: HasBallPossession 
class HasBallPossession(Constraint):

    def __init__(self, args):
        self.playerID = args.get('player', None)

    def __call__(self, scene, sample):
        player_objs = findObj(self.playerID, scene.objects)

        if not player_objs:
            print(f"Player '{self.player}' not found in the scene.")
            return False

        return player_objs[0].gameObject.ballPossession
    
# MARK: InZone

FIELD_WIDTH, FIELD_HEIGHT = 20, 34
NUM_ZONES_X, NUM_ZONES_Y = 4, 5
ZONE_WIDTH = FIELD_WIDTH / NUM_ZONES_X
ZONE_HEIGHT = FIELD_HEIGHT / NUM_ZONES_Y

class InZone(Constraint):

    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.zone = args.get('zone', None)

    def __call__(self, scene, sample):
        pos = None

        if not (sample and isEgo(self.objID, scene)):
        # if self.objID != scene.egoObject.name:
            objs = findObj(self.objID, scene.objects)

            if not objs:
                print(f"Object '{self.objID}' not found in the scene.")
                return False
            
            obj = objs[0]
            _pos = getattr(obj, 'position', (sample[0], sample,[1], 0))
            pos = (_pos[0], _pos[1])

        else:
            pos = sample

        zone_label = self.get_zone(pos)
        return zone_label in self.zone

    def get_zone(self, point):

        point_x = point[0]
        point_y = point[1]

        x_zone, y_zone = "", ""

        if -10 <= point_x < -5:
            x_zone = 'A'
        elif -5 <= point_x < 0:
            x_zone = 'B'
        elif 0 <= point_x < 5:
            x_zone = 'C'
        else:
            x_zone = 'D'
        
        if -17 <= point_y < -17 + ZONE_HEIGHT:
            y_zone = '1'
        elif -17 + ZONE_HEIGHT <= point_y < -17 + ZONE_HEIGHT*2:
            y_zone = '2'
        elif -17 + ZONE_HEIGHT*2 <= point_y < -17 + ZONE_HEIGHT*3:
            y_zone = '3'
        elif -17 + ZONE_HEIGHT*3 <= point_y < -17 + ZONE_HEIGHT*4:
            y_zone = '4'
        else:
            y_zone = '5'

        return x_zone + y_zone
        
# MARK: MovingTowards
class MovingTowards(Constraint):

    def __init__(self, args={}):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)

    def __call__(self, scene, sample):

        # if self.objID == scene.egoObject.name:
        # if sample and isEgo(self.passerID, scene):
        #     moving_obj = scene.egoObject
        # else:
        #     moving_objs = findObj(self.objID, scene.objects)

        #     if not moving_objs:
        #         print(f"Moving object '{self.objID}' not found in the scene.")
        #         return False
            
        #     moving_obj = moving_objs[0]

        moving_objs = findObj(self.objID, scene.objects)
        ref_objs = findObj(self.refID, scene.objects)

        if not ref_objs:
            print(f"Reference object '{self.refID}' not found in the scene.")
            return False
        
        if not moving_objs:
            print(f"Reference object '{self.refID}' not found in the scene.")
            return False
        
        ref_obj = ref_objs[0]
        moving_obj = moving_objs[0]

        def distance(pos1, pos2):
            return np.sqrt((pos1.x - pos2.x) ** 2 + (pos1.y - pos2.y) ** 2)
        
        # if self.objID == scene.egoObject.name:
        #     current_distance = distance(Vector(sample[0], sample[1], 0), ref_obj.position)
        #     previous_distance = distance(moving_obj.position, ref_obj.prevPosition)
        # else:
        #     current_distance = distance(moving_obj.position, ref_obj.position)
        #     previous_distance = distance(moving_obj.prevPosition, ref_obj.prevPosition)

        current_distance = distance(moving_obj.position, ref_obj.position)
        previous_distance = distance(moving_obj.prevPosition, ref_obj.prevPosition)
        # print(f"distance travelled: {previous_distance - current_distance}")
        return previous_distance - current_distance <= -0.05

# MARK: Pressure
class Pressure(Constraint):
    def __init__(self, args={}):
        self.player1 = args.get('player1', None)
        self.player2 = args.get('player2', None)

    def __call__(self, scene, sample):
        player1 = findObj(self.player1, scene.objects)[0]
        player2 = findObj(self.player2, scene.objects)[0]

        behav = player1.gameObject.behavior.lower()
        name = player2.name.lower()

        if 'follow' in behav and name in behav:
            return True
        return False
    
# MARK: MakePass
class MakePass(Constraint):
    def __init__(self, args={}):
        self.player = args.get('player', None)

    def __call__(self, scene, sample):
        player_list = findObj(self.player, scene.objects)
        if not player_list:
            print(f"MakePass(): Player '{self.player}' not found in the scene.")
            return True # so that the program would move forward
        else:
            player = player_list[0]
        # print(f"player: {player}")
        # print(f"type(player): {type(player)}")

        behav = player.gameObject.behavior
        # print(f"MakePass(): player: {player.name}, behavior: {behav}")

        if behav is None or behav == "":
            return False
        
        else:
            if 'pass' in behav.lower():
                return True
        return False


# MARK: HasPathToPass
class HasPath(Constraint):

    def __init__(self, args={}):
        self.passerID = args.get('obj1', None)
        self.receiverID = args.get('obj2', None)
        self.radius = args.get('path_width', None)
        self.radiusAvg = self.radius.get('avg', 0.0)
        self.radiusStd = self.radius.get('std', 1.0)

    def __call__(self, scene, sample):
        if sample and isEgo(self.passerID, scene):
            passer_pos = Vector(sample[0], sample[1], 0)
            receiver_list = findObj(self.receiverID, scene.objects)
            receiver_obj = receiver_list[0]
            start = passer_pos
            end   = receiver_obj.position
        elif sample and isEgo(self.receiverID, scene):
            receiver_pos = Vector(sample[0], sample[1], 0)
            passerID_list = findObj(self.passerID, scene.objects)
            passer_obj = passerID_list[0]
            start = passer_obj.position
            end   = receiver_pos
        else:
            passer_list = findObj(self.passerID, scene.objects)
            receiver_list = findObj(self.receiverID, scene.objects)

            passer_obj = passer_list[0]
            receiver_obj = receiver_list[0]
        
            start = passer_obj.position
            end = receiver_obj.position

        min_d = 100
        for obj in scene.objects:
            if 'opponent' in obj.name.lower():
                d = self.distance_to_line(start, end, obj.position)
                # print(f"distance d: {d}")
                min_d = min(min_d, d)
        
        # print(f"min_d: {min_d}, radiusAvg: {self.radiusAvg}")

        if self.radius is None:
            print("Warning: No radius threshold learned.")
            return False
        
        # print(f"HasPathToPass: min_d: {min_d}")
        return min_d >= self.radiusAvg - self.radiusStd

    def distance_to_line(self, start, end, point):
        """
        Calculate the minimum distance from a point to a line segment.
        
        Parameters:
            start (tuple): The (x, y) coordinates of the start point of the segment.
            end (tuple): The (x, y) coordinates of the end point of the segment.
            point (tuple): The (x, y) coordinates of the point.
            
        Returns:
            float: The shortest distance from 'point' to the line segment between 'start' and 'end'.
        """
        x1, y1 = start.x, start.y
        x2, y2 = end.x, end.y
        x0, y0 = point.x, point.y
        
        # Compute the vector from start to end
        dx = x2 - x1
        dy = y2 - y1
        
        # Compute the squared length of the line segment
        segment_length_sq = dx * dx + dy * dy
        
        # If the segment is a single point, return the distance from the point to start
        if segment_length_sq == 0:
            print("HasPathToPass: Segment length is zero")
            return math.hypot(x0 - x1, y0 - y1)
        
        # Compute the projection scalar of point onto the line (normalized by segment length)
        t = ((x0 - x1) * dx + (y0 - y1) * dy) / segment_length_sq

        # print(f"HasPathToPass) t: {t}")
        
        # If the projection falls before the start of the segment, use the start point
        if t < 0:
            distance = math.hypot(x0 - x1, y0 - y1)
            return distance
        elif t > 1:
            distance = math.hypot(x0 - x2, y0 - y2)
            return distance
        
        # Otherwise, the projection falls on the segment. Compute the projection coordinates.
        proj_x = x1 + t * dx
        proj_y = y1 + t * dy
        
        # Return the distance from the point to the projection point
        return math.hypot(x0 - proj_x, y0 - proj_y)
    
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
            self.max_std = self.max.get('std', 1.0)
        else:
            self.max = {'avg': 3.0, 'std': 1.0}
            self.max_avg = 3.0
            self.max_std = 1.0

    def __call__(self, scene, sample):
        obj_pos, ref_pos = None, None

        if sample and isEgo(self.obj, scene):
            obj_pos = Vector(sample[0], sample[1], 0)
            ref = findObj(self.ref, scene.objects)
            ref_pos = ref[0].position
        elif sample and isEgo(self.ref, scene):
            ref_pos = Vector(sample[0], sample[1], 0)
            obj = findObj(self.obj, scene.objects)
            obj_pos = obj[0].position
        else:
            obj = findObj(self.obj, scene.objects)
            ref = findObj(self.ref, scene.objects)

            if not obj:
                print(f"CloseTo(): Object '{self.obj}' not found in the scene.")
                return True # so that the program would at least move on
            
            if not ref:
                print(f"CloseTo(): Reference object '{self.ref}' not found in the scene.")
                return True # so that the program would at least move on
            
            obj_pos = obj[0].position
            ref_pos = ref[0].position
        
        def distance(pos1, pos2):
            return np.sqrt((pos1.x - pos2.x) ** 2 + (pos1.y - pos2.y) ** 2)

        dist = distance(obj_pos, ref_pos)
        # print(f"dist: {dist}")
        return dist <= self.max_avg + self.max_std

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

    def __call__(self, scene, sample):

        if sample and isEgo(self.fromID, scene):
            pos1 = Vector(sample[0], sample[1], 0)
        else:
            from_objs = findObj(self.fromID, scene.objects)
            if not from_objs:
                print(f"Missing objects for DistanceTo: from_obj '{self.fromID}' not found.")
                return False
            from_obj = from_objs[0]
            pos1 = from_obj.position

        to_objs = findObj(self.toID, scene.objects)
        if not to_objs:
            print(f"Missing objects for DistanceTo: to_obj '{self.toID}' not found.")
            return False
        to_obj = to_objs[0]
        pos2 = to_obj.position

        dist = np.sqrt((pos1.x - pos2.x) ** 2 + (pos1.y - pos2.y) ** 2)

        min_threshold = self.minAvg
        max_threshold = self.maxAvg

        if self.operator == 'within':
            return min_threshold <= dist <= max_threshold
        elif self.operator == 'less_than':
            return dist < max_threshold
        elif self.operator == 'greater_than':
            return dist > min_threshold
        else:
            print(f"Unknown operator '{self.operator}' in DistanceTo constraint.")
            return False
        
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

    def __call__(self, scene, sample):
        if sample and isEgo(self.objID, scene):
            player_y = sample[1]
            if self.refID:
                ref_objs = findObj(self.refID, scene.objects)
                if not ref_objs:
                    print(f"HeightRelation() IF: Reference object '{self.refID}' not found in the scene.")
                    return True # so that the program would at least move on
                ref_obj = ref_objs[0]
                ref_y = ref_obj.position.y
                value = player_y - ref_y
            else:
                value = player_y

        elif sample and isEgo(self.refID, scene):
            ref_y = sample[1]
            if self.objID:
                player_objs = findObj(self.objID, scene.objects)
                if not player_objs:
                    print(f"HeightRelation() ELIF: obj Player '{self.objID}' not found in the scene.")
                    return True
                player_obj = player_objs[0]
                player_y = player_obj.position.y
                value = player_y - ref_y
            else:
                value = ref_y

        else:
            player_objs = findObj(self.objID, scene.objects)
            ref_objs = findObj(self.refID, scene.objects)
            if player_objs and ref_objs:
                player_obj = player_objs[0]
                ref_obj = ref_objs[0]
                player_y = player_obj.position.y
                ref_y = ref_obj.position.y
                value = player_y - ref_y
            elif not player_objs:
                print(f"HeightRelation() ELIF: obj Player '{self.objID}' not found in the scene.")
                return True # so that the program would at least move on
            elif player_objs and not ref_objs:
                player_obj = player_objs[0]
                player_y = player_obj.position.y
                value = player_y
            else:
                print(f"HeightRelation() ELSE: ref = '{self.refID}' and obj = '{self.refID}' not found in the scene.")
                return True # so that the program would at least move on
        
        if self.relation == 'below' and self.refID and self.objID:
            return value < 0 and abs(value) > self.threshold_avg - self.threshold_std
        elif self.relation == 'below' and not (self.refID and self.objID):
            return value < 0 and abs(value) > self.threshold_avg - self.threshold_std
        elif self.relation == 'above' and self.refID and self.objID:
            return value > 0 and abs(value) > self.threshold_avg - self.threshold_std
        elif self.relation == 'above' and not (self.refID and self.objID):
            return value > 0 and abs(value) > self.threshold_avg - self.threshold_std
        else:
            print(f"HeightRelation(): Unknown relation '{self.relation}' in HeightRelation.")
            return True # so that the program would at least move on

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

    def __call__(self, scene, sample):
        if sample and isEgo(self.objID, scene):
            player_x = sample[0]
            if self.refID:
                ref_objs = findObj(self.refID, scene.objects)
                if not ref_objs:
                    print(f"HorizontalRelation() IF: Reference object '{self.refID}' not found in the scene.")
                    return True # so that the program would at least move on
                ref_obj = ref_objs[0]
                ref_x = ref_obj.position.x
                value = player_x - ref_x
            else:
                value = player_x

        elif sample and isEgo(self.refID, scene):
            ref_y = sample[0]
            if self.objID:
                player_objs = findObj(self.objID, scene.objects)
                if not player_objs:
                    print(f"HorizontalRelation() ELIF: obj Player '{self.objID}' not found in the scene.")
                    return True
                player_obj = player_objs[0]
                player_x = player_obj.position.x
                value = player_x - ref_x
            else:
                value = ref_x

        else:
            player_objs = findObj(self.objID, scene.objects)
            ref_objs = findObj(self.refID, scene.objects)
            if player_objs and ref_objs:
                player_obj = player_objs[0]
                ref_obj = ref_objs[0]
                player_x = player_obj.position.x
                ref_x = ref_obj.position.x
                value = player_x - ref_x
            elif not player_objs:
                print(f"HorizontalRelation() ELIF: obj Player '{self.objID}' not found in the scene.")
                return True # so that the program would at least move on
            elif player_objs and not ref_objs:
                player_obj = player_objs[0]
                player_x = player_obj.position.x
                value = player_x
            else:
                print(f"HorizontalRelation() ELSE: ref = '{self.refID}' and obj = '{self.refID}' not found in the scene.")
                return True # so that the program would at least move on
        
        if self.relation == 'left' and self.refID and self.objID:
            return value < 0 and abs(value) > self.threshold_avg - self.threshold_std
        elif self.relation == 'left' and not (self.refID and self.objID):
            return value < 0 and abs(value) > self.threshold_avg - self.threshold_std
        elif self.relation == 'right' and self.refID and self.objID:
            return value > 0 and abs(value) > self.threshold_avg - self.threshold_std
        elif self.relation == 'right' and not (self.refID and self.objID):
            return value > 0 and abs(value) > self.threshold_avg - self.threshold_std
        else:
            print(f"HorizontalRelation(): Unknown relation '{self.relation}' .")
            return True # so that the program would at least move on

# MARK: OrientedTo
class OrientedTo(Constraint):
    def __init__(self, args):
        self.objID = args.get('obj', None)
        self.refID = args.get('ref', None)
        self.side = args.get('side', None)

        self.min_angle = args.get('min', 0.0)
        self.max_angle = args.get('max', float('inf'))
        self.operator = args.get('operator', 'within')

    def __call__(self, scene, sample):

        if self.objID == scene.egoObject.name:
            obj_pos = Vector(sample[0], sample[1], 0)
        else:
            obj_list = findObj(self.objID, scene.objects)
            if not obj_list:
                print(f"Object '{self.objID}' not found in the scene.")
                return False
            obj_pos = obj_list[0].position

        ref_list = findObj(self.refID, scene.objects)
        if not ref_list:
            print(f"Reference object '{self.refID}' not found in the scene.")
            return False
        ref_obj = ref_list[0]
        ref_pos = ref_obj.position

        try:
            ref_orientation = ref_obj.orientation
        except AttributeError:
            print(f"Reference object '{self.refID}' has no orientation attribute.")
            return False
        
        v = np.array([obj_pos.x - ref_pos.x, obj_pos.y - ref_pos.y])
        u = np.array([ref_orientation.x, ref_orientation.y])
        dot = np.dot(u, v)
        cross = u[0] * v[1] - u[1] * v[0]
        angle = np.arctan2(cross, dot)

        if self.side == 'left':
            if angle < 0:
                return False
            computed_angle = angle
        elif self.side == 'right':
            if angle > 0:
                return False
            computed_angle = -angle
        else:
            print(f"Unknown side '{self.side}' specified in OrientedTo.")
            return False

        if self.operator == 'within':
            return self.min_angle <= computed_angle <= self.max_angle
        elif self.operator == 'less_than':
            return computed_angle < self.max_angle
        elif self.operator == 'greater_than':
            return computed_angle > self.min_angle
        else:
            print(f"Unknown operator '{self.operator}' in OrientedTo.")
            return False
