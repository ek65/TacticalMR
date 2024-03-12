from scenic.core.simulators import Action
from scenic.core.vectors import Vector
from scenic.core.object_types import OrientedPoint, Point
from scenic.simulators.unity.client import *

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

# AIAgent Actions
class SpeakAction(Action):
    def __init__(self, input):
        self.actionName = "Speak"
        
        if not isinstance(input, str):
            raise RuntimeError("output must be a string")
        self.input = input

    def applyTo(self, obj, sim):
        obj.gameObject.DoAction(self.actionName, self.input)