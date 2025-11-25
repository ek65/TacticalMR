from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

# Constraint and target definitions for Coach (Robot1) and Human1

# Targets for Robot1 to approach Box1 and Human1
A1target_box = DistanceTo({'from': 'Robot1', 'to': 'Box1', 'min': None,
                           'max': {'avg': 1.0, 'std': 0.1}, 'operator': 'less_than'})
A2target_human = DistanceTo({'from': 'Robot1', 'to': 'Human1', 'min': None,
                             'max': {'avg': 2.0, 'std': 0.2}, 'operator': 'less_than'})

# Preconditions and monitoring conditions
A1pre_handRaised = HandRaised({'player': 'Human1'})
A2pre_possessed = IsPossessed({'obj': 'Box1'})
A3pre_packaged = IsPackaged({'obj': 'Box1'})
A4pre_robotNearHuman = DistanceTo({'from': 'Robot1', 'to': 'Human1', 'min': None,
                                   'max': {'avg': 2.0, 'std': 0.2}, 'operator': 'less_than'})

def λ_target_box():
    cond = A1target_box
    return cond.dist(simulation(), ego=True)

def λ_target_human():
    cond = A2target_human
    return cond.dist(simulation(), ego=True)

def λ_pre_handRaised():
    return A1pre_handRaised.bool(simulation())

def λ_pre_possessed():
    return A2pre_possessed.bool(simulation())

def λ_pre_packaged():
    return A3pre_packaged.bool(simulation())

def λ_pre_readyToPackage():
    # Robot is close to Human1 and the box is no longer being held
    print("A4pre_robotNearHuman", A4pre_robotNearHuman.bool(simulation()))
    print("A2pre_possessed", A2pre_possessed.bool(simulation()))
    cond = A4pre_robotNearHuman & ~A2pre_possessed
    return cond.bool(simulation())


behavior CoachBehavior():
    do Idle() for 3 seconds

    # do Speak("Wait until Human1 raises hand asking for help.")
    do Idle() until λ_pre_handRaised()

    # do Speak("Move within 1 meter of Box1 to retrieve it.")
    do MoveTo(λ_target_box(), False)

    # do Speak("Pick up the nearest box on the floor.")
    do PickUp()

    print("coach here0")
    # do Speak("Wait until Box1 is possessed by me.")
    do Idle() until λ_pre_possessed()

    print("coach here1")

    # do Speak("Move within 2 meters of Human1 to deliver.")
    do MoveTo(λ_target_human(), True)

    print("coach here2")

    # do Speak("Put the box down in front of Human1.")
    do PutDown()

    print("coach here3")

    # do Speak("Wait until Human1 finishes packaging Box1.")
    do Idle() until λ_pre_packaged()

    print("coach here4")

    do Idle()


behavior Agent1Behavior():
    do Idle() for 3 seconds

    ## Raise hand to request the robot’s help.
    do RaiseHand()

    print("here0")
    ## Wait until robot arrives and the box is placed (near human and not held).
    do Idle() until λ_pre_readyToPackage()

    print("here1")

    ## Package the delivered box.
    do Packaging()

    print("here2")

    do Idle()


# Agents and objects instantiation (use positions from demonstrations)
Human1 = new Player at Uniform((-2.639944, 6.53012657, 0.1975),
                              (-2.67881584, 6.43111324, 0.1975)), with behavior Agent1Behavior(), with name "Human1"

ego = new Robot at Uniform((-1.79286087, -3.74339628, 0.168299451),
                          (-1.92893577, -3.99485588, 0.163965)), with behavior CoachBehavior(), with name "Robot1"

Box1 = new Box at Uniform((-3.202844, -3.6241827, 0.8430421),
                         (-2.8524003, -1.345963, 1.30546224)), with name "Box1"

# Always end the program with a termination condition for the simulator UI
terminate when (ego.gameObject.stopButton)