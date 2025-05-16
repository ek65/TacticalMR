from scenic.simulators.unity.actions_backup import *
from scenic.simulators.unity.behaviors_backup import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do MoveTo(λ_target0) until λ_termination0(simulation(), None)
    print("moveto")
    do Idle() until λ_precondition_0(simulation(), None)
    print("idle1")
    do GetBallPossession(ball)
    print("getball")
    do Idle() until λ_precondition_1_4(simulation(), None)
    print("idle2")
    if λ_precondition1(simulation(), None):
        print("precondition1")
        do MoveTo(λ_target2) until λ_termination2(simulation(), None)
        print("moveto2")
        do Idle() until λ_precondition_2(simulation(), None)
        print("idle3")
        do Shoot(goal)
    else:
        print("precondition4")
        do Pass(teammate)
A1termination_0 = MakePass({'player': 'teammate'})
A1target_0 = HorizontalRelation({'obj': 'Coach', 'ref': None, 'relation': 'left', 'horizontal_threshold': {'avg': 5, 'std': 0.0}})
A2target_0 = HorizontalRelation({'obj': 'Coach', 'ref': None, 'relation': 'right', 'horizontal_threshold': {'avg': 5, 'std': 0.0}})
A1termination_2 = CloseTo({'obj': 'Coach', 'ref': 'goal', 'max': {'avg': 4.937129569846508, 'std': 0.0}})
A1target_2 = CloseTo({'obj': 'Coach', 'ref': 'goal', 'max': {'avg': 4.937129569846508, 'std': 0.16371462664283953}})
A1precondition_0 = MakePass({'player': 'teammate'})
A2precondition_0 = HasBallPossession({'player': 'Coach'})
A1precondition_1 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_2 = CloseTo({'obj': 'Coach', 'ref': 'goal', 'max': {'avg': 4.937129569846508, 'std': 0.0}})
A1precondition_4 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A2precondition_4 = CloseTo({'obj': 'teammate', 'ref': 'goal', 'max': 10})
def λ_target0(scene, sample):
    return A1target_0(simulation(), sample) or A2target_0(simulation(), sample)

def λ_target2(scene, sample):
    return A1target_2(simulation(), sample)

def λ_termination0(scene, sample):
    return A1termination_0(simulation(), None)

def λ_termination1(scene, sample):
    return True

def λ_termination2(scene, sample):
    return A1termination_2(simulation(), None)

def λ_termination3(scene, sample):
    return True

def λ_termination4(scene, sample):
    return True

def λ_precondition0(scene, sample):
    return (A1precondition_0(simulation(), sample) and A2precondition_0(simulation(), sample))

def λ_precondition_0(scene, sample):
    return λ_precondition0(simulation(), sample)

def λ_precondition1(scene, sample):
    output = A1precondition_1(simulation(), sample)
    print(f"opponent pressure coach: {output}")
    return output

def λ_precondition4(scene, sample):
    output1 = A1precondition_4(simulation(), sample)
    output2 = A2precondition_4(simulation(), sample)
    print(f"opponent pressure coach: {output1}, teammate close to goal: {output2}")
    return A1precondition_4(simulation(), sample) and A2precondition_4(simulation(), sample)

def λ_precondition_1_4(scene, sample):
    return λ_precondition1(simulation(), sample) or λ_precondition4(simulation(), sample)

def λ_precondition2(scene, sample):
    return A1precondition_2(simulation(), sample)

def λ_precondition_2(scene, sample):
    return λ_precondition2(simulation(), sample)

behavior Follow(obj):
    while True:
        do MoveToBehavior(obj, distance = 3, status = f"Follow {obj.name}")

behavior opponent1Behavior():
    do Idle() until teammate.gameObject.ballPossession
    do Follow(ball) until ego.gameObject.ballPossession
    do Uniform(Follow(ego), Follow(teammate))
    # do Follow(teammate)
    # print("opponent follows ego")
    # do Follow(ego)

A = HasPath({'obj1': 'teammate', 'obj2': 'coach', 'path_width':{'avg': 2, 'std':1}})

behavior TeammateBehavior():
    passed = False
    try:
        do GetBallPossession(ball)
        do Idle()
    interrupt when (A(simulation(), None) and not passed and self.gameObject.ballPossession):
        do Idle() for 2.5 seconds
        do Pass(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point) until MakePass({'player': 'coach'})(simulation(), None)
        do Idle() for 1 seconds
        do GetBallPossession(ball)
        do Shoot(goal)
        passed = True

teammate = new Player at (0,0), 
                with name "teammate",
                with team "blue",
                with behavior TeammateBehavior()

ball = new Ball ahead of teammate by 1

opponent = new Player ahead of teammate by 5,
                    facing toward teammate,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()

ego = new Human behind opponent by 5, 
            facing toward teammate,
            with name "Coach",
            with team "blue"

goal = new Goal behind opponent by 10, facing away from ego

terminate when (ego.gameObject.stopButton)