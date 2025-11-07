





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