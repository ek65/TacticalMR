from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    while True:
        do Speak("I am waiting for a worker to signal for a part they need.")
        do Idle() until λ_precondition_0_or_1(simulation(), None)
        if λ_precondition_1(simulation(), None):
            do Speak("Worker B raised their hand. I'll get Part A for them now.")
            do PickUp('PartA at shelf')
            do Speak("I have Part A and am moving to worker B's station.")
            do MoveTo(λ_target_1()) until λ_termination_1(simulation(), None)
            do Speak("Here is Part A for you, worker B.")
            do PutDown('PartA')
        if λ_precondition_0(simulation(), None):
            do Speak("Worker A raised their hand. I'll get Part B for them now.")
            do PickUp('PartB at shelf')
            do Speak("I have Part B and am moving to worker A's station.")
            do MoveTo(λ_target_0()) until λ_termination_0(simulation(), None)
            do Speak("Here is Part B for you, worker A.")
            do PutDown('PartB')

A1precondition_0 = HandRaised({'player': 'workerA'})
A1precondition_1 = HandRaised({'player': 'workerB'})
A1termination_0 = CloseTo({'obj': 'Coach', 'ref': 'workerA'})
A1termination_1 = CloseTo({'obj': 'Coach', 'ref': 'workerB'})

def λ_target_0():
    return workerA

def λ_target_1():
    return workerB

def λ_termination_0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_termination_1(scene, sample):
    return A1termination_1.bool(simulation())

def λ_precondition_0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_precondition_1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_precondition_0_or_1(scene, sample):
    return A1precondition_0.bool(simulation()) or A1precondition_1.bool(simulation())
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