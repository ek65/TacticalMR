from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Speak("I will wait at my initial location.")
    do Idle() for 3 seconds
    do Speak("Move to replace the worker who has left their station.")
    do MoveTo(λ_target0()) until λ_termination0(simulation(), None)
    do Speak("Pick up the items needed for packaging at the station.")
    do PickUp(λ_target1())
    do Speak("Begin packaging at the worker's station until they return.")
    do Packaging(λ_target2()) until λ_termination1(simulation(), None)
    do Speak("The worker has returned. I will leave the station now.")
    do Drop(λ_target3())
    do Speak("I will return to my initial waiting position.")
    do MoveTo(λ_target4()) until λ_termination2(simulation(), None)
    do Speak("Idle after completing substitution.")
    do Idle() for 2 seconds

A1target_0 = InZone({'player': 'Coach', 'zone': λ_zone_left_worker()})
A1termination_0 = InZone({'player': 'Coach', 'zone': λ_zone_left_worker()})

A1target_1 = λ_partA_at_station()
A1target_2 = λ_pack_station_of_left_worker()
A1termination_1 = λ_worker_returned(simulation())

A1target_3 = λ_pack_station_of_left_worker()
A1target_4 = InZone({'player': 'Coach', 'zone': λ_zone_initial_wait()})
A1termination_2 = InZone({'player': 'Coach', 'zone': λ_zone_initial_wait()})

def λ_target0():
    # Move to the station of the worker who left.
    return A1target_0

def λ_termination0(scene, sample):
    # Arrived at the left worker's station.
    return A1termination_0

def λ_target1():
    # Target to pick up parts at the left worker's station.
    return A1target_1

def λ_target2():
    # The workstation location for packaging.
    return A1target_2

def λ_termination1(scene, sample):
    # Worker has returned to the station.
    return A1termination_1

def λ_target3():
    # The pack station to drop items after worker returns.
    return A1target_3

def λ_target4():
    # Return to the original waiting zone.
    return A1target_4

def λ_termination2(scene, sample):
    # Arrived at initial waiting location.
    return A1termination_2

def λ_zone_left_worker():
    # Determine which worker left and return their zone.
    # (Pseudocode: choose the correct worker's zone, e.g., 'B2')
    return 'B2'

def λ_zone_initial_wait():
    # Zone where coach waits initially (pseudocode, e.g., 'A3')
    return 'A3'

def λ_partA_at_station():
    # Find PartA at the worker's station who left (e.g., 'PartA (1) at shelf')
    return 'PartA at shelf'

def λ_pack_station_of_left_worker():
    # The target pack station for packaging (reuse left worker's station location, e.g., 'B2')
    return 'B2'

def λ_worker_returned(scene):
    # Check if the left worker has returned to their station.
    # InZone returns True if returned.
    return InZone('worker2', λ_zone_left_worker())





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