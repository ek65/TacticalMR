from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random



behavior CoachBehavior():
    do Speak("Wait while the teammate gets the ball")
    do Idle() until λ_teammate_possession(simulation(), None)
    
    do Speak("Time to overlap the teammate and create space")
    do MoveTo(λ_overlap_position())
    
    do Speak("Wait for the teammate to notice the overlap or to pass")
    do Idle() until λ_teammate_pass_or_open(simulation(), None)
    
    do Speak("Receive the pass from the teammate and get possession")
    do ReceiveBall()
    
    do Speak("Check if the defender is close or far before deciding")
    do Idle() until λ_possession_confirm(simulation(), None)
    
    if λ_defender_is_close(simulation(), None):
        do Speak("Defender is close, pass the ball back to teammate")
        do Pass(teammate)
    else:
        do Speak("Defender is far, move forward into space with the ball")
        do MoveTo(λ_forward_position())
        do Speak("Maintain possession or look for next opportunity")
        do Idle() until λ_termination(simulation(), None)


# Constraints
A1_overlap = Overlap({
    'player': 'Coach',
    'ball': 'ball',
    'goal': 'goal',
    'opponent': 'defender1',
    'theta': {'avg': 34.5, 'std': 5.5},
    'dist': {'avg': 6.8, 'std': 2.2}
})

A2_makepass = MakePass({'player': 'teammate'})

A3_possession = HasBallPossession({'player': 'Coach'})

A4_defender_close = CloseTo({
    'obj': 'Coach',
    'ref': 'defender1',
    'max': {'avg': 3.7, 'std': 0.5}
})

A5_forward = DistanceTo({
    'from': 'Coach',
    'to': 'goal',
    'min': {'avg': 8.3, 'std': 2.2},
    'max': None,
    'operator': 'greater_than'
})


# Lambda Functions
def λ_teammate_possession(scene, sample):
    return HasBallPossession({'player': 'teammate'}).bool(simulation())

def λ_overlap_position():
    return A1_overlap.dist(simulation(), ego=True)

def λ_teammate_pass_or_open(scene, sample):
    return (
        A2_makepass.bool(simulation()) or
        HasPath('teammate', 'Coach', 3.0).bool(simulation())
    )

def λ_possession_confirm(scene, sample):
    return A3_possession.bool(simulation())

def λ_defender_is_close(scene, sample):
    return A4_defender_close.bool(simulation())

def λ_forward_position():
    return A5_forward.dist(simulation(), ego=True)

def λ_termination(scene, sample):
    return not A3_possession.bool(simulation())



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
    do GetBallPossession(ball)
    do Idle() until ego.position.y > 2
    print("ego at good position")
    do Idle() for 1 seconds
    do Pass(ego, slow=False) until (distance from opponent to ego) <= 3
    print((distance from opponent to ego))
    print("pass happened")
    do Idle() for 5 seconds
    do DribbleTo(goal) until (distance from opponent to ego) > 3
    do Idle() for 2 seconds
    

behavior DefenderBehavior():
    do Idle() for 1 seconds
    do Idle() until ego.position.y > 1
    do Follow(ego) until ego.gameObject.ballPossession
    

teammate = new Player at (0, 0, 0),
      with behavior TeammateBehavior(), with name "teammate"

ball = new Ball ahead of teammate by 1

ego = new Coach at (ego_x_distance, ego_y_distance, 0), with name "Coach", with team "blue", with behavior CoachBehavior()

opponent = new Player at (0, Uniform(4, 6), 0), with name "defender1",
            with behavior DefenderBehavior()

goal = new Goal at (0, 17, 0)