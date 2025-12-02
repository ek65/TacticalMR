from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

# ===== Constraint and Target Definitions for Coach (Robot) =====

# Targets for the robot (ego) to approach Box1 and Human1
A_R_target_box = DistanceTo({'from': 'Robot1', 'to': 'Box1',
                             'min': None, 'max': {'avg': 1.0, 'std': 0.1},
                             'operator': 'less_than'})
A_R_target_human = DistanceTo({'from': 'Robot1', 'to': 'Human1',
                               'min': None, 'max': {'avg': 1.2, 'std': 0.1},
                               'operator': 'less_than'})

# Preconditions used inside the robot behavior
A_R_close_to_box = DistanceTo({'from': 'Robot1', 'to': 'Box1',
                               'min': None, 'max': {'avg': 1.0, 'std': 0.1},
                               'operator': 'less_than'})
A_R_close_to_human = DistanceTo({'from': 'Robot1', 'to': 'Human1',
                                 'min': None, 'max': {'avg': 1.2, 'std': 0.1},
                                 'operator': 'less_than'})
A_R_possessed = IsPossessed({'obj': 'Box1'})
A_R_packaged = IsPackaged({'obj': 'Box1'})

def λ_target_R_box():
    cond = A_R_target_box
    return cond.dist(simulation(), ego=True)

def λ_target_R_human():
    cond = A_R_target_human
    return cond.dist(simulation(), ego=True)

def λ_precondition_R_closeBox():
    return A_R_close_to_box.bool(simulation())

def λ_precondition_R_closeHuman():
    return A_R_close_to_human.bool(simulation())

def λ_precondition_R_possessed():
    return A_R_possessed.bool(simulation())

def λ_precondition_R_notPossessed():
    return (not A_R_possessed.bool(simulation()))

def λ_precondition_R_packaged():
    return A_R_packaged.bool(simulation())


# ===== Constraint and Target Definitions for Human (Environment Agent) =====

B_H_box_near_human = DistanceTo({'from': 'Human1', 'to': 'Box1',
                                 'min': None, 'max': {'avg': 1.5, 'std': 0.1},
                                 'operator': 'less_than'})
B_H_packaged = IsPackaged({'obj': 'Box1'})

def λ_precondition_H_boxNear():
    return B_H_box_near_human.bool(simulation())

def λ_precondition_H_packaged():
    return B_H_packaged.bool(simulation())


# ========================= Behaviors =========================

behavior CoachBehavior():
    do Idle() for 3 seconds

    do Speak("Move next to the box within 1 meter.")
    do MoveTo(λ_target_R_box())

    do Speak("Wait until close enough to grab the box.")
    do Idle() until λ_precondition_R_closeBox()

    do Speak("Pick up the box now.")
    do PickUp()

    do Speak("Wait until you are holding the box.")
    do Idle() until λ_precondition_R_possessed()

    do Speak("Move next to the human within 1 meter.")
    do MoveTo(λ_target_R_human())

    do Speak("Wait until you are beside the human.")
    do Idle() until λ_precondition_R_closeHuman()

    do Speak("Put the box down for the human.")
    do PutDown()

    do Speak("Wait until the box is no longer in your hands.")
    do Idle() until λ_precondition_R_notPossessed()

    do Speak("Wait until the human packages the box.")
    do Idle() until λ_precondition_R_packaged()

    do Idle()


behavior Agent1Behavior():
    do Idle() for 3 seconds

    ## CHANGED: Wait until the box is near the human (~1.5 m) AND not possessed (i.e., placed on table).
    ## This enforces: packaging starts only after the robot puts the box down.
    do Idle() until λ_precondition_H_boxNear() and λ_precondition_R_notPossessed()

    ## Package the nearest box placed on the table.
    do Packaging()

    ## Ensure the box has been packaged before finishing.
    do Idle() until λ_precondition_H_packaged()

    do Idle()


# ========================= Object Instantiation =========================

# Note: coordinates are given as (x, z, y)

Human = new Player at Uniform(
            (-2.4219, 5.5018, 0.2),
            (-2.6399, 5.5063, 0.2)
        ), with behavior Agent1Behavior(), with name "Human1"

ego = new Robot at Uniform(
            (2.3399, 10.0044, 0.2),
            (1.0008, -1.1785, 0.2)
        ), with behavior CoachBehavior(), with name "Robot1"

Box1 = new Box at Uniform(
            (0.7148, 2.3064, 0.2),
            (-3.2955, 10.6929, 0.2)
        ), with name "Box1"

terminate when ego.gameObject.stopButton

####Environment Behavior START####
####Environment Behavior START####
####Environment Behavior START####