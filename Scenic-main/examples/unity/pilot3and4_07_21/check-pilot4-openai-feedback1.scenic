from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Check diagonally to get into a better passing lane.")
    # Move diagonally with respect to opponent to create better angle for playing
    do MoveTo(λ_target_check())
    do Speak("Wait until teammate passes to you after you check.")
    do Idle() until λ_precondition_teampass(simulation(), None)
    do Speak("Go to the ball and get possession after teammate's pass.")
    do MoveToBallAndGetPossession()
    # FEEDBACK: Removed the redundant 'Speak' and 'Idle until HasBallPossession' step.
    do Speak("Decide: pass back to teammate or dribble depending on opponent.")
    if λ_precondition_opponent_in_front(simulation(), None):
        do Speak("Pass to teammate who is now in good position.")
        do Pass(teammate)
    else:
        do Speak("Move close to goal to create shooting opportunity.")
        do MoveTo(λ_target_goal())
        do Speak("Wait until you are close to goal and can shoot.")
        do Idle() until λ_precondition_can_shoot(simulation(), None)
        do Speak("Take a shot at the goal.")
        do Shoot(goal)
    do Idle()

# Constraints and their instantiations

# Move diagonally from current position to create a better passing lane;
# Assume diagonal check puts Coach 4.5m from opponent, and angle ~35deg to ball/opponent axis
A1target_check_dist = DistanceTo({
    'from': 'Coach', 'to': 'opponent',
    'min': {'avg': 4.5, 'std': 0.5},
    'max': None,
    'operator': 'greater_than'
})
A1target_check_angle = AtAngle({
    'player': 'Coach', 'ball': 'opponent',
    'left': {'theta': {'avg': 35.0, 'std': 5.0},
             'dist': {'avg': 4.5, 'std': 0.7}},
    # FEEDBACK: Added a 'right' parameter to allow for diagonal checking to either side.
    # The coach's feedback indicated the movement was too horizontal and that the
    # coach should move more at an angle, pointing to both left and right options.
    'right': {'theta': {'avg': 35.0, 'std': 5.0},
              'dist': {'avg': 4.5, 'std': 0.7}}
})

def λ_target_check():
    cond = A1target_check_dist and A1target_check_angle
    return cond.dist(simulation(), ego=True)

# Wait until MakePass(teammate) indicates a pass has been made to Coach
A1precondition_teampass = MakePass({'player': 'teammate'})

def λ_precondition_teampass(scene, sample):
    return A1precondition_teampass.bool(simulation())

# Wait until Coach has possession (after pass)
A1precondition_havepos = HasBallPossession({'player': 'Coach'})

def λ_precondition_havepos(scene, sample):
    return A1precondition_havepos.bool(simulation())

# If there is no clear path to goal (opponent is blocking), Coach will pass
A1precondition_opponent_front = DistanceTo({
    'from': 'Coach', 'to': 'opponent',
    'min': None,
    'max': {'avg': 5.0, 'std': 1.0},
    'operator': 'less_than'
})

def λ_precondition_opponent_in_front(scene, sample):
    return A1precondition_opponent_front.bool(simulation())

# If Coach decides to attack the goal, need to MoveTo within 6m of goal
A1target_goal = DistanceTo({
    'from': 'Coach', 'to': 'goal',
    'min': None,
    'max': {'avg': 6.0, 'std': 0.5},
    'operator': 'less_than'
})

def λ_target_goal():
    return A1target_goal.dist(simulation(), ego=True)

# Wait until possession + within shooting range + clear path to goal
A1precondition_near_goal = DistanceTo({
    'from': 'Coach', 'to': 'goal',
    'min': None,
    'max': {'avg': 6.0, 'std': 0.5},
    'operator': 'less_than'
})
A2precondition_havepos = HasBallPossession({'player': 'Coach'})
A3precondition_clearpath = HasPath({
    'obj1': 'Coach', 'obj2': 'goal',
    'path_width': {'avg': 2.5, 'std': 0.3}
})

def λ_precondition_can_shoot(scene, sample):
    return (
        A1precondition_near_goal.bool(simulation()) and
        A2precondition_havepos.bool(simulation()) and
        A3precondition_clearpath.bool(simulation())
    )
    
# Parameters for variance
coach_start_dist = Uniform(5, 8)  # initial distance from teammate
coach_check_dist = Uniform(4, 6)   # how much closer coach checks
coach_check_angle = Uniform(-45, 45)  # angle of check (degrees)
opponent_dist = Uniform(1, 5)         # distance behind coach
opponent_speed = Uniform(5, 7)        # opponent's movement speed

# Behaviors
behavior TeammatePass():
    do Idle() for 1.0 seconds  # Give coach time to start 
    do MoveToBallAndGetPossession()
    print("got ball")
    do Idle() for 10.0 seconds
    do Pass(ego)
    do Idle()

behavior OpponentFollowCoach():
    do Idle() for 1.0 seconds  # Wait for coach to start checking
    speed = float(opponent_speed)
    do SetPlayerSpeed(speed)
    while True:
        if distance from self to ego > 2.0:
            do MoveToBehavior(ego.position, distance=2.0)
        else:
            do Idle() for 0.1 seconds

# Place teammate (AI) at origin
teammate = new Player at (0, 0, 0), with name "teammate", with team "blue", with behavior TeammatePass()

# Place coach (human) in front of teammate
ego = new Coach ahead of teammate by coach_start_dist, with name "Coach", with team "blue", with behavior CoachBehavior()

# Place opponent ahead of coach (further from goal than coach)
opponent = new Player ahead of ego by opponent_dist, facing toward ego, with name "opponent", with team "red", with behavior OpponentFollowCoach()

# Ball at teammate's feet
ball = new Ball ahead of teammate by 0.5

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)