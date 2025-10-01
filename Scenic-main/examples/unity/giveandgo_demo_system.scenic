from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
from scenic.simulators.unity.constraints import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do Idle() for 3 seconds
    # do Speak("you should move away from opponent by more than 6 meters by moving to the left, and recieve the ball from teammate")
    do MoveTo(λ_target0(), True)
    # do Speak("wait to see if the opponent decides to pressure you or not")
    do Idle() until True
    if λ_precondition_1(): # explain the precondition in the speak line below.
        # when using if/elif/else conditions, explain which condition is satisfied first
        # do Speak("In this case, the opponent is pressuring you.")
        # explain the action to take in the speak line below.
        # do Speak("So, move more than 6 meters away from opponent")
        do MoveTo(λ_target2(), False)
        # do Speak("wait until teammate moves towards goal")
        do Idle() until λ_precondition_4()
        # do Speak("pass the ball to teammate")
        do Pass(teammate)
    else:
        # do Speak("The opponent is not pressuring you.")
        # do Speak("So, move close to goal, within 12 meters")
        do MoveTo(λ_target5())
        # do Speak("take a shot towards the goal")
        do Shoot(goal)
    do Idle()

A1target_0 = DistanceTo({'from': 'opponent', 'to': 'Coach', 'min': {'avg': 6.399477695297064, 'std': 0.8416729364595561}, 'max': None, 'operator': 'greater_than'})
A2target_0 = HorizontalRelation({'obj': 'Coach', 'ref': 'opponent', 'relation': 'left', 'horizontal_threshold': {'avg': 4.0, 'std': 1.0}})
A1target_2 = DistanceTo({'from': 'Coach', 'to': 'opponent', 'min': {'avg': 6.172259899611368, 'std': 0.0}, 'max': None, 'operator': 'greater_than'})
A2target_5 = DistanceTo({'from': 'Coach', 'to': 'goal', 'min': None, 'max': {'avg': 11.941602839093648, 'std': 0.01539784416917822}, 'operator': 'less_than'})

A1precondition_0 = MakePass({'player': 'teammate'})
A1precondition_1 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_3 = Pressure({'player1': 'opponent', 'player2': 'Coach'})
A1precondition_4 = MovingTowards({'obj': 'Teammate', 'ref': 'goal'})

def λ_target0():
    cond = A1target_0 & A2target_0
    return cond.dist(simulation(), ego=True)

def λ_target2():
    return A1target_2.dist(simulation(), ego=True)

def λ_target5():
    return A2target_5.dist(simulation(), ego = True)

def λ_precondition_0():
    return A1precondition_0.bool(simulation())

def λ_precondition3():
    cond = ~ A1precondition_3
    return cond.bool(simulation())

def λ_precondition_1():
    return A1precondition_1.bool(simulation())

def λ_precondition_4():
    return A1precondition_4.bool(simulation())

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
    gotBall = False
    try:
        do MoveToBallAndGetPossession()
        gotBall = True
        do Idle()
    interrupt when (not passed and self.gameObject.ballPossession and gotBall and ego.triggerPass):
        do Idle() for 0.5 seconds
        do Pass(ego.xMark)
        do Idle() for 0.5 seconds
        take StopAction()
        point = new Point at (0,10,0)
        do MoveToBehavior(point)
        do Idle() until self.gameObject.ballPossession
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

ego = new Coach behind opponent by 5, 
            facing toward teammate,
            with name "Coach",
            with team "blue",
            with behavior CoachBehavior()

goal = new Goal behind opponent by 10, facing away from ego

terminate when (ego.gameObject.stopButton)