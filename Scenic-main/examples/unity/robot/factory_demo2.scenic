from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import random

# SCENARIO:
# the robot is spawned in a random location
# there are two humans working with different parts -
# one is working with part A and the other is working with part B
# two cases can happen:
# 1. one of the humans will raise their hand if they run out of parts,
#       then soon after the other human will raise their hand
# 2. both humans will raise their hand at the same time
# in case 1:
#      the robot should perform the request for the first human that raised their hand, then the second human
# in case 2:
#      the robot should perform the request for the human that is closest to it first, then the second human
# the robot will go to the bin of parts to look for the part
#       (the bin will always have 1 of each part)
# robot will pick up the part from the bin and bring it to the human

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

# Robot spawns at a random location within a defined region
ego = new RobotCoach at (Range(24.06, 31.77), Range(-41.45, -40.45), 0), with name "Coach"

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