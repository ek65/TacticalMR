from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

### Modeling Physical Conditions using APIs from the provided API library

# --- Targets (for Robot movement) ---
# Stage stop (after pickup) from both demos
A1target_stage_0 = DistanceTo({'from': 'Robot1', 'to': {'x': -1.98498821, 'y': 10.7174091}, 'min': None, 'max': {'avg': 0.8, 'std': 0.05}, 'operator': 'less_than'})
A1target_stage_1 = DistanceTo({'from': 'Robot1', 'to': {'x': -1.98498869, 'y': 10.3888216}, 'min': None, 'max': {'avg': 0.8, 'std': 0.05}, 'operator': 'less_than'})

# Final drop spots from both demos
A1target_drop_0 = DistanceTo({'from': 'Robot1', 'to': {'x': -3.18075943, 'y': 11.3294754}, 'min': None, 'max': {'avg': 0.8, 'std': 0.05}, 'operator': 'less_than'})
A1target_drop_1 = DistanceTo({'from': 'Robot1', 'to': {'x': -3.2443428,  'y': 11.01892},   'min': None, 'max': {'avg': 0.8, 'std': 0.05}, 'operator': 'less_than'})

def λ_target_stage():
    # union of stage positions observed in the demonstrations
    cond = (A1target_stage_0 | A1target_stage_1)
    return cond.dist(simulation(), ego=True)

def λ_target_drop():
    # union of drop positions observed in the demonstrations
    cond = (A1target_drop_0 | A1target_drop_1)
    return cond.dist(simulation(), ego=True)

# --- Preconditions / Terminations ---
A1pre_hr = HandRaised({'player': 'Human1'})
A1pre_possessed = IsPossessed({'obj': 'Box1'})
A1pre_packaged = IsPackaged({'obj': 'Box1'})

# human waits until robot is near either drop zone (within ~1 m)
A1pre_robot_near_drop0 = DistanceTo({'from': 'Robot1', 'to': {'x': -3.18075943, 'y': 11.3294754}, 'min': None, 'max': {'avg': 1.0, 'std': 0.1}, 'operator': 'less_than'})
A1pre_robot_near_drop1 = DistanceTo({'from': 'Robot1', 'to': {'x': -3.2443428,  'y': 11.01892},   'min': None, 'max': {'avg': 1.0, 'std': 0.1}, 'operator': 'less_than'})

def λ_pre_hr():
    return A1pre_hr.bool(simulation())

def λ_pre_possessed():
    return A1pre_possessed.bool(simulation())

def λ_pre_packaged():
    return A1pre_packaged.bool(simulation())

def λ_pre_robot_at_drop():
    cond = (A1pre_robot_near_drop0 | A1pre_robot_near_drop1)
    return cond.bool(simulation())

# ----------------- BEHAVIORS -----------------

behavior CoachBehavior():
    # (Human is the coached agent)
    do Idle() for 3 seconds

    do Speak("Raise your hand to ask the robot for help.")
    do RaiseHand()

    do Speak("Wait until the robot is within about 1 meter of the drop area.")
    do Idle() until λ_pre_robot_at_drop()

    do Speak("Now package the delivered box.")
    do Packaging()

    do Speak("Wait until the box is fully packaged.")
    do Idle() until λ_pre_packaged()

    do Idle()

behavior Agent1Behavior():
    # (Robot behavior in the environment)
    do Idle() for 3 seconds

    ## Wait for the human to raise a hand requesting help.
    do Idle() until λ_pre_hr()

    ## Pick up the nearest box (requested by the human).
    do PickUp()

    ## Ensure the box is in possession before moving.
    do Idle() until λ_pre_possessed()

    ## Move to the observed staging position before the final drop.
    do MoveTo(λ_target_stage())

    ## Proceed to the final drop location near the human.
    do Idle() until True
    do MoveTo(λ_target_drop())

    ## Put the box down for the human to package.
    do PutDown()

    do Idle()

# ----------------- SCENE INSTANTIATION -----------------

# Human start positions from the two demonstrations (x, z, y). y is height.
Human1_pos0 = (-1.98498869, 11.3636265, 0)
Human1_pos1 = (-2.04645348, 11.1822281, 0)

# Robot start positions from the two demonstrations
Robot1_pos0 = (-2.86186028, 9.35915, 0)
Robot1_pos1 = (-2.53687024, 6.064379, 0)

# Box start positions from the two demonstrations
Box1_pos0 = (-3.926896, 9.497033, 0)
Box1_pos1 = (-3.84609556, 6.08518553, 0)

# Instantiate agents/objects (choose uniformly between demonstrated starts)
ego = new Player at Uniform(Human1_pos0, Human1_pos1), with behavior CoachBehavior(), with name "Human1"
Robot1 = new Robot at Uniform(Robot1_pos0, Robot1_pos1), with behavior Agent1Behavior(), with name "Robot1"
Box1 = new Box at Uniform(Box1_pos0, Box1_pos1), with name "Box1"

# Always end the program with the following statement.
terminate when (ego.gameObject.stopButton)

####Environment Behavior START####