from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
from scenic.core.regions import MeshVolumeRegion
import trimesh
import random

footed = DiscreteRange(-1, 1)

pressingDistance = 2.5 #Uniform(4, 5)
shootingDistance = Uniform (4, 8)

behavior opponentBehavior():
    try:
        do MoveTo(ball)
    interrupt when self.gameObject.ballPossession:
        do SetPlayerSpeed(1.0)
        do MoveTo(goal.position) for 0.1 seconds
    interrupt when self.gameObject.ballPossession and distance from self to ego < (pressingDistance + 2) and distance from self to ego > (pressingDistance):
        do SetPlayerSpeed(1.5)
        do MoveTo(self.position + Vector((self.position.x - ego.position.x) * 5, 0, 0)) for 0.1 seconds
    interrupt when distance from self to goal < distance from ego to goal: # ahead of defendant
            try:
                do SetPlayerSpeed(1.0)
                do MoveTo(goal.position + Vector(0, 4, 0)) for 0.1 seconds
            interrupt when distance from self to goal < shootingDistance:
                do MoveTo(ego.position)
                do Idle()

# Coach Behavior

# Define Ego

ego = new Human at (0, 0, 0) 


opponent = new Player ahead of ego by Uniform(5, 6),
                facing directly toward ego,
                with name "opponent",
                with behavior opponentBehavior()
ball = new Ball ahead of opponent by 0.5
goal = new Goal behind ego by 3, facing away from ego

# Python


terminate when (ego.gameObject.stopButton)