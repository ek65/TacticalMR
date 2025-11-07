from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("Waiting for a worker to raise their hand for a part.")
    do Idle() until λ_precondition_raise_hand(simulation(), None)
    if λ_precondition_workerB_hand_first(simulation(), None):
        do Speak("Worker B raised hand first, picking up the nearest part for worker B.")
        do PickUp(λ_target_partB()) until λ_termination_pickupB(simulation(), None)
        do Speak("Delivering part to worker B.")
        do MoveTo(workerB) until λ_termination_movetoB(simulation(), None)
        do Speak("Putting down part for worker B.")
        do PutDown(workerB) until λ_termination_putdownB(simulation(), None)
        do Speak("Now picking up next part for worker A.")
        do PickUp(λ_target_partA()) until λ_termination_pickupA(simulation(), None)
        do Speak("Delivering part to worker A.")
        do MoveTo(workerA) until λ_termination_movetoA(simulation(), None)
        do Speak("Putting down part for worker A.")
        do PutDown(workerA) until λ_termination_putdownA(simulation(), None)
    elif λ_precondition_workerA_hand_first(simulation(), None):
        do Speak("Worker A raised hand first, picking up the nearest part for worker A.")
        do PickUp(λ_target_partB()) until λ_termination_pickupB(simulation(), None)
        do Speak("Delivering part to worker A.")
        do MoveTo(workerA) until λ_termination_movetoA(simulation(), None)
        do Speak("Putting down part for worker A.")
        do PutDown(workerA) until λ_termination_putdownA(simulation(), None)
        do Speak("Now picking up next part for worker B.")
        do PickUp(λ_target_partA()) until λ_termination_pickupA(simulation(), None)
        do Speak("Delivering part to worker B.")
        do MoveTo(workerB) until λ_termination_movetoB(simulation(), None)
        do Speak("Putting down part for worker B.")
        do PutDown(workerB) until λ_termination_putdownB(simulation(), None)
    else:
        do Speak("Both workers raised hands at the same time, give part to worker B first as closer.")
        do PickUp(λ_target_partA()) until λ_termination_pickupA(simulation(), None)
        do Speak("Delivering part to worker B.")
        do MoveTo(workerB) until λ_termination_movetoB(simulation(), None)
        do Speak("Putting down part for worker B.")
        do PutDown(workerB) until λ_termination_putdownB(simulation(), None)
        do Speak("Now picking up and delivering part to worker A second.")
        do PickUp(λ_target_partB()) until λ_termination_pickupB(simulation(), None)
        do MoveTo(workerA) until λ_termination_movetoA(simulation(), None)
        do PutDown(workerA) until λ_termination_putdownA(simulation(), None)

A1precondition_raise_hand = HandRaised({'player': 'workerA'})
A2precondition_raise_hand = HandRaised({'player': 'workerB'})

def λ_precondition_raise_hand(scene, sample):
    return A1precondition_raise_hand.bool(simulation()) or A2precondition_raise_hand.bool(simulation())

A1precondition_workerA_hand_first = HandRaised({'player': 'workerA'})
A2precondition_workerA_hand_first = HandRaised({'player': 'workerA'})
A_handB_first = HandRaised({'player': 'workerB'})
A_handA_first = HandRaised({'player': 'workerA'})

def λ_precondition_workerB_hand_first(scene, sample):
    # Worker B raises hand before Worker A
    return (A_handB_first.bool(simulation()) and not A_handA_first.bool(simulation()))

def λ_precondition_workerA_hand_first(scene, sample):
    # Worker A raises hand before Worker B
    return (A_handA_first.bool(simulation()) and not A_handB_first.bool(simulation()))

def λ_target_partA():
    # Always picks up PartA from shelf if available, else PartA
    return "PartA at shelf"

def λ_target_partB():
    # Always picks up PartB from shelf if available, else PartB
    return "PartB at shelf"

def λ_termination_pickupA(scene, sample):
    # Coach has picked up the PartA object
    # PartA is no longer on the shelf, in Coach's possession
    return True

def λ_termination_pickupB(scene, sample):
    # Coach has picked up the PartB object
    # PartB not at shelf, now with Coach
    return True

def λ_termination_movetoA(scene, sample):
    # Arrived at workerA
    return CloseTo({'obj': 'Coach', 'ref': 'workerA', 'max': 1.0}).bool(simulation())

def λ_termination_movetoB(scene, sample):
    # Arrived at workerB
    return CloseTo({'obj': 'Coach', 'ref': 'workerB', 'max': 1.0}).bool(simulation())

def λ_termination_putdownA(scene, sample):
    # Part delivered to workerA
    return True

def λ_termination_putdownB(scene, sample):
    # Part delivered to workerB
    return True
# Human behaviors

behavior raiseHandLater():
    do Idle() for 2 seconds
    take PackagingAction()
    do Idle() for 5 seconds
    take RaiseHandAction()  # signals out of parts
    do Idle() for 1 seconds

behavior raiseHandNow():
    do Idle() for 2 seconds
    take PackagingAction()
    do Idle() for 3 seconds
    take RaiseHandAction() 
    do Idle() for 1 seconds

# ROBOT BEHAVIOR

# behavior robotBehavior():
#     wait for hand raise from any worker

#     if not simultaneousRaise:
#         # Case 1: One human raises hand first, then the other
#         if workerA.handIsRaised and not workerB.handIsRaised:
#             do MoveTo(partA)
#             take PickUpAction()
#             do MoveTo(workerA)
#             take PutDownAction()
#         elif workerB.handIsRaised and not workerA.handIsRaised:
#             do MoveTo(partB)
#             take PickUpAction()
#             do MoveTo(workerB)
#             take PutDownAction()
#     else:
#         # Case 2: Both raise hand at same time -> choose closest
#         distA = distance from ego to workerA
#         distB = distance from ego to workerB

#         if distA < distB:
#             do MoveTo(partA)
#             take PickUpAction()
#             do MoveTo(workerA)
#             take PutDownAction()
#         else:
#             do MoveTo(partB)
#             take PickUpAction()
#             do MoveTo(workerB)
#             take PutDownAction()

# SCENE CONFIGURATION

# Parts bin always has 1 of each part
partA = new PartA at (27.78, -42.293, 0.266), with name "PartA"
partB = new PartB at (28.574, -42.268, 0.255), with name "PartB"

workerAPosition = (24.75, -38.5, 0)
workerBPosition = (30, -39, 0)

# Randomly choose whether both raise hands at same time or staggered
simultaneousRaise = random.choice([True, False])
print("Simultaneous raise:", simultaneousRaise)

# Assign behaviors at initialization based on scenario
if simultaneousRaise:
    workerA = new Player at workerAPosition, facing ego, with name "workerA", with behavior raiseHandNow()
    workerB = new Player at workerBPosition, facing ego, with name "workerB", with behavior raiseHandNow()
else:
    firstRaiserIsA = random.choice([True, False])
    if firstRaiserIsA:
        workerA = new Player at workerAPosition, facing ego, with name "workerA", with behavior raiseHandNow()
        workerB = new Player at workerBPosition, facing ego, with name "workerB", with behavior raiseHandLater()
    else:
        workerA = new Player at workerAPosition, facing ego, with name "workerA", with behavior raiseHandLater()
        workerB = new Player at workerBPosition, facing ego, with name "workerB", with behavior raiseHandNow()
               
ego = new Robot at (31.02, -41.45, 0), 
            with name "Coach",
            with behavior CoachBehavior()