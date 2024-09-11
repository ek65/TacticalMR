from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

timestep = 0.1

footed = DiscreteRange(-1, 1)

pressingDistance = 3.5 #Uniform(4, 5)
shootingDistance = Uniform (4, 8)

behavior opponentBehavior():
    try:
        do InterceptBall(football)
    interrupt when hasBallPosession(self):
        do SetPlayerSpeed(5.0)
        do MoveTo(goal.position) for 0.1 seconds
        opponent.prevPosition = opponent.position
    interrupt when self.gameObject.ballPossession and distance from self to ego < pressingDistance:
        do SetPlayerSpeed(10.0)
        if abs(opponent.position.x - ego.position.x) < 1:
            do MoveTo(ego.position + Vector(1.5 * footed, 1.5, 0)) for 0.1 seconds
            opponent.prevPosition = opponent.position
        else:
            do MoveTo(ego.position + Vector(2 * footed, -1, 0)) for 0.1 seconds
            opponent.prevPosition = opponent.position
    interrupt when self.gameObject.ballPossession and distance from self to ego < (pressingDistance + 2) and distance from self to ego > (pressingDistance):
        do SetPlayerSpeed(1.5)
        do MoveTo(self.position + Vector((self.position.x - ego.position.x) * 5, 0, 0)) for 0.1 seconds
        opponent.prevPosition = opponent.position
    interrupt when distance from self to goal < distance from ego to goal: 
            try:
                do SetPlayerSpeed(5.0)
                do MoveTo(goal.position + Vector(0, 4, 0)) for 0.1 seconds
                opponent.prevPosition = opponent.position
            interrupt when distance from self to goal < shootingDistance:
                do ShootBall(goal.position)
                do Idle()
                opponent.prevPosition = opponent.position


ego = new Human at (0, 0, 0)

opponent = new Player ahead of ego by Uniform(8, 10),
                facing directly toward ego,
                with behavior opponentBehavior()



football = new Ball ahead of ego by 0.5
goal = new Goal behind ego by 8, facing away from ego
        
terminate when (ego.gameObject.stopButton)

