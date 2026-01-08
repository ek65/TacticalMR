from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

A1target_0 = DistanceTo({'from': 'Robot1', 'to': 'Box2', 'min': None, 'max': {'avg': 2.0, 'std': 0.5}, 'operator': 'less_than'})
A1target_1 = DistanceTo({'from': 'Robot1', 'to': {'x': -3.08, 'y': 5.29}, 'min': None, 'max': {'avg': 2.0, 'std': 0.5}, 'operator': 'less_than'})

# A1precondition_0 = IsPackaged({'obj': 'Box1'})
A1precondition_1 = HandRaised({'player': 'Human1'})
A1precondition_11 = IsPossessed({'obj': 'Box2'})
A1precondition_2 = DistanceTo({'from': 'Robot1', 'to': 'Box2', 'min': None, 'max': {'avg': 3, 'std': 0.5}, 'operator': 'less_than'})
A1precondition_3 = DistanceTo({'from': 'Robot1', 'to': 'Human1', 'min': None, 'max': {'avg': 3, 'std': 0.5}, 'operator': 'less_than'})

def target0():
    # Move close to the box (within ~2 m)
    return A1target_0.dist(simulation(), ego=True)

def target1():
    return A1target_1.dist(simulation(), ego=True)

def precondition_0():
    return A1precondition_0.bool(simulation())

def precondition_1():
    return A1precondition_1.bool(simulation())

def precondition_11():
    return A1precondition_11.bool(simulation())

def precondition_2():
    return A1precondition_2.bool(simulation())

def precondition_3():
    return A1precondition_3.bool(simulation())

# def precondition_3():
#     return A1precondition_3.bool(simulation())

behavior HumanBehavior():
    # Always start by idling for 3 seconds
    do Idle() for 3 seconds

    # # Human packages box in front of them
    # do Packaging()

    # # Wait until Human finishes packaging Box1
    # do Idle() until precondition_0()

    # Raise hand to signal packaging complete
    do RaiseHand()

    do Idle() for 25 seconds

    do MoveToBehavior(Vector(-3.03, 7.34, 0))

    do Packaging()

behavior RobotBehavior():
    # Always start by idling for 3 seconds
    do Idle() for 3 seconds

    do Speak("Wait until human raises hand")
    do Idle() until precondition_1()
    # print("here0")

    do Speak("Pick up the Box")
    do PickUp()
    # print("here3")

    do Speak("Wait until I have picked up the Box")
    do Idle() until precondition_11()
    # do Idle() until True # make example showing that this should always be true after PickUp

    do Speak("Move to the Human to deliver the Box")
    do MoveToBehavior(Vector(-3.03, 8.24, 0))
    print("here4")

    do Speak("Wait until arrived at the Human")
    do Idle() for 2 seconds

    do Speak("Deliver the Box")
    do PutDown()

    do Idle()

ego = new Robot at (2.57, 7.82, 0),
        with behavior RobotBehavior(),
        with name "Robot1"

human = new Player at (-3.08, 6.85, 0),
        with behavior HumanBehavior(),
        with name "Human1"

# box1 = new Box at (-4, 2, 1),
#         with name "Box1"

box2 = new Box at (2.57, 8.5, 0),
        with name "Box2"

terminate when (ego.gameObject.stopButton)