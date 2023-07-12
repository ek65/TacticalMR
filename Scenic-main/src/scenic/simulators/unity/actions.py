from scenic.core.simulators import Action
from scenic.core.vectors import Vector
from scenic.core.object_types import OrientedPoint, Point

# Language: Python 3
# This file holds all base actions defined for scenic
# Each action should follow similar format:
#
#   class ClassAction(Action):
#       def __init__(self, ...):
#           ...
#       def applyTo(self, obj, sim):
#           ...class MoveToAction(Action):

class MoveToAction(Action):
    def __init__(self, obj):
        if isinstance(obj,tuple) or type(obj) is tuple:
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
            obj.gameObject.MoveToPosition(self.position)