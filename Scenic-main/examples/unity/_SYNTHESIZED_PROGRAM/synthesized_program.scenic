from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####

# --- Targets (for Robot movement) ---
# Stage stop (after pickup) from both demos
A1target_stage_0 = DistanceTo({'from': 'Robot1', 'to': {'x': -1.98498821, 'y': 10.7174091}, 'min': None, 'max': {'avg': 0.8, 'std': 0.05}, 'operator': 'less_than'})
A1target_stage_1 = DistanceTo({'from': 'Robot1', 'to': {'x': -1.98498869, 'y': 10.3888216}, 'min': None, 'max': {'avg': 0.8, 'std': 0.05}, 'operator': 'less_than'})

# Final drop spot updated to coach-specified location
# CHANGED: both entries now point to (-3.142775, 11.5613346) to tightly focus the drop location.
A1target_drop_0 = DistanceTo({'from': 'Robot1', 'to': {'x': -3.142775, 'y': 11.5613346}, 'min': None, 'max': {'avg': 0.8, 'std': 0.05}, 'operator': 'less_than'})
A1target_drop_1 = DistanceTo({'from': 'Robot1', 'to': {'x': -3.142775, 'y': 11.5613346}, 'min': None, 'max': {'avg': 0.8, 'std': 0.05}, 'operator': 'less_than'})

# NEW: target to move in front of the specific box spawn location provided by coach
A1target_box = DistanceTo({'from': 'Robot1', 'to': {'x': 0.149280369, 'y': 8.603329}, 'min': None, 'max': {'avg': 0.8, 'std': 0.05}, 'operator': 'less_than'})

def λ_target_stage():
    # union of stage positions observed in the demonstrations
    cond = (A1target_stage_0 | A1target_stage_1)
    return cond.dist(simulation(), ego=True)

def λ_target_drop():
    # union (now redundant but preserved) pointing to coach-specified drop location
    cond = (A1target_drop_0 | A1target_drop_1)
    return cond.dist(simulation(), ego=True)

# NEW: move-to target right in front of the specified box location
def λ_target_box():
    cond = A1target_box
    return cond.dist(simulation(), ego=True)

# --- Preconditions / Terminations ---
A1pre_hr = HandRaised({'player': 'Human1'})
A1pre_possessed = IsPossessed({'obj': 'Box1'})
A1pre_packaged = IsPackaged({'obj': 'Box1'})

# human waits until robot is near the (new) drop zone (within ~1 m)
# CHANGED: both guards point to the precise coach-specified drop coordinate.
A1pre_robot_near_drop0 = DistanceTo({'from': 'Robot1', 'to': {'x': -3.142775, 'y': 11.5613346}, 'min': None, 'max': {'avg': 1.0, 'std': 0.1}, 'operator': 'less_than'})
A1pre_robot_near_drop1 = DistanceTo({'from': 'Robot1', 'to': {'x': -3.142775, 'y': 11.5613346}, 'min': None, 'max': {'avg': 1.0, 'std': 0.1}, 'operator': 'less_than'})

def λ_pre_hr():
    return A1pre_hr.bool(simulation())

def λ_pre_possessed():
    return A1pre_possessed.bool(simulation())

def λ_pre_packaged():
    return A1pre_packaged.bool(simulation())

def λ_pre_robot_at_drop():
    cond = (A1pre_robot_near_drop0 | A1pre_robot_near_drop1)
    return cond.bool(simulation())

# Retained (unused after coach’s correction), but harmless.
# New: ensure the box has been put down at the drop spot before packaging.
# We wait for (robot near drop) AND (box not possessed by any agent).
def λ_pre_box_placed_at_drop():
    # NOTE: Using conjunction (&) and negation (~) as allowed by the API composition rules.
    cond = (~A1pre_possessed) & (A1pre_robot_near_drop0 | A1pre_robot_near_drop1)
    return cond.bool(simulation())

# ----------------- BEHAVIORS -----------------

behavior CoachBehavior():
    # (Human is the coached agent)
    do Idle() for 3 seconds

    do Speak("Raise your hand to ask the robot for help.")
    do RaiseHand()

    # CHANGED: Wait exactly for robot proximity (~1 m) to the specified drop area.
    do Speak("Wait until robot is within 1 meter of the drop spot.")
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

    ## CHANGED: Move in front of the specified box location before picking up.
    do MoveTo(λ_target_box())

    ## Pick up the nearest box (requested by the human).
    do PickUp()

    ## Ensure the box is in possession before moving.
    do Idle() until λ_pre_possessed()

    ## CHANGED: Go directly to the precise drop location near the human.
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

# Box start positions
# CHANGED: spawn Box1 at the coach-specified location (x, z, y).
Box1_pos0 = (0.149280369, 8.603329, 0.1975)
Box1_pos1 = (0.149280369, 8.603329, 0.1975)

# Instantiate agents/objects (choose uniformly between demonstrated starts; Box is deterministic here)
ego = new Player at Uniform(Human1_pos0, Human1_pos1), with behavior CoachBehavior(), with name "Human1"
Robot1 = new Robot at Uniform(Robot1_pos0, Robot1_pos1), with behavior Agent1Behavior(), with name "Robot1"
Box1 = new Box at Uniform(Box1_pos0, Box1_pos1), with name "Box1"

# Always end the program with the following statement.
terminate when (ego.gameObject.stopButton)

####Environment Behavior START####