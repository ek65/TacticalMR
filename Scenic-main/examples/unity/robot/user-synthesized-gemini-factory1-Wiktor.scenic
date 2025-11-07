from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    while True:
        do Speak("Alright team, I'm watching. Waiting for a player to get open and call for the ball.")
        do Idle() until λ_precondition_0(simulation(), None)
        if λ_precondition_1(simulation(), None):
            do Speak("I see an opportunity for a quick play. Moving to the part.")
            do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
            do Speak("Got the part! It's like gaining possession of the ball.")
            do PickUp('PartA')
            do Speak("Worker B is open! I'm moving in for the hand-off.")
            do MoveTo(λ_target1()) until λ_termination1(simulation(), None)
            do Speak("Easy pass complete. Great teamwork to get the part there.")
            do PutDown('PartA')
        else:
            do Speak("The part is back on the shelf. I'll have to make a long play.")
            do MoveTo(λ_target2()) until λ_termination2(simulation(), None)
            do Speak("Okay, I have the part. Time to start the attack.")
            do PickUp('PartA at shelf')
            do Speak("I see Worker B is open. Sending a long ball his way.")
            do MoveTo(λ_target3()) until λ_termination3(simulation(), None)
            do Speak("Perfectly placed long pass! He has the part now.")
            do PutDown('PartA at shelf')

A1precondition_0 = HandRaised({'player': 'workerB'})
A1precondition_1 = DistanceTo({'to': 'PartA', 'from': 'PartA (1) at shelf', 'operator': 'greater_than', 'min': {'avg': 2.0, 'std': 0.5}})
A1termination_0 = CloseTo({'obj': 'Coach', 'ref': 'PartA', 'max': {'avg': 1.0, 'std': 0.1}})
A1termination_1 = CloseTo({'obj': 'Coach', 'ref': 'workerB', 'max': {'avg': 1.5, 'std': 0.1}})
A1termination_2 = CloseTo({'obj': 'Coach', 'ref': 'PartA at shelf', 'max': {'avg': 1.0, 'std': 0.1}})
A1termination_3 = CloseTo({'obj': 'Coach', 'ref': 'workerB', 'max': {'avg': 1.5, 'std': 0.1}})

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_target0():
    return 'PartA'

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_target1():
    return 'workerB'

def λ_termination1(scene, sample):
    return A1termination_1.bool(simulation())

def λ_target2():
    return 'PartA at shelf'

def λ_termination2(scene, sample):
    return A1termination_2.bool(simulation())

def λ_target3():
    return 'workerB'

def λ_termination3(scene, sample):
    return A1termination_3.bool(simulation())






# Human behaviors
behavior workerRequestBehavior():
    do Idle() for 2 seconds
    take PackagingAction()
    do Idle() for 4 seconds
    take RaiseHandAction()  # Signals out of parts
    do Idle() for 1 seconds

behavior workerPassiveBehavior():
    do Idle() for 2 seconds
    take PackagingAction()
    do Idle() for 4 seconds 
    
ego = new Robot at (31.02, -41.45, 0), 
            with name "Coach",
            with behavior CoachBehavior()

# Randomly choose which worker will request parts
requestingWorkerIsA = random.choice([True, False])
print("Requesting worker is A:", requestingWorkerIsA)

# Instantiate workers
if requestingWorkerIsA:
    workerA = new Player at (24.75, -38.5, 0), with name "workerA", with behavior workerRequestBehavior()
    workerB = new Player at (30, -39, 0), with name "workerB", with behavior workerPassiveBehavior()
else:
    workerA = new Player at (24.75, -38.5, 0), with name "workerA", with behavior workerPassiveBehavior()
    workerB = new Player at (30, -39, 0), with name "workerB", with behavior workerRequestBehavior()

# Part availability in bin
numPartA = random.choice([True, False])
numPartB = random.choice([True, False])
print("Part A in bin:", numPartA)
print("Part B in bin:", numPartB)

# Place parts in bin if available
if numPartA:
    partA = new PartA at (27.78, -42.293, 0.266), with name "PartA"

if numPartB:
    partB = new PartB at (28.574, -42.268, 0.255), with name "PartB"