from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

# ===========================
# Behavior Functions
# ===========================

behavior CoachBehavior():
    # Always start with an idle of 3 seconds
    do Idle() for 3 seconds

    # Wait for the human signal to begin
    do Speak("Wait until Human1 raises a hand to request help.")
    do Idle() until λ_pre_handRaised()

    # Move next to the box for pickup
    do Speak("Move to pickup spot near (-2.22, 10.77) within 1 meter.")
    do MoveTo(λ_target_pickup())

    # Wait until the box is confirmed in possession
    do Speak("Wait until Box1 is in possession after pickup.")
    do Idle() until λ_pre_possessed()

    # Carry the box to the human’s work area for drop-off
    do Speak("Carry box to drop area near (-2.68, 6.12) within 1 meter.")
    do MoveTo(λ_target_drop())

    # Put the box down on the table
    do Speak("Put the box down on the table for the human to work.")
    do PutDown()

    # Wait for the human to finish packaging
    do Speak("Wait until Box1 is packaged by Human1.")
    do Idle() until λ_pre_packaged()

    # End behavior with idle
    do Idle()


behavior Agent1Behavior():
    # Human (environment agent)
    do Idle() for 3 seconds

    ## Raise hand to request the robot’s help
    do RaiseHand()

    ## Wait until the box is not being held anymore (ready for packaging)
    do Idle() until λ_pre_notPossessed()

    ## Package the nearest box on the table
    do Packaging()

    do Idle()


# ===========================
# Constraint Definitions (outside behaviors)
# ===========================

# Targets for robot movement
A_target_pickup = DistanceTo({
    'from': 'Robot1',
    'to': {'x': -2.2202, 'y': 10.7733},   # pickup waypoint near the box
    'min': None,
    'max': {'avg': 1.0, 'std': 0.2},
    'operator': 'less_than'
})

A_target_drop1 = DistanceTo({
    'from': 'Robot1',
    'to': {'x': -2.6788, 'y': 6.1156},    # drop waypoint (demo_0)
    'min': None,
    'max': {'avg': 1.0, 'std': 0.2},
    'operator': 'less_than'
})

A_target_drop2 = DistanceTo({
    'from': 'Robot1',
    'to': {'x': -2.7700, 'y': 5.7238},    # drop waypoint (demo_1)
    'min': None,
    'max': {'avg': 1.0, 'std': 0.2},
    'operator': 'less_than'
})

# Preconditions
P_handRaised = HandRaised({'player': 'Human1'})
P_possessed  = IsPossessed({'obj': 'Box1'})
P_packaged   = IsPackaged({'obj': 'Box1'})

# ===========================
# λ functions
# ===========================

def λ_target_pickup():
    cond = A_target_pickup
    return cond.dist(simulation(), ego=True)

def λ_target_drop():
    # Allow either of the two demonstrated drop spots
    cond = A_target_drop1 | A_target_drop2
    return cond.dist(simulation(), ego=True)

def λ_pre_handRaised():
    return P_handRaised.bool(simulation())

def λ_pre_possessed():
    return P_possessed.bool(simulation())

def λ_pre_notPossessed():
    return not P_possessed.bool(simulation())

def λ_pre_packaged():
    return P_packaged.bool(simulation())


# ===========================
# Object and Agent Instantiations
# ===========================

# Human placement from demonstrations (x, z, y)
Human1 = new Player at (-2.678816, 7.039619, 0.1975), with behavior Agent1Behavior(), with name "Human1"

# Robot (the coached agent, ego)
ego = new Robot at (3.162263, 10.390024, 0.1975), with behavior CoachBehavior(), with name "Robot1"

# Box on table location (x, z, y)
Box1 = new Box at (-3.398378, 10.931032, 0.9556), with name "Box1"

# Always terminate on stop button
terminate when (ego.gameObject.stopButton)

####Environment Behavior START####