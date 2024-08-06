from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

# TODO: Right now the ball automatically gets recieved by AI when near ball, 
# instead make an action or paramter that enables this when we want

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (4, 2, .1), position = (-1, -1.5, 0))

class Coach(Human):
    explained : False

behavior opponent1Behavior():
    try:
        do InterceptBall(ball)
        do Idle() 
    interrupt when ((distance from ego to self) < 4 and self.gameObject.ballPossession and distance from self to pt > 0.1):
        do Idle() for 0.5 seconds
        do GroundPassFast(opponent2.position)
        do Idle() for 1 seconds
        do ApproachGoal(pt)
    interrupt when (distance from self to pt < 0.5 and self.gameObject.ballPossession):
        option = Uniform(1, 2, 3)
        # The finishing shot will be skewed left, center, or right
        if (option == 1):
            do ShootBall(goal.position, "left-middle")
        elif (option == 2):
            do ShootBall(goal.position, "center-middle")
        elif (option == 3):
            do ShootBall(goal.position, "right-middle")
        abort
    
behavior opponent2Behavior():
    try:
        do Idle()
    interrupt when (self.gameObject.ballPossession):
        do Idle() for 1 seconds
        do GroundPassFast(pt.position)
        abort

behavior coachBehavior():
    try:
        do Idle()
    # interrupt when (distance from opponent1.position to ball < 0.5):
    #     do LookAt(goal.position)
    interrupt when self.gameObject.pause == False and self.explained == False and (distance from opponent1.position to ball < 0.5):
        do Pause()
    interrupt when self.gameObject.pause == True and self.explained == False:
        if (self.explained == False):
            do Speak("Say \"" + pressExplanation + "\"")
            self.explained = True
    interrupt when self.gameObject.pause == False and self.explained == True:
        do Idle()

pressExplanation = "When you're on the field and you notice you're the one closest to the opponent with the ball, that's your cue to press. You move in, keeping yourself within a meter of them. This setup allows you to react quickly if they try to make a move past you."



test = False
spawn_range = Range(0,0.1)
ego = new Coach at (5, spawn_range, 0), with behavior coachBehavior()
ball = new Ball at ego offset by Range(-4, 4) @ Range(4, 4.5)
pt = new Point in penalty_box
goal = new Goal behind ego by Range(2.9,3), facing away from ego


opponent1 = new Player at ball offset by Range(-1, 1) @ Range(4.6, 4.7),
                    facing toward ego,
                    with behavior opponent1Behavior(),
                    with name "opponent1"


opponent2 = new Player right of ego by Range(4, 6), 
                    facing toward opponent1,
                    with behavior opponent2Behavior(),
                    with name "opponent2"

terminate when (ego.gameObject.stopButton)