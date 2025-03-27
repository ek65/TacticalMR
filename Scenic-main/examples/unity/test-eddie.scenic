from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model
import trimesh
from scenic.core.regions import MeshVolumeRegion
import random

behavior CoachBehavior():
    do MoveToWrapper(λ_target0) until λ_termination0(scene, sample)
    do Idle() until A0 and B0
    do passTo({'obj': 'teammate', 'through': False, 'info': 'Coach passes the ball to the teammate as an opponent approaches.'})
    do Idle() until A1
    do MoveToWrapper(λ_target2) until λ_termination2(scene, sample)

behavior MoveToWrapper(λ_dest):
    scene = simulation()
    sample = Vector(0, 0)
    sample = sample_target(scene, sample, λ_dest)
    while (distance from self to sample > 0.5):
        do MoveTo(sample) for timestep seconds
        sample = sample_target(scene, sample, λ_dest)
    do Idle() for 1 seconds

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

A0 = HasBallPossession({'player': 'Coach'})
A0 = CloseTo({'obj': 'Coach', 'ref': 'ball', 'max': [2.282500000000004, 0.0]})
A2 = HasBallPossession({'player': 'Coach'})
A2 = InZone({'obj': 'coach', 'zone': ['C4']})
B2 = HeightRelation({'obj': 'Coach', 'ref': 'opponent', 'relation': 'behind', 'height_threshold': {'avg': 2.8031676999999995, 'std': 2.0}})
A0 = HasBallPossession({'player': 'Coach'})
B0 = MovingTowards({'obj': 'opponent', 'ref': 'Coach'})
A1 = HasBallPossession({'player': 'teammate'})

def λ_dest0(scene, sample):
    return A0(scene, sample)
def λ_termination0(scene, sample):
    return A0(scene, sample)
def λ_termination1(scene, sample):
    return None  # No logical expression provided
def λ_dest2(scene, sample):
    return A2(scene, sample) and B2(scene, sample)
def λ_termination2(scene, sample):
    return A2(scene, sample)
def λ_precondition0(scene, sample):
    return A0(scene, sample) and B0(scene, sample)
def λ_precondition1(scene, sample):
    return A1(scene, sample)


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
    interrupt when (self.gameObject.ballPossession):
        print("ego ahead of opponent")
        point = new Point at (0, 11, 0)
        do Idle() for 1.5 seconds
        do PassTo(ego, slow=False)
        do Idle() for 0.5 seconds
        take StopAction()

behavior GetBall():
    while not self.gameObject.ballPossession:
        take MoveToAction(ball.position)

def teammateHasBallPossession():
    for obj in simulation().objects:
        if isinstance(obj, Player) and obj.team == "blue" and obj.gameObject.ballPossession:
            return True
    return False

behavior GetBehindAndReceiveBall(player, zone): # similar logic as inzone
    
    do MoveTo(point) until self.position.y > player.position.y + 2
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()

behavior ReceiveBall():
    while teammateHasBallPossession():
        take IdleAction()
    do GetBall()
    
# ego = new Human at (0,0)
ego = new Coach at (0,0),
        with behavior CoachBehavior(),
        with name 'Coach'

ball = new Ball ahead of ego by 1

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