from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    while True:
        do Speak("I am observing the workers to see if they need any parts.")
        do Idle() until λ_precondition_0_1(simulation(), None)
        if λ_precondition0(simulation(), None):
            if λ_precondition_cartB(simulation(), None):
                do Speak("Worker A needs a gray part from the cart, I'll get it.")
                do PickUp(lookup("PartB"))
                do Speak("Here is the part for you, worker A.")
                do PutDown()
            else:
                do Speak("Worker A needs a gray part from the shelf, I'll go get it.")
                do PickUp(Uniform(lookup("PartB (1) at shelf"), lookup("PartB (2) at shelf")))
                do Speak("Here is the gray part from the shelf, worker A.")
                do PutDown()
        elif λ_precondition1(simulation(), None):
            if λ_precondition_cartA(simulation(), None):
                do Speak("Worker B needs a red part from the cart, on it.")
                do PickUp(lookup("PartA"))
                do Speak("Here you go worker B, one red part.")
                do PutDown()
            else:
                do Speak("Worker B needs a red part from the shelf. I am on my way.")
                do PickUp(Uniform(lookup("PartA (1) at shelf"), lookup("PartA (2) at shelf")))
                do Speak("Here is the red part from the shelf, worker B.")
                do PutDown()

A1precondition_0 = HandRaised({'player': 'workerA'})
A1precondition_1 = HandRaised({'player': 'workerB'})
A1precondition_cartA = CloseTo({'obj': 'Coach', 'ref': 'PartA', 'max': {'avg': 3.0, 'std': 0.5}})
A1precondition_cartB = CloseTo({'obj': 'Coach', 'ref': 'PartB', 'max': {'avg': 3.0, 'std': 0.5}})

def λ_target0():
    return

def λ_target1():
    return

def λ_target2():
    return

def λ_target3():
    return

def λ_target4():
    return

def λ_target5():
    return

def λ_target6():
    return

def λ_target7():
    return

def λ_termination0(scene, sample):
    return

def λ_termination1(scene, sample):
    return

def λ_termination2(scene, sample):
    return

def λ_termination3(scene, sample):
    return

def λ_termination4(scene, sample):
    return

def λ_termination5(scene, sample):
    return

def λ_termination6(scene, sample):
    return

def λ_termination7(scene, sample):
    return

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition_0_1(scene, sample):
    return λ_precondition0(scene, sample) or λ_precondition1(scene, sample)

def λ_precondition_cartA(scene, sample):
    return A1precondition_cartA.bool(simulation())

def λ_precondition_cartB(scene, sample):
    return A1precondition_cartB.bool(simulation())






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
    
ego = new Coach at (31.02, -41.45, 0), 
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
    workerA = new Player at (24.75, -38.5, 0), with name "workerA", with behavior workerRequestBehavior()
    workerB = new Player at (30, -39, 0), with name "workerB", with behavior workerPassiveBehavior()

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