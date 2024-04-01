from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior opponent1Behavior():
    try:
        do InterceptBall(ball)
        do Idle()
    interrupt when (self.gameObject.ballPossession):
        do Idle()
    
behavior coachBehavior():
    try:
        do Idle()
    interrupt when self.gameObject.pause == False and self.explained == False and (distance from opponent1.position to ball < 0.5):
        do Pause()
    interrupt when self.gameObject.pause == True and self.explained == False:
        if (self.explained == False):
            do Explain(pressExplanation)
            self.explained = True
    interrupt when self.gameObject.pause == False and self.explained == True:
        do Idle()

pressExplanation = "When you're on the field and you notice you're the one closest to the opponent with the ball, that's your cue to press. You move in, keeping yourself within a meter of them. This setup allows you to react quickly if they try to make a move past you."

class Coach(Human):
    explained : False

spawn_range = Range(0,0.1)
ego = new Human at (5, spawn_range, 0), with behavior coachBehavior()
ball = new Ball at ego offset by Range(-4, 4) @ Range(4, 4.5)
goal = new Goal behind ego by Range(2.9,3), facing away from ego

opponent1 = new OffensePlayer ahead of ball by Range(0.5, 1),
                    facing toward ego,
                    with behavior opponent1Behavior()
opponent1.name = "Opponent A"

terminate when (ego.gameObject.stopButton)