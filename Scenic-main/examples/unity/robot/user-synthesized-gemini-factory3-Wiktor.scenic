from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("I will wait until a worker leaves their station to provide assistance.")
    try:
        do Idle()
    interrupt when λ_precondition0(simulation(), None):
        do Speak("Worker 1 has left their station. I will move to take their place.")
        do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
        do Speak("I will handle the packaging until worker 1 gets back.")
        do Packaging() until λ_precondition1(simulation(), None)
        do Speak("Worker 1 is back. I will now leave the station.")
        do MoveTo(λ_target1()) until λ_termination1(simulation(), None)
    interrupt when λ_precondition2(simulation(), None):
        do Speak("Worker 2 has left their station. I am moving to cover for them.")
        do MoveTo(λ_target2()) until λ_termination2(simulation(), None)
        do Speak("I will handle the packaging until worker 2 gets back.")
        do Packaging() until λ_precondition3(simulation(), None)
        do Speak("Worker 2 is back. I will now leave the station.")
        do MoveTo(λ_target3()) until λ_termination3(simulation(), None)

A1precondition_0 = DistanceTo({'to': 'worker1', 'from': 'worker2', 'operator': 'greater_than', 'min': {'avg': 5.0, 'std': 0.5}})
A1target_0 = CloseTo({'obj': 'Coach', 'ref': 'worker1', 'max': {'avg': 1.5, 'std': 0.2}})
A1termination_0 = CloseTo({'obj': 'Coach', 'ref': 'worker1', 'max': {'avg': 1.5, 'std': 0.2}})
A1precondition_1 = CloseTo({'obj': 'worker1', 'ref': 'worker2', 'max': {'avg': 4.0, 'std': 0.5}})
A1target_1 = DistanceTo({'from': 'Coach', 'to': 'worker1', 'operator': 'greater_than', 'min': {'avg': 5.0, 'std': 0.5}})
A1termination_1 = DistanceTo({'from': 'Coach', 'to': 'worker1', 'operator': 'greater_than', 'min': {'avg': 5.0, 'std': 0.5}})
A1precondition_2 = DistanceTo({'to': 'worker2', 'from': 'worker1', 'operator': 'greater_than', 'min': {'avg': 5.0, 'std': 0.5}})
A1target_2 = CloseTo({'obj': 'Coach', 'ref': 'worker2', 'max': {'avg': 1.5, 'std': 0.2}})
A1termination_2 = CloseTo({'obj': 'Coach', 'ref': 'worker2', 'max': {'avg': 1.5, 'std': 0.2}})
A1precondition_3 = CloseTo({'obj': 'worker2', 'ref': 'worker1', 'max': {'avg': 4.0, 'std': 0.5}})
A1target_3 = DistanceTo({'from': 'Coach', 'to': 'worker2', 'operator': 'greater_than', 'min': {'avg': 5.0, 'std': 0.5}})
A1termination_3 = DistanceTo({'from': 'Coach', 'to': 'worker2', 'operator': 'greater_than', 'min': {'avg': 5.0, 'std': 0.5}})

def λ_precondition0(scene, sample):
    return A1precondition_0.bool(simulation())

def λ_target0():
    return A1target_0.dist(simulation(), ego=True)

def λ_termination0(scene, sample):
    return A1termination_0.bool(simulation())

def λ_precondition1(scene, sample):
    return A1precondition_1.bool(simulation())

def λ_target1():
    return A1target_1.dist(simulation(), ego=True)

def λ_termination1(scene, sample):
    return A1termination_1.bool(simulation())

def λ_precondition2(scene, sample):
    return A1precondition_2.bool(simulation())

def λ_target2():
    return A1target_2.dist(simulation(), ego=True)

def λ_termination2(scene, sample):
    return A1termination_2.bool(simulation())

def λ_precondition3(scene, sample):
    return A1precondition_3.bool(simulation())

def λ_target3():
    return A1target_3.dist(simulation(), ego=True)

def λ_termination3(scene, sample):
    return A1termination_3.bool(simulation())





# Define idle packaging behavior
behavior packagingBehavior():
    do Idle() for 1 seconds
    take PackagingAction()
    do Idle() for 3 seconds

# Define leave-and-return behavior
behavior leaveStationBehavior(station):
    do Idle() for 1 seconds
    take PackagingAction()
    do Idle() for 3 seconds
    do MoveTo(Vector(23.92, -30.14, 0))  # "away" position
    do Idle() for 3 seconds     # away time
    do MoveTo(station)          # return to original station
    do Idle() for 1 seconds
    take PackagingAction()
    do Idle() for 2 seconds

# ROBOT BEHAVIOR
# behavior robotBehavior():
#     # Wait for any worker to leave
#     do Idle() until (distance from worker1.position to station1) > 1 or 
#                     (distance from worker2.position to station2) > 1 or 
#                     (distance from worker3.position to station3) > 1
#     if departingWorker == 1:
#         do MoveTo(station1)
#         take PackagingAction()
#         wait until worker1 at station1
#     elif departingWorker == 2:
#         do MoveTo(station2)
#         take PackagingAction()
#         wait until worker2 at station2
#     else:
#         do MoveTo(station3)
#         take PackagingAction()
#         wait until worker3 at station3

#     # Return to original position
#     do MoveTo(robotStartPos)

# SCENE SETUP

# Define fixed worker stations
station1 = (24.75, -38.5, 0)
station2 = (30, -39, 0)
station3 = (23.92, -41.91, 0)

# Randomly choose which worker leaves
departingWorkerIndex = random.choice([1, 2, 3])
print("Departing worker index:", departingWorkerIndex)

# Instantiate workers with correct behaviors at init
if departingWorkerIndex == 1:
    worker1 = new Player at station1, with name "worker1", with behavior leaveStationBehavior(station1)
    worker2 = new Player at station2, with name "worker2", with behavior packagingBehavior()
    worker3 = new Player at station3, with name "worker3", with behavior packagingBehavior()
elif departingWorkerIndex == 2:
    worker1 = new Player at station1, with name "worker1", with behavior packagingBehavior()
    worker2 = new Player at station2, with name "worker2", with behavior leaveStationBehavior(station2)
    worker3 = new Player at station3, with name "worker3", with behavior packagingBehavior()
else:
    worker1 = new Player at station1, with name "worker1", with behavior packagingBehavior()
    worker2 = new Player at station2, with name "worker2", with behavior packagingBehavior()
    worker3 = new Player at station3, with name "worker3", with behavior leaveStationBehavior(station3)
    
ego = new Robot at (31.02, -41.45, 0), 
            with name "Coach",
            with behavior CoachBehavior()