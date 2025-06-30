from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import random

# SCENARIO:
# there are two humans working with different parts -
# one is working with part A and the other is working with part B
# one of the humans will raise their hand if they run out of parts
# the robot will go to the bin of parts to look for the part
#       (the bin will have a 0 or 1 of each part)
# if there are no parts that the human asked for in the bin,
#       robot goes to a shelf to pick up parts
# otherwise robot will pick up the part from the bin and bring it to the human

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

# Robot behavior
# behavior robotBehavior():
    # # wait for hand raise
    # do Idle() until worker1.handRaised
    # if requestingWorkerIsA:
    #     if numPartA > 0:
    #         do MoveTo(Vector(10.67, -65, 0))  # bin location
    #         take PickUpAction()
    #     else:
    #         do MoveTo(Vector(5, -65, 0))  # shelf location
    #         take PickUpAction()
    #     do MoveTo(workerA)
    #     take PutDownAction()
    # else:
    #     if numPartB > 0:
    #         do MoveTo(Vector(10.67, -67, 0))  # bin location
    #         take PickUpAction()
    #     else:
    #         do MoveTo(Vector(5, -67, 0))  # shelf location
    #         take PickUpAction()
    #     do MoveTo(workerB)
    #     take PutDownAction()

# Instantiate robot
ego = new RobotCoach at (31.02, -41.45, 0), with name "Coach"

# Randomly choose which worker will request parts
# requestingWorkerIsA = random.choice([True, False])
requestingWorkerIsA = False
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

partA_shelf = new PartA at (21.8, -30.05, 0), with name "PartA at shelf"
partB_shelf = new PartB at (26.53035, -30.03, 0), with name "PartB at shelf"