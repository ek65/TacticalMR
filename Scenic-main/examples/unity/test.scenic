from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

def movesToward(player1, player2):
    dist1 = distance from player1.prevPosition to player2.prevPosition
    dist2 = distance from player1.position to player2.position
    return dist2 < dist1

behavior opponent1Behavior():
    do Idle() until ego.gameObject.ballPossession
    while True:
        do MoveTo(ball, distance = 4)

behavior TeammateBehavior():
    try:
        do Idle()
    interrupt when (ego.position.y > opponent.position.y):
        print("ego ahead of opponent")
        point = new Point at (0, 0, 11)
        do PassTo(point, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()

behavior GetBall():
    while not self.gameObject.ballPossession:
        take MoveToAction(ball.position)

def GetBehind(player): # similar logic as inzone
    point = new Point behind player by 5
    return point

def teammateHasBallPossession():
    for obj in simulation().objects:
        if isinstance(obj, Player) and obj.team == "blue" and obj.gameObject.ballPossession:
            return True
    return False

behavior ReceiveBall():
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()

sample = None

behavior CoachBehavior():
    scene = simulation()
    scene.egoObject = ego
    print('[MoveTo] the ball')
    do MoveToWrapper(target_1) until termination_1(scene, sample)
    print("getBall")
    do Idle() until movesToward(opponent, ego)
    print("Idle")
    do PassTo(teammate)
    print("pass To teammate")
    do MoveTo(GetBehind(opponent))
    print("get behind opponent")
    do ReceiveBall()
    print("receiveball")

# test = False
# ego = new Human at (0, 0)
ego = new Coach at (0,0),
        with behavior CoachBehavior()

ball = new Ball at (0, 3)

opponent = new Player ahead of ego by 5,
                    facing toward ego,
                    with name "opponent",
                    with team "red",
                    with behavior opponent1Behavior()

goal = new Goal behind opponent by 5, facing away from ego

teammate = new Player offset by (Uniform(-5,5), 7), 
                with name "teammate",
                with team "blue",
                with behavior TeammateBehavior()

terminate when (ego.gameObject.stopButton)

# ------ Sampling ------

def sample_target(scene, prev_target, λ_dest) -> Vector: 
    global sample
    i = 0
    target = [prev_target.x, prev_target.y]
    
    while not λ_dest(scene, target):
        x = Range(-FIELD_WIDTH / 2, FIELD_WIDTH / 2)
        y = Range(-FIELD_HEIGHT / 2, FIELD_HEIGHT / 2)
        target = [x,y]
        if i > 100000:
            raise Exception("Maximum sample depth exceeded.")
        i += 1

    sample = Vector(target[0], target[1])
    return sample

behavior MoveToWrapper(λ_dest):
    scene = simulation()
    sample = Vector(0, 0)
    sample = sample_target(scene, sample, λ_dest)
    while (distance from self to sample > 0.5):
        do MoveTo(sample) for timestep seconds
        sample = sample_target(scene, sample, λ_dest)
    do Idle() for 1 seconds

# ----------------------

A = DistanceTo({
    'from': 'coach',
    'to': 'opponent',
    'min': {
        'avg': 0.0
    },
    'max': {
        'avg': 0.5
    },
    'operator': 'less_than',
})

B = HasBallPossession({
    'player': 'coach'
})

def target_1(scene, sample):
    return A(scene, sample)

def termination_1(scene, sample):
    return B(scene, sample)

    