from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Wait for a worker to raise their hand.")
    do Idle() until λ_precondition_raisedHand(simulation(), None)
    do Speak("Pick up PartA to bring to workerB.")
    do MoveTo(partA) until λ_termination_PickUp(simulation(), None)
    do Idle() for 2 seconds
    #do PickUp(λ_target_PartA()) until λ_termination_PickUp(simulation(), None)
    do PickUp()
    print('picked up')
    do Speak("Deliver PartA and put it down for workerB.")
    do MoveTo(λ_target_workerB()) until λ_termination_MoveTo(simulation(), None)
    do Speak("Put down PartA for workerB.")
    do Idle() for 2 seconds
    #do PutDown(λ_putdown_PartA()) until λ_termination_PutDown(simulation(), None)
    do PutDown()
    print('put down')

A1precondition_raisedHand = HandRaised({'player': 'workerB'})
A1target_PartA = CloseTo({'obj': 'Coach', 'ref': 'partA', 'max': {'avg': 1.0, 'std': 0.01}})
A1termination_PickUp = CloseTo({'obj': 'Coach', 'ref': 'partA', 'max': {'avg': 1.2, 'std': 0.01}})
A1target_workerB = CloseTo({'obj': 'Coach', 'ref': 'workerB', 'max': {'avg': 1.2, 'std': 0.03}})
A1termination_MoveTo = CloseTo({'obj': 'Coach', 'ref': 'workerB', 'max': {'avg': 1.2, 'std': 0.03}})
A1putdown_PartA = CloseTo({'obj': 'Coach', 'ref': 'workerB', 'max': {'avg': 1.2, 'std': 0.03}})
A1termination_PutDown = CloseTo({'obj': 'partA', 'ref': 'workerB', 'max': {'avg': 1.2, 'std': 0.03}})

def λ_precondition_raisedHand(scene, sample):
    return A1precondition_raisedHand.bool(simulation())

def λ_target_PartA():
    return A1target_PartA.dist(simulation(), ego=True)

def λ_termination_PickUp(scene, sample):
    return A1termination_PickUp.bool(simulation())

def λ_target_workerB():
    return A1target_workerB.dist(simulation(), ego=True)

def λ_termination_MoveTo(scene, sample):
    return A1termination_MoveTo.bool(simulation())

def λ_putdown_PartA():
    return A1putdown_PartA.dist(simulation(), ego=True)

def λ_termination_PutDown(scene, sample):
    return A1termination_PutDown.bool(simulation())






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
    workerA = new Player at (24.75, -38.5, 0), with name "workerA", with behavior workerPassiveBehavior()
    workerB = new Player at (30, -39, 0), with name "workerB", with behavior workerRequestBehavior()
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
    
partA_shelf = new PartA at (21.8, -30.05, 0), with name "PartA at shelf"
partB_shelf = new PartB at (26.53035, -30.03, 0), with name "PartB at shelf"