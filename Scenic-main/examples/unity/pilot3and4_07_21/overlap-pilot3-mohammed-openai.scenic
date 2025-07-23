from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random
####HEADER ENDS####


A1_overlap = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 32.0, 'std': 6.0},
    'dist': {'avg': 9.0, 'std': 1.0}
})

A1_haspass_teammate_to_coach = MakePass({'player': 'teammate'})
A2_coach_gets_possession = HasBallPossession({'player': 'Coach'})
A3_opponent_presses_coach = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A4_coach_close_to_goal = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 6.0, 'std': 0.8}, 'operator': 'less_than'})
A5_coach_has_path_to_goal = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.3, 'std': 0.3}})
A6_teammate_clear_path = HasPath({'obj1': 'Coach', 'obj2': 'teammate', 'path_width': {'avg': 2.0, 'std': 0.2}})
A7_pass_coach_to_teammate = MakePass({'player': 'Coach'})
A8_teammate_gets_back_possession = HasBallPossession({'player': 'teammate'})

def λ_target_overlap():
    return A1_overlap.dist(simulation(), ego=True)

def λ_precondition_teammate_passed(scene, sample):
    return A1_haspass_teammate_to_coach.bool(simulation())

def λ_precondition_coach_gets_ball(scene, sample):
    return A2_coach_gets_possession.bool(simulation())

def λ_termination_overlap(scene, sample):
    # Terminate when Coach makes the run to overlap/beyond the teammate (approx beyond threshold), 
    # as an intermediate: i.e., on loss of clear overlap angle/opponent behavior change.
    # But we use a frame count or opponent changes direction (not directly the overlap goal)
    return A1_haspass_teammate_to_coach.bool(simulation())  # e.g., teammate initiates the pass

def λ_precondition_pass_arrives(scene, sample):
    return A2_coach_gets_possession.bool(simulation())

def λ_precondition_shot(scene, sample):
    # When Coach is close enough to goal, has ball, and has a clear path to shoot
    return (A4_coach_close_to_goal.bool(simulation()) and
            A2_coach_gets_possession.bool(simulation()) and
            A5_coach_has_path_to_goal.bool(simulation()))

def λ_precondition_under_pressure(scene, sample):
    # When opponent is close enough to pressure Coach as Coach gets possession
    return A3_opponent_presses_coach.bool(simulation())

def λ_precondition_safe_pass(scene, sample):
    return A6_teammate_clear_path.bool(simulation())

def λ_precondition_pass_given(scene, sample):
    return A7_pass_coach_to_teammate.bool(simulation())

def λ_precondition_teammate_got_pass(scene, sample):
    return A8_teammate_gets_back_possession.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Move beyond your teammate to overlap and create an attacking option.")
    do MoveTo(λ_target_overlap())
    do Speak("Wait until your teammate recognizes your run and passes.")
    do Idle() until λ_precondition_teammate_passed(simulation(), None)
    do Speak("Time to stop and receive the ball from your teammate.")
    do StopAndReceiveBall()
    do Speak("Wait until you get possession of the ball.")
    do Idle() until λ_precondition_coach_gets_ball(simulation(), None)
    if λ_precondition_under_pressure(simulation(), None):
        do Speak("If under pressure, quickly decide to shoot or pass.")
        if λ_precondition_shot(simulation(), None):
            do Speak("There's space, drive at goal and take a shot!")
            do MoveTo(λ_target_overlap())   # Continue running toward goal as a burst forward
            do Idle() until λ_precondition_shot(simulation(), None)
            do Speak("Go for the goal, take your shot!")
            do Shoot(goal)
        else:
            do Speak("Defender too close, look to pass back to teammate.")
            do Idle() until λ_precondition_safe_pass(simulation(), None)
            do Pass(teammate)
            do Speak("Wait for your teammate to take the next action.")
            do Idle() until λ_precondition_teammate_got_pass(simulation(), None)
            do Idle()
    else:
        do Speak("With space available, drive forward and shoot at goal.")
        do MoveTo(λ_target_overlap())
        do Idle() until λ_precondition_shot(simulation(), None)
        do Speak("Now go ahead and shoot at goal.")
        do Shoot(goal)
        do Idle()
    do Idle()



####Environment Behavior START####
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
    do MoveToBallAndGetPossession()
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