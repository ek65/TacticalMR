from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

A1_overlap = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'opponent',
    'theta': {'avg': 52.0, 'std': 4.0},
    'dist': {'avg': 7.0, 'std': 0.7}
})

A2_pass_to_coach = MakePass({'player': 'teammate'})
A3_coach_possession = HasBallPossession({'player': 'Coach'})
A4_path_to_goal = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.2}})
A5_pass_to_goal = MakePass({'player': 'Coach'})
A6_final_coach_possession = HasBallPossession({'player': 'Coach'})
A7_path_to_goal_final = HasPath({'obj1': 'Coach', 'obj2': 'goal', 'path_width': {'avg': 2.0, 'std': 0.2}})

def λ_target_overlap():
    return A1_overlap.dist(simulation(), ego=True)

def λ_precondition_pass_to_coach(scene, sample):
    return A2_pass_to_coach.bool(simulation())

def λ_precondition_coach_possession(scene, sample):
    return A3_coach_possession.bool(simulation())

def λ_precondition_path_to_goal(scene, sample):
    return A4_path_to_goal.bool(simulation())

def λ_precondition_pass_to_goal(scene, sample):
    return A5_pass_to_goal.bool(simulation())

def λ_precondition_final_coach_possession(scene, sample):
    return A6_final_coach_possession.bool(simulation())

def λ_precondition_final_path_to_goal(scene, sample):
    return A7_path_to_goal_final.bool(simulation())

def λ_termination_overlap(scene, sample):
    # Terminate overlap movement if ball possession switches or after a short time
    return not A3_coach_possession.bool(simulation())

def λ_termination_pass(scene, sample):
    # Terminate pass action if recipient starts moving towards ball (not pass success itself)
    return not A6_final_coach_possession.bool(simulation())

behavior CoachBehavior():
    do Idle() for 3 seconds
    do Speak("Start the overlap run to get in a better position behind opponent.")
    do MoveTo(λ_target_overlap())
    do Speak("Wait until teammate passes ball.")
    do Idle() until λ_precondition_pass_to_coach(simulation(), None)
    do Speak("Stop and receive ball as Coach prepares to get possession.")
    do StopAndReceiveBall()
    do Speak("Wait until Coach gains ball possession.")
    do Idle() until λ_precondition_coach_possession(simulation(), None)
    do Speak("Check for a clear path to goal to decide next action.")
    do Idle() until λ_precondition_path_to_goal(simulation(), None)
    do Speak("Pass ball forward into space near goal.")
    do Pass(goal)
    do Speak("Wait until pass is complete and Coach regains possession near goal.")
    do Idle() until λ_precondition_final_coach_possession(simulation(), None)
    do Speak("Check for final shooting opportunity with clear path.")
    do Idle() until λ_precondition_final_path_to_goal(simulation(), None)
    do Speak("Take a shot at the goal to finish the attack.")
    do Shoot(goal)
    do Idle()





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