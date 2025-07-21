from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Make a forward overlapping run beyond teammate to attract defender and create attack option")
    do MoveTo(λ_target_overlap())
    do Speak("Wait until teammate passes ball to you")
    do Idle() until λ_precondition_teammate_passed(simulation(), None)
    do Speak("Stop and receive the incoming ball from teammate")
    do StopAndReceiveBall()
    do Speak("Wait until you have ball possession")
    do Idle() until λ_precondition_has_ball(simulation(), None)
    if λ_precondition_space_for_shot(simulation(), None):
        do Speak("Drive forward into open space and prepare to shoot")
        do MoveTo(λ_target_drive_goal())
        do Speak("Wait until close enough to goal with ball possession")
        do Idle() until λ_precondition_close_to_goal(simulation(), None)
        do Speak("Take a shot at the goal")
        do Shoot(goal)
    else:
        do Speak("Opponent is too close, consider turning back or passing")
        do Pass(teammate)
    do Idle()

# Overlapping run: Move beyond teammate with respect to ball, goal, and opponent. Use average angle 28°, dist 6.0m (from narration and typical overlap positions)
A_overlap = Overlap({'player': 'Coach', 'ball': 'ball', 'goal': 'goal', 'opponent': 'opponent',
                     'theta': {'avg': 28.0, 'std': 4.5}, 'dist': {'avg': 6.0, 'std': 0.7}})

# Teammate makes a pass (Coach is running and waiting for the pass)
A_teammate_passed = MakePass({'player': 'teammate'})

# True if Coach has received or gained possession of the ball
A_has_ball = HasBallPossession({'player': 'Coach'})

# True if there is enough space in front (distance to opponent at least 4.0m)
A_space_for_shot = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 4.0, 'std': 0.5}, 'max': None, 'operator': 'greater_than'})

# Drive forward toward goal, ensure clear path; stop at 9 meters from goal (shot range in demo)
A_drive_goal = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 9.0, 'std': 0.8}, 'operator': 'less_than'})
A_drive_goal_path = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.3, 'std': 0.4}})

# Ready to shoot: ball possession, within 9 meters and with path
A_close_to_goal_poss = HasBallPossession({'player': 'Coach'})
A_close_to_goal_dist = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 9.0, 'std': 0.8}, 'operator': 'less_than'})
A_close_to_goal_path = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.3, 'std': 0.4}})

def λ_target_overlap():
    return A_overlap.dist(simulation(), ego=True)

def λ_precondition_teammate_passed(scene, sample):
    return A_teammate_passed.bool(simulation())

def λ_precondition_has_ball(scene, sample):
    return A_has_ball.bool(simulation())

def λ_precondition_space_for_shot(scene, sample):
    return A_space_for_shot.bool(simulation())

def λ_target_drive_goal():
    cond = A_drive_goal and A_drive_goal_path
    return cond.dist(simulation(), ego=True)

def λ_precondition_close_to_goal(scene, sample):
    return (A_close_to_goal_poss.bool(simulation())
            and A_close_to_goal_dist.bool(simulation())
            and A_close_to_goal_path.bool(simulation()))




opponent_y_distance = Uniform(3, 5)
opponent_x_distance = Uniform(-2, 2)
ego_x_distance = Uniform(-2, 2)
ego_y_distance = Uniform(-1, -2)

# Ensure teammate and opponent are on the same side
#require (opponent_x_distance < 0 and ego_x_distance < 0) or (opponent_x_distance >= 0 and ego_x_distance >= 0)

behavior Follow(obj):
    while ego.position.y > 1:
        do MoveToBehavior(obj, distance = 2, status = f"Follow {obj.name}")

behavior TeammateBehavior():
    do Idle() for 1 seconds
    do MoveToBallAndGetPossession(ball)
    do Idle() for 10 seconds
    do Idle() until ego.position.y > 2
    do Pass(ego, slow=False) until (distance from opponent to ego) <= 3
    do DribbleTo(goal) until (distance from opponent to ego) > 3
    do Idle() for 2 seconds
    

behavior DefenderBehavior():
    do Idle() for 1 seconds
    do Idle() until ego.position.y > 1
    while True:
        if distance from self to ego > 2.0:
            do MoveToBehavior(ego.position, distance=2.0)
        else:
            do Idle() for 0.1 seconds   
    

teammate = new Player at (0, 0, 0),
      with behavior TeammateBehavior(), with name "teammate", with team "blue"

ball = new Ball ahead of teammate by 1

ego = new Coach at (ego_x_distance, ego_y_distance, 0), with name "Coach", with team "blue", with behavior CoachBehavior()

opponent = new Player at (0, Uniform(4, 6), 0), with name "opponent",
            with behavior DefenderBehavior(), with team "red"

goal = new Goal at (0, 17, 0)
terminate when (ego.gameObject.stopButton)