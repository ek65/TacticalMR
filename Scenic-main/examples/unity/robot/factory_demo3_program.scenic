




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