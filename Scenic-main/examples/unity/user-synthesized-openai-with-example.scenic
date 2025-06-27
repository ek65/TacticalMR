from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    print('pre-speak')
    do Speak("Let us observe the workers for any requests.")
    print('post-speak')
    do Idle() until λ_precondition_raisehand_workerA(simulation(), None) or λ_precondition_raisehand_workerB(simulation(), None)
    print('post Idle')
    if λ_precondition_raisehand_workerA(simulation(), None):
        do Speak("Worker A raised hand. Prepare to deliver PartB or fetch from shelf if needed.")
        if λ_precondition_partb_cart(simulation(), None):
            do Speak("PartB is in the nearby cart. Pick up the gray object for Worker A.")
            do PickUp(λ_target_partb_cart())
            do Speak("Hand the gray object to Worker A.")
            do PutDown(workerA)
        elif λ_precondition_partb2_shelf(simulation(), None):
            do Speak("No gray object in cart. Go to the shelf to get PartB (2).")
            do MoveTo(λ_target_partb2_shelf())
            do PickUp(PartB_2_shelf)
            do Speak("Hand over PartB (2) from shelf to Worker A.")
            do PutDown(workerA)
        else:
            do Speak("Retrieve PartB (1) at shelf for Worker A.")
            do MoveTo(λ_target_partb1_shelf())
            do PickUp(PartB_1_shelf)
            do Speak("Hand over PartB (1) from shelf to Worker A.")
            do PutDown(workerA)
        do Speak("Return to observing for next requests.")
        do Idle()  # Wait for next raise hand event

    if λ_precondition_raisehand_workerB(simulation(), None):
        do Speak("Worker B raised hand. Prepare red object for them.")
        do PickUp(PartA)
        do Speak("Hand the red object to Worker B.")
        do PutDown(workerB)
        do Speak("Return to observing for next requests.")
        do Idle()

def λ_precondition_raisehand_workerA(scene, sample):
    return HandRaised({'player': 'workerA'}).bool(simulation())

def λ_precondition_raisehand_workerB(scene, sample):
    return HandRaised({'player': 'workerB'}).bool(simulation())

def λ_precondition_partb_cart(scene, sample):
    # Check if PartB is present and accessible in cart (as a local convention)
    # PartB not at shelf means available in cart.
    return (not λ_precondition_partb2_shelf(scene, sample)) and (not λ_precondition_partb1_shelf(scene, sample))

def λ_precondition_partb2_shelf(scene, sample):
    # If PartB (2) at shelf exists and is not yet picked up
    return True  # Provide real check if state available

def λ_precondition_partb1_shelf(scene, sample):
    # If PartB (1) at shelf exists and is not yet picked up
    return True  # Provide real check if state available

def λ_target_partb_cart():
    return PartB  # Naming as present in the cart

def λ_target_partb2_shelf():
    return PartB_2_shelf

def λ_target_partb1_shelf():
    return PartB_1_shelf

def λ_termination_dummy(scene, sample):
    return True  # Placeholder if termination condition is needed







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