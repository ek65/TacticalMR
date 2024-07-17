from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

penalty_box = MeshVolumeRegion(trimesh.creation.box((1, 1, 1)), dimensions = (4, 2, .1), position = (-1, -1.5, 0))

behavior aiBehavior():
    do Idle() for 5 seconds
    ego.gameObject.pause = True
    do Speak("Say \"My name is Jorge Diaz Chao\"")
    do Idle() for 5 seconds
    ego.gameObject.pause = Falses
    do Idle()
    
    


ego = new Human at (0, 0, 0), with behavior aiBehavior

# ai = new AIAgent at (0, 0, 0)
# ai = new AIAgent at (0, 0, 0), with behavior aiBehavior()


terminate when (ego.gameObject.stopButton)