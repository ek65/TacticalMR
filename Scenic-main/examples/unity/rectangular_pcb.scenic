# Scenario 02:
# Rectangular Press-Cover-Balance formation

#    0    0.   0    0   
# /--------------------\
# |       H            |
# |  O         O       |
# |                 O  |
# |                    |
# \--------------------/
#    0    0    0    0    

#            |
#            V

#    0    0    0.   0    +8
# /--------------------\
# |            O       | +4
# |       H         O  |  0
# |  O                 | -4
# |                    | -8
# \--------------------/
#    0    0    0    0    -10
#
#    -4   0    +4   +8

# H: user (defendant)
# O: defendants
# 0: attackers
# .: football
# -, |, /, \ : defending boundary

from scenic.simulators.unity.actions import *
from scenic.simulators.unity.behaviors import *
model scenic.simulators.unity.model

# Classes -------------------------------------------------------

class Defendant(Player):
    friendly: True
    action: "idle"
    target: None

class Opponent(Player):
    friendly: False
    counterTarget: None

# Actions and conditions ----------------------------------------

# Idle

behavior idleBehavior():
    do MoveTo(self.tacticalPosition)
    do Idle()

# Press

def pressingCondition(self):
    # Defendant is the closest to opponent with ball

    print(f"checking if {self.name} is pressing")

    objects = simulation().objects

    posessionOpponent = None
    for obj in objects:
        if not isinstance(obj, Opponent) or obj.gameObject.ballPossession:
            continue
        posessionOpponent = obj

    if posessionOpponent:
        return self == closerTo(posessionOpponent, Defendant)
    else:
        return False

behavior pressingBehavior():
    do MoveTo(Vector(self.position.x, 4, 0))
    do Idle()
# Cover

def coveringCondition(self):
    # Pressing defendant isn't covered already by 2 or more 
    # Pressing player isn't too far from tactical position

    objects = simulation().objects
    pressingDefendants = []

    for obj in objects:
        if not isinstance(obj, Defendant) or obj.action != "press":
            continue
        pressingDefendants.append(obj)

    for player in pressingDefendants:
        if self in closerTo(player, Defendant, 2):
            return True

    return False
    
behavior coveringBehavior():
    do MoveTo(Vector(self.position.x, 0, 0))
    do Idle()

# Utility functions ---------------------------------------------

def closerTo(origin, type: type, num=1, condition=None) -> list:
    objects = simulation().objects
    distances = []

    for obj in objects:
        if not isinstance(obj, type) or obj == origin:
            continue

        if condition != None and not condition(obj):
            continue

        d = distance from origin to obj
        distances.append((obj, d))

    distances.sort(key=lambda x: x[1])
    closer = [obj for obj, _ in distances[:num]]

    if len(closer) == 1:
        return closer[0]
    else:
        return closer

def teamPossession(player) -> bool:
    objects = simulation().objects

    for obj in objects:

        if isinstance(player, Defendant):
            if (isinstance(obj, Defendant) and obj.gameObject.ballPossession):
                return True

        elif isinstance(player, Opponent):
            if (isinstance(obj, Opponent) and obj.gameObject.ballPossession):
                return True

    return False

def findTargets(player, prev=None):

    if isinstance(player, Opponent):
        closestDefendant = closerTo(player, Defendant)
        findTargets(closestDefendant, player)
    elif isinstance(player, Defendant):
        if isinstance(prev, Opponent):
            player.action = "press"
            closerDefendants = closerTo(player, Defendant, 2)
            for nextPlayer in closerDefendants:
                findTargets(nextPlayer, player)
        if isinstance(prev, Defendant):
            player.action = "cover"

# Behaviors -----------------------------------------------------

behavior opponentBehavior():

    try:
        do Idle()
    interrupt when self == closerTo(football, Opponent):
        do InterceptBall(football)
    interrupt when teamPossession(self):
        do MoveTo(self.tacticalPosition)
    interrupt when self.gameObject.ballPossession:

        try:
            do MoveTo(self.tacticalPosition)
        interrupt when distance from self to self.tacticalPosition < 0.5:

            counterTarget = closerTo(self, Defendant)
            counterTarget.action = "press"

            cooldown = Range(3, 6)
            do Idle() for cooldown seconds

            available = closerTo(self, Opponent, 2)
            receiver = DiscreteRange(0, len(available) - 1)

            print(f"{self.name} passed the ball to {available[receiver].name}")
            do GroundPassFast(available[receiver].position)
            do Idle() for 1 seconds

# TODO: Dynamize positions

behavior defendantBehavior():
    try:
        do idleBehavior()
    interrupt when self.action == "balance":
        do MoveTo(Vector(self.position.x, -4, 0))
        do Idle()
    interrupt when coveringCondition(self):
        do coveringBehavior()
    interrupt when pressingCondition(self):
        do pressingBehavior()

# Objects -------------------------------------------------------

football = new Ball at (0, 6, 0)
ego = new Human at (0, 0, 0)

for i in [(-8, 8, "Mohammed"), (0, 8, "Daniel"), (8, 8, "Jorge"), (16, 8, "Devin")]:
    opponent = new Opponent offset by (i[0], i[1], 0),
                    with behavior opponentBehavior(),
                    facing toward ego
    opponent.team = "red"
    opponent.tacticalPosition = Vector(i[0], i[1], 0)
    opponent.name = i[2]

for i in [(-8, -4), (8, -4), (16, -4)]:
    defendant = new Defendant offset by (i[0], i[1], 0),
                    with behavior defendantBehavior()
    defendant.team = "blue"
    defendant.tacticalPosition = Vector(i[0], i[1], 0)

terminate when (ego.gameObject.stopButton)