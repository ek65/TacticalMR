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
        do MoveToBehavior(ball, distance = 3)

behavior TeammateBehavior():
    try:
        do Idle()
    interrupt when (ego.position.y > opponent.position.y):
        print("ego ahead of opponent")
        do PassTo(ego, slow=False)
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
    print('[MoveTo] the opponent (terminate if has ball)')
    do MoveTo(target_1) until termination_1(simulation())
    # do GetBall()
    print("[Pass] to the teammate")
    do Idle() until termination_3(simulation())
    do PassTo(teammate)
    print("[MoveTo] behind the opponent")
    do MoveTo(target_2) until termination_2(simulation())
    print("[Idle] behind the opponent")
    do Idle()
    print("receiveball")

# test = False
# ego = new Human at (0, 0)
ego = new Coach at (0,-3,0),
        with behavior CoachBehavior()

ball = new Ball ahead of ego by 5

opponent = new Player ahead of ego by 10,
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


C = InZone({
    'obj': 'Coach',
    'zone': ['B4','C4']
})

# C = HeightRelation({
#     'obj': 'coach',
#     'ref': 'opponent',
#     'relation': 'ahead',
#     'height_threshold': {
#         'avg': 3.3
#     }
# })

D = HasAngleOfPass({
    'passer': 'teammate',
    'receiver': 'Coach',
    'radius': {'avg': 2.0, 'std': 1.0}
})

E = HorizontalRelation({
    'obj': 'Coach',
    'ref': 'opponent',
    'relation': 'right',
    'x_threshold': {'avg': 3.0, 'std': 1.0},
})

F = MovingTowards({
    'obj': 'opponent',
    'ref': 'teammate'
})

G = CloseTo({
    'ref': 'ball',
    'obj': 'Coach',
    'max': 2
})

def target_1(scene, sample):
    return G(scene, sample)

def termination_1(scene):
    return B(scene, None)

def target_2(scene, sample):
    return C(scene, sample) and D(scene, sample)

def termination_2(scene):
    return B(scene, None)

def termination_3(scene):
    return F(scene, None)